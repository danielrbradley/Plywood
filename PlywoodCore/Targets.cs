using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Amazon.S3;
using Amazon.S3.Model;

namespace Plywood
{
    public class Targets : ControllerBase
    {
        public const string STR_TARGET_INDEX_PATH = ".targets.index";
        public const string STR_TARGETS_CONTAINER_PATH = "targets";

        public Targets() : base() { }
        public Targets(ControllerConfiguration context) : base(context) { }

        public void CreateTarget(Target target)
        {
            if (target == null)
                throw new ArgumentNullException("target", "Target cannot be null.");
            if (target.GroupKey == Guid.Empty)
                throw new ArgumentException("Group key cannot be empty.", "target.GroupKey");
            
            using (var stream = Serialisation.Serialise(target))
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        var groupsController = new Groups(Context);
                        if (!groupsController.GroupExists(target.GroupKey))
                            throw new GroupNotFoundException(String.Format("Group with the key \"{0}\" could not be found.", target.GroupKey));

                        var indexesController = new Internal.Indexes(Context);

                        string indexPath = GetGroupTargetsIndexPath(target.GroupKey);
                        var appIndex = indexesController.LoadIndex(indexPath);
                        if (appIndex.Entries.Any(e => e.Key == target.Key))
                        {
                            throw new DeploymentException("Index already contains entry for given key!");
                        }

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = string.Format("{0}/{1}/{2}", STR_TARGETS_CONTAINER_PATH, target.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }

                        appIndex.Entries.Add(new Internal.EntityIndexEntry() { Key = target.Key, Name = target.Name });
                        Internal.Indexes.NameSortIndex(appIndex);
                        indexesController.UpdateIndex(indexPath, appIndex);
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
                Plywood.Internal.AwsHelpers.SoftDeleteFolders(Context, string.Format("{0}/{1}", STR_TARGETS_CONTAINER_PATH, key.ToString("N")));

                var indexesController = new Internal.Indexes(Context);

                string indexPath = GetGroupTargetsIndexPath(target.GroupKey);
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
                            return Serialisation.ParseTarget(stream);
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

        public TargetList SearchGroupTargets(Guid groupKey, string query = null, int offset = 0, int pageSize = 50)
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
                var index = indexesController.LoadIndex(GetGroupTargetsIndexPath(groupKey));

                var filteredIndex = index.Entries.AsQueryable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var queryParts = query.ToLower().Split(new char[] { ' ', '\t', ',' }).Where(qp => !string.IsNullOrWhiteSpace(qp)).ToArray();
                    filteredIndex = filteredIndex.Where(e => queryParts.Any(q => e.Name.ToLower().Contains(q)));
                }

                var count = filteredIndex.Count();
                var listItems = filteredIndex.Skip(offset).Take(pageSize).Select(e => new TargetListItem() { Key = e.Key, Name = e.Name }).ToList();
                var list = new TargetList()
                {
                    GroupKey = groupKey,
                    Targets = listItems,
                    Query = query,
                    Offset = offset,
                    PageSize = pageSize,
                    TotalCount = count,
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
            
            using (var stream = Serialisation.Serialise(target))
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = string.Format("{0}/{1}/{2}", STR_TARGETS_CONTAINER_PATH, target.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }

                        var indexesController = new Internal.Indexes(Context);
                        string indexPath = GetGroupTargetsIndexPath(target.GroupKey);
                        indexesController.PutIndexEntry(indexPath, new Internal.EntityIndexEntry() { Key = target.Key, Name = target.Name });
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed updating target.", awsEx);
                }
            }
        }

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
                        Key = string.Format("{0}/{1}/{2}", STR_TARGETS_CONTAINER_PATH, key.ToString("N"), STR_INFO_FILE_NAME),
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
