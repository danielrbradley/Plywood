using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Amazon.S3;
using Amazon.S3.Model;

namespace Plywood
{
    public class Instances : ControllerBase
    {
        [Obsolete]
        public const string STR_INSTANCE_INDEX_PATH = ".instances.index";
        [Obsolete]
        public const string STR_INSTANCES_CONTAINER_PATH = "instances";

        public Instances() : base() { }
        public Instances(ControllerConfiguration context) : base(context) { }

        public void CreateInstance(Instance instance)
        {
            if (instance == null)
                throw new ArgumentNullException("instance", "Instance cannot be null.");
            if (instance.TargetKey == Guid.Empty)
                throw new ArgumentException("Target key cannot be empty.", "instance.TargetKey");

            using (var stream = instance.Serialise())
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        var targetsController = new Targets(Context);
                        if (!targetsController.TargetExists(instance.TargetKey))
                            throw new TargetNotFoundException(String.Format("Target with the key \"{0}\" could not be found.", instance.TargetKey));

                        var indexesController = new Internal.Indexes(Context);

                        string indexPath = GetTargetInstancesIndexPath(instance.TargetKey);
                        var instanceIndex = indexesController.LoadIndex(indexPath);
                        if (instanceIndex.Entries.Any(e => e.Key == instance.Key))
                        {
                            throw new DeploymentException("Target instances index already contains entry for new instance key!");
                        }

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = string.Format("{0}/{1}/{2}", STR_INSTANCES_CONTAINER_PATH, instance.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }

                        instanceIndex.Entries.Add(new Internal.EntityIndexEntry() { Key = instance.Key, Name = instance.Name });
                        Internal.Indexes.NameSortIndex(instanceIndex);
                        indexesController.UpdateIndex(indexPath, instanceIndex);
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed creating instance.", awsEx);
                }
            }
        }

        public void DeleteInstance(Guid instanceKey)
        {
            var instance = GetInstance(instanceKey);
            try
            {
                Plywood.Internal.AwsHelpers.SoftDeleteFolders(Context, string.Format("{0}/{1}", STR_INSTANCES_CONTAINER_PATH, instanceKey.ToString("N")));

                var indexesController = new Internal.Indexes(Context);

                string indexPath = GetTargetInstancesIndexPath(instance.TargetKey);
                var appIndex = indexesController.LoadIndex(indexPath);
                if (appIndex.Entries.Any(e => e.Key == instanceKey))
                {
                    appIndex.Entries.Remove(appIndex.Entries.Single(e => e.Key == instanceKey));
                    Internal.Indexes.NameSortIndex(appIndex);
                    indexesController.UpdateIndex(indexPath, appIndex);
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed deleting instance.", awsEx);
            }
        }

        public bool InstanceExists(Guid instanceKey)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObjectMetadata(new GetObjectMetadataRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = string.Format("{0}/{1}/{2}", STR_INSTANCES_CONTAINER_PATH, instanceKey.ToString("N"), STR_INFO_FILE_NAME),
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
                    throw new DeploymentException(string.Format("Failed getting instance with key \"{0}\"", instanceKey), awsEx);
                }
            }
        }

        public Instance GetInstance(Guid instanceKey)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObject(new GetObjectRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = string.Format("{0}/{1}/{2}", STR_INSTANCES_CONTAINER_PATH, instanceKey.ToString("N"), STR_INFO_FILE_NAME),
                    }))
                    {
                        using (var stream = res.ResponseStream)
                        {
                            return new Instance(stream);
                        }
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                if (awsEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new InstanceNotFoundException(string.Format("Could not find the instance with key: {0}", instanceKey), awsEx);
                }
                else
                {
                    throw new DeploymentException(string.Format("Failed getting instance with key \"{0}\"", instanceKey), awsEx);
                }
            }
        }

        public InstanceList SearchInstances(Guid targetKey, string query = null, int offset = 0, int pageSize = 50)
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
                var index = indexesController.LoadIndex(GetTargetInstancesIndexPath(targetKey));

                var filteredIndex = index.Entries.AsQueryable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var queryParts = query.ToLower().Split(new char[] { ' ', '\t', ',' }).Where(qp => !string.IsNullOrWhiteSpace(qp)).ToArray();
                    filteredIndex = filteredIndex.Where(e => queryParts.Any(q => e.Name.ToLower().Contains(q)));
                }

                var count = filteredIndex.Count();
                var listItems = filteredIndex.Skip(offset).Take(pageSize).Select(e => new InstanceListItem() { Key = e.Key, Name = e.Name }).ToList();
                var list = new InstanceList()
                {
                    TargetKey = targetKey,
                    Instances = listItems,
                    Query = query,
                    Offset = offset,
                    PageSize = pageSize,
                    TotalCount = count,
                };

                return list;
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed searcing instances.", awsEx);
            }
        }

        public void UpdateInstance(Instance updatedInstance)
        {
            if (updatedInstance == null)
                throw new ArgumentNullException("updatedInstance", "Instance cannot be null.");

            var existingInstance = GetInstance(updatedInstance.Key);
            // Don't allow moving between targets.
            updatedInstance.TargetKey = existingInstance.TargetKey;

            using (var stream = updatedInstance.Serialise())
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = string.Format("{0}/{1}/{2}", STR_INSTANCES_CONTAINER_PATH, updatedInstance.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }

                        var indexesController = new Internal.Indexes(Context);
                        string indexPath = GetTargetInstancesIndexPath(updatedInstance.TargetKey);
                        indexesController.PutIndexEntry(indexPath, new Internal.EntityIndexEntry() { Key = updatedInstance.Key, Name = updatedInstance.Name });
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed updating instance.", awsEx);
                }
            }
        }

        private string GetTargetInstancesIndexPath(Guid targetKey)
        {
            return string.Format("{0}/{1}/{2}", Targets.STR_TARGETS_CONTAINER_PATH, targetKey.ToString("N"), STR_INSTANCE_INDEX_PATH);
        }
    }
}
