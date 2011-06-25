using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using Plywood.Utils;

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
                        var indexesController = new Internal.Indexes(Context);
                        var index = indexesController.LoadIndex(STR_GROUP_INDEX_PATH);
                        if (index.Entries.Any(e => e.Key == group.Key))
                        {
                            throw new DeploymentException("Index already contains entry for given key!");
                        }

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = string.Format("{0}/{1}/{2}", STR_GROUPS_CONTAINER_PATH, group.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }

                        index.Entries.Add(new Internal.EntityIndexEntry() { Key = group.Key, Name = group.Name });
                        Internal.Indexes.NameSortIndex(index);
                        indexesController.UpdateIndex(STR_GROUP_INDEX_PATH, index);
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed creating group.", awsEx);
                }
            }
        }

        public void DeleteGroup(Guid key)
        {
            try
            {
                Plywood.Internal.AwsHelpers.SoftDeleteFolders(Context, string.Format("{0}/{1}", STR_GROUPS_CONTAINER_PATH, key.ToString("N")));

                var indexesController = new Internal.Indexes(Context);
                indexesController.DeleteIndexEntry(STR_GROUP_INDEX_PATH, key);
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

        public GroupList SearchGroups(string query = null, int offset = 0, int pageSize = 50)
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
                var index = indexesController.LoadIndex(STR_GROUP_INDEX_PATH);

                var filteredIndex = index.Entries.AsQueryable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var queryParts = query.ToLower().Split(new char[] { ' ', '\t', ',' }).Where(qp => !string.IsNullOrWhiteSpace(qp)).ToArray();
                    filteredIndex = filteredIndex.Where(e => queryParts.Any(q => e.Name.ToLower().Contains(q)));
                }

                var count = filteredIndex.Count();
                var listItems = filteredIndex.Skip(offset).Take(pageSize).Select(e => new GroupListItem() { Key = e.Key, Name = e.Name }).ToList();
                var list = new GroupList()
                {
                    Groups = listItems,
                    Query = query,
                    Offset = offset,
                    PageSize = pageSize,
                    TotalCount = count,
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

                        var indexesController = new Internal.Indexes(Context);
                        indexesController.PutIndexEntry(STR_GROUP_INDEX_PATH, new Internal.EntityIndexEntry() { Key = group.Key, Name = group.Name });
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed updating group.", awsEx);
                }
            }
        }
    }
}
