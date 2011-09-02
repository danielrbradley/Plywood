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
    public class Targets : ControllerBase
    {
        [Obsolete]
        public const string STR_TARGET_INDEX_PATH = ".targets.index";
        [Obsolete]
        public const string STR_TARGETS_CONTAINER_PATH = "targets";

        public Targets() : base() { }
        public Targets(ControllerConfiguration context) : base(context) { }

        public void CreateTarget(Target target)
        {
            if (target == null)
                throw new ArgumentNullException("target", "Target cannot be null.");
            if (target.GroupKey == Guid.Empty)
                throw new ArgumentException("Group key cannot be empty.", "target.GroupKey");

            using (var stream = target.Serialise())
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        var groupsController = new Groups(Context);
                        if (!groupsController.GroupExists(target.GroupKey))
                            throw new GroupNotFoundException(String.Format("Group with the key \"{0}\" could not be found.", target.GroupKey));

                        var indexEntries = new IndexEntries(Context);

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = Paths.GetTargetDetailsKey(target.Key),
                            InputStream = stream,
                        })) { }

                        indexEntries.PutEntity(target);
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed creating target.", awsEx);
                }
            }
        }

        public void DeleteTarget(Guid key)
        {
            var target = GetTarget(key);
            try
            {
                var indexEntries = new IndexEntries(Context);

                indexEntries.DeleteEntity(target);

                Plywood.Internal.AwsHelpers.SoftDeleteFolders(Context, Paths.GetTargetDetailsKey(key));
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed deleting target.", awsEx);
            }
        }

        public Target GetTarget(Guid key)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObject(new GetObjectRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = string.Format("{0}/{1}/{2}", STR_TARGETS_CONTAINER_PATH, key.ToString("N"), STR_INFO_FILE_NAME),
                    }))
                    {
                        using (var stream = res.ResponseStream)
                        {
                            return new Target(stream);
                        }
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                if (awsEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new TargetNotFoundException(string.Format("Could not find the target with key: {0}", key), awsEx);
                }
                else
                {
                    throw new DeploymentException(string.Format("Failed getting target with key \"{0}\"", key), awsEx);
                }
            }
        }

        public TargetList SearchTargets(Guid? groupKey, string query = null, string marker = null, int pageSize = 50)
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
                startLocations[0] = "ti";
                if (groupKey.HasValue)
                    startLocations[1] = string.Format("gi/{0}/ti");

                IEnumerable<string> basePaths;

                if (!string.IsNullOrWhiteSpace(query))
                    basePaths = new SimpleTokeniser().Tokenise(query).SelectMany(token =>
                        startLocations.Select(l => string.Format("{0}/t/{1}", l, Indexes.IndexEntries.GetTokenHash(token))));
                else
                    basePaths = startLocations.Select(l => string.Format("{0}/e", l));

                var indexEntries = new IndexEntries(Context);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var targets = rawResults.FileNames.Select(fileName => new TargetListItem(fileName));
                var list = new TargetList()
                {
                    Targets = targets,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = targets.Last().Marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed searcing targets.", awsEx);
            }
        }

        public void UpdateTarget(Target target)
        {
            if (target == null)
                throw new ArgumentNullException("target", "Target cannot be null.");

            var existingTarget = GetTarget(target.Key);
            // Don't allow moving between groups.
            target.GroupKey = existingTarget.GroupKey;

            using (var stream = target.Serialise())
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = Paths.GetTargetDetailsKey(target.Key),
                            InputStream = stream,
                        })) { }

                        var indexEntries = new IndexEntries(Context);
                        indexEntries.UpdateEntity(existingTarget, target);
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed updating target.", awsEx);
                }
            }
        }

        [Obsolete]
        public string GetGroupTargetsIndexPath(Guid groupKey)
        {
            return string.Format("{0}/{1}/{2}", Groups.STR_GROUPS_CONTAINER_PATH, groupKey.ToString("N"), STR_TARGET_INDEX_PATH);
        }

        public bool TargetExists(Guid key)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObjectMetadata(new GetObjectMetadataRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = Paths.GetTargetDetailsKey(key),
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
                    throw new DeploymentException(string.Format("Failed getting target with key \"{0}\"", key), awsEx);
                }
            }
        }
    }
}
