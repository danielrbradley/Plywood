using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Amazon.S3;
using Amazon.S3.Model;

namespace Plywood
{
    public class Apps : ControllerBase
    {
        public const string STR_APP_INDEX_PATH = ".apps.index";
        public const string STR_APPS_CONTAINER_PATH = "apps";

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
                        Key = string.Format("{0}/{1}/{2}", STR_APPS_CONTAINER_PATH, key.ToString("N"), STR_INFO_FILE_NAME),
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

            using (var stream = Serialisation.Serialise(app))
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        var groupsController = new Groups(Context);
                        if (!groupsController.GroupExists(app.GroupKey))
                            throw new GroupNotFoundException(String.Format("Group with the key {0} could not be found.", app.GroupKey));

                        var indexesController = new Internal.Indexes(Context);

                        string indexPath = GetGroupAppsIndexPath(app.GroupKey);
                        var appIndex = indexesController.LoadIndex(indexPath);
                        if (appIndex.Entries.Any(e => e.Key == app.Key))
                        {
                            throw new DeploymentException("Index already contains entry for given key!");
                        }

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = string.Format("{0}/{1}/{2}", STR_APPS_CONTAINER_PATH, app.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }

                        appIndex.Entries.Add(new Internal.EntityIndexEntry() { Key = app.Key, Name = app.Name });
                        Internal.Indexes.NameSortIndex(appIndex);
                        indexesController.UpdateIndex(indexPath, appIndex);
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
                Plywood.Internal.AwsHelpers.SoftDeleteFolders(Context, string.Format("{0}/{1}", STR_APPS_CONTAINER_PATH, key.ToString("N")));

                var indexesController = new Internal.Indexes(Context);

                string indexPath = GetGroupAppsIndexPath(app.GroupKey);
                var appIndex = indexesController.LoadIndex(indexPath);
                if (appIndex.Entries.Any(e => e.Key == key))
                {
                    appIndex.Entries.Remove(appIndex.Entries.Single(e => e.Key == key));
                    Internal.Indexes.NameSortIndex(appIndex);
                    indexesController.UpdateIndex(indexPath, appIndex);
                }
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
                        Key = string.Format("{0}/{1}/{2}", STR_APPS_CONTAINER_PATH, key.ToString("N"), STR_INFO_FILE_NAME),
                    }))
                    {
                        using (var stream = res.ResponseStream)
                        {
                            return Serialisation.ParseApp(stream);
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

        public AppList SearchGroupApps(Guid groupKey, string query = null, int offset = 0, int pageSize = 50)
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException("offset", "Offset cannot be a negative number.");
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 1.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                var indexesController = new Internal.Indexes(Context);
                var index = indexesController.LoadIndex(GetGroupAppsIndexPath(groupKey));

                var filteredIndex = index.Entries.AsQueryable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var queryParts = query.ToLower().Split(new char[] { ' ', '\t', ',' }).Where(qp => !string.IsNullOrWhiteSpace(qp)).ToArray();
                    filteredIndex = filteredIndex.Where(e => queryParts.Any(q => e.Name.ToLower().Contains(q)));
                }

                var count = filteredIndex.Count();
                var listItems = filteredIndex.Skip(offset).Take(pageSize).Select(e => new AppListItem() { Key = e.Key, Name = e.Name }).ToList();
                var list = new AppList()
                {
                    GroupKey = groupKey,
                    Apps = listItems,
                    Query = query,
                    Offset = offset,
                    PageSize = pageSize,
                    TotalCount = count,
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

            using (var stream = Serialisation.Serialise(app))
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        var indexesController = new Internal.Indexes(Context);
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
                            Key = string.Format("{0}/{1}/{2}", STR_APPS_CONTAINER_PATH, app.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }

                        // This will not currently get called.
                        if (existingApp.GroupKey != app.GroupKey)
                        {
                            string oldAppIndexPath = GetGroupAppsIndexPath(existingApp.GroupKey);
                            indexesController.DeleteIndexEntry(oldAppIndexPath, app.Key);
                        }

                        string newAppIndexPath = GetGroupAppsIndexPath(app.GroupKey);
                        indexesController.PutIndexEntry(newAppIndexPath, new Internal.EntityIndexEntry() { Key = app.Key, Name = app.Name });
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed updating app.", awsEx);
                }
            }
        }

        public static string GetGroupAppsIndexPath(Guid groupKey)
        {
            return string.Format("{0}/{1}/{2}", Groups.STR_GROUPS_CONTAINER_PATH, groupKey.ToString("N"), STR_APP_INDEX_PATH);
        }
    }
}
