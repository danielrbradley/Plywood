using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Amazon.S3;
using Amazon.S3.Model;
using Plywood.Indexes;

namespace Plywood
{
    public class Apps : ControllerBase
    {
        [Obsolete]
        public const string STR_APP_INDEX_PATH = ".apps.index";
        [Obsolete]
        public const string STR_APPS_CONTAINER_PATH = "a";

        public Apps() : base() { }
        public Apps(ControllerConfiguration context) : base(context) { }

        internal bool AppExists(Guid key)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObjectMetadata(new GetObjectMetadataRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = Paths.GetAppDetailsKey(key),
                    })) { return true; }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                if (awsEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }
                else
                {
                    throw new DeploymentException(string.Format("Failed getting app with key \"{0}\"", key), awsEx);
                }
            }
        }

        public void CreateApp(App app)
        {
            if (app == null)
                throw new ArgumentException("App cannot be null.", "app");
            if (app.GroupKey == Guid.Empty)
                throw new ArgumentException("Group key cannot be empty.", "app.GroupKey");

            using (var stream = app.Serialise())
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        var groupsController = new Groups(Context);
                        if (!groupsController.GroupExists(app.GroupKey))
                            throw new GroupNotFoundException(String.Format("Group with the key {0} could not be found.", app.GroupKey));

                        var indexEntries = new IndexEntries(Context);
                        // TODO: Check key, name and deployment directory are unique.

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = Paths.GetAppDetailsKey(app.Key),
                            InputStream = stream,
                        })) { }

                        indexEntries.PutEntity(app);
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed creating app.", awsEx);
                }
            }
        }

        public void DeleteApp(Guid key)
        {
            var app = GetApp(key);
            try
            {
                var indexEntries = new IndexEntries(Context);

                indexEntries.DeleteEntity(app);

                Plywood.Internal.AwsHelpers.SoftDeleteFolders(Context, Paths.GetAppDetailsKey(key));

            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed deleting app.", awsEx);
            }
        }

        public App GetApp(Guid key)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObject(new GetObjectRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = Paths.GetAppDetailsKey(key),
                    }))
                    {
                        using (var stream = res.ResponseStream)
                        {
                            return new App(stream);
                        }
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                if (awsEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new AppNotFoundException(string.Format("Could not find the app with key: {0}", key), awsEx);
                }
                else
                {
                    throw new DeploymentException(string.Format("Failed getting app with key \"{0}\"", key), awsEx);
                }
            }
        }

        public string PushAppRevision(Guid appKey)
        {
            var app = GetApp(appKey);
            var thisRevision = String.Format("{0}.{1}", app.MajorVersion, app.Revision);
            app.Revision += 1;
            UpdateApp(app);
            return thisRevision;
        }

        public AppList SearchGroupApps(Guid? groupKey = null, string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 1.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                string[] startLocations;
                if (groupKey.HasValue)
                    startLocations = new string[2];
                else
                    startLocations = new string[1];
                startLocations[0] = "ai";
                if (groupKey.HasValue)
                    startLocations[1] = string.Format("gi/{0}");

                IEnumerable<string> basePaths;

                if (!string.IsNullOrWhiteSpace(query))
                    basePaths = new SimpleTokeniser().Tokenise(query).SelectMany(token =>
                        startLocations.Select(l => string.Format("{0}/t/{1}", l, Indexes.IndexEntries.GetTokenHash(token))));
                else
                    basePaths = startLocations.Select(l => string.Format("{0}/e", l));

                var indexEntries = new IndexEntries(Context);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var apps = rawResults.FileNames.Select(fileName => new AppListItem(fileName));
                var list = new AppList()
                {
                    Apps = apps,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = apps.Last().Marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed searcing apps.", awsEx);
            }
        }

        public void UpdateApp(App app)
        {
            if (app == null)
                throw new ArgumentNullException("app", "App cannot be null.");

            var existingApp = GetApp(app.Key);
            // Don't allow moving between groups right now as would have to recursively update references from versions and targets within app.
            app.GroupKey = existingApp.GroupKey;

            using (var stream = app.Serialise())
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        // This will not currently get called.
                        if (existingApp.GroupKey != app.GroupKey)
                        {
                            var groupsController = new Groups(Context);
                            if (!groupsController.GroupExists(app.GroupKey))
                                throw new GroupNotFoundException(string.Format("Group with key \"{0}\" to move app into cannot be found.", app.GroupKey));
                        }

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = Paths.GetAppDetailsKey(app.Key),
                            InputStream = stream,
                        })) { }

                        var indexEntries = new IndexEntries(Context);
                        indexEntries.UpdateEntity(existingApp, app);
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed updating app.", awsEx);
                }
            }
        }

        [Obsolete]
        public static string GetGroupAppsIndexPath(Guid groupKey)
        {
            return string.Format("{0}/{1}/{2}", Groups.STR_GROUPS_CONTAINER_PATH, groupKey.ToString("N"), STR_APP_INDEX_PATH);
        }
    }
}
