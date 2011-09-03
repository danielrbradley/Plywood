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

                        var indexEntries = new IndexEntries(Context);

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = Paths.GetInstanceDetailsKey(instance.Key),
                            InputStream = stream,
                        })) { }

                        indexEntries.PutEntity(instance);
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

                var indexEntries = new IndexEntries(Context);
                indexEntries.DeleteEntity(instance);
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
                        Key = Paths.GetInstanceDetailsKey(instanceKey),
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
                        Key = Paths.GetInstanceDetailsKey(instanceKey),
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

        public InstanceList SearchInstances(Guid targetKey, string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 1.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                IEnumerable<string> basePaths;

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var tokens = new SimpleTokeniser().Tokenise(query).ToList();

                    basePaths = tokens.Distinct().Select(token =>
                        string.Format("t/{0}/ii/t/{1}", Utils.Indexes.EncodeGuid(targetKey), Indexes.IndexEntries.GetTokenHash(token)));
                }
                else
                    basePaths = new List<string>() { string.Format("t/{0}/ii/e", Utils.Indexes.EncodeGuid(targetKey)) };

                var indexEntries = new IndexEntries(Context);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var instances = rawResults.FileNames.Select(fileName => new InstanceListItem(fileName));
                var list = new InstanceList()
                {
                    Instances = instances,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = instances.Last().Marker,
                    IsTruncated = rawResults.IsTruncated,
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
                            Key = Paths.GetInstanceDetailsKey(updatedInstance.Key),
                            InputStream = stream,
                        })) { }

                        var indexEntries = new IndexEntries(Context);
                        indexEntries.UpdateEntity(existingInstance, updatedInstance);
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
