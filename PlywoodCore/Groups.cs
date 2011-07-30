using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using Plywood.Utils;
using Plywood.Indexes;

namespace Plywood
{
    public class Groups : ControllerBase
    {
        public const string STR_GROUP_INDEX_PATH = ".groups.index";
        public const string STR_GROUPS_CONTAINER_PATH = "groups";

        public Groups() : base() { }
        public Groups(ControllerConfiguration context) : base(context) { }

        public void CreateGroup(Group group)
        {
            if (group == null)
                throw new ArgumentNullException("group", "Group cannot be null.");

            using (var stream = group.Serialise())
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = string.Format("{0}/{1}/{2}", STR_GROUPS_CONTAINER_PATH, group.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }
                    }

                    var indexEntries = new IndexEntries(Context);
                    indexEntries.PutIndexEntry(group.GetIndexEntry());
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed creating group.", awsEx);
                }
            }
        }

        public void DeleteGroup(Guid key)
        {
            var group = GetGroup(key);
            try
            {
                Plywood.Internal.AwsHelpers.SoftDeleteFolders(Context, string.Format("{0}/{1}", STR_GROUPS_CONTAINER_PATH, key.ToString("N")));

                var indexEntries = new IndexEntries(Context);
                indexEntries.DeleteIndexEntry(group.GetIndexEntry());
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed deleting group.", awsEx);
            }
        }

        public Group GetGroup(Guid key)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObject(new GetObjectRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = string.Format("{0}/{1}/{2}", STR_GROUPS_CONTAINER_PATH, key.ToString("N"), STR_INFO_FILE_NAME),
                    }))
                    {
                        using (var stream = res.ResponseStream)
                        {
                            return new Group(stream);
                        }
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                if (awsEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new GroupNotFoundException(string.Format("Could not find the group with key: {0}", key), awsEx);
                }
                else
                {
                    throw new DeploymentException(string.Format("Failed getting group with key \"{0}\"", key), awsEx);
                }
            }
        }

        public bool GroupExists(Guid key)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObjectMetadata(new GetObjectMetadataRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = string.Format("{0}/{1}/{2}", STR_GROUPS_CONTAINER_PATH, key.ToString("N"), STR_INFO_FILE_NAME),
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
                    throw new DeploymentException(string.Format("Failed getting group with key \"{0}\"", key), awsEx);
                }
            }
        }

        public GroupList SearchGroups(string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 1.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                var indexEntries = new IndexEntries(Context);
                IEnumerable<string> tokens = null;
                if (!string.IsNullOrWhiteSpace(query))
                    tokens = new SimpleTokeniser().Tokenise(query);
                var queryResults = indexEntries.QueryIndex(pageSize, marker, "gi", tokens);

                var list = new GroupList()
                {
                    Groups = queryResults.Results.Select(r => new GroupListItem()
                    {
                        Key = r.EntryKey,
                        Name = r.EntryText,
                    }),
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = queryResults.NextMarker,
                };

                return list;
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed searching groups.", awsEx);
            }
        }

        public void UpdateGroup(Group group)
        {
            if (group == null)
                throw new ArgumentNullException("group", "Group cannot be null.");

            var oldGroup = GetGroup(group.Key);

            using (var stream = group.Serialise())
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = string.Format("{0}/{1}/{2}", STR_GROUPS_CONTAINER_PATH, group.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }
                    }

                    var indexEntries = new IndexEntries(Context);
                    indexEntries.UpdateIndexEntry(oldGroup.GetIndexEntry(), group.GetIndexEntry());
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed updating group.", awsEx);
                }
            }
        }
    }
}
