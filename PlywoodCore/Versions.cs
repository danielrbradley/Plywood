using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;

namespace Plywood
{
    public class Versions : ControllerBase
    {
        public const string STR_VERSION_INDEX_PATH = ".versions.index";
        public const string STR_VERSIONS_CONTAINER_PATH = "versions";

        public Versions() : base() { }
        public Versions(ControllerConfiguration context) : base(context) { }

        public void CreateVersion(Version version)
        {
            if (version == null)
                throw new ArgumentNullException("version", "Version cannot be null.");
            if (version.AppKey == Guid.Empty)
                throw new ArgumentException("App key cannot be empty.", "version.AppKey");

            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    var appsController = new Apps(Context);
                    var app = appsController.GetApp(version.AppKey);
                    version.GroupKey = app.GroupKey;

                    var indexesController = new Internal.Indexes(Context);

                    using (var stream = version.Serialise())
                    {
                        string indexPath = GetAppVersionsIndexPath(version.AppKey);
                        var index = indexesController.LoadIndex(indexPath);
                        if (index.Entries.Any(e => e.Key == version.Key))
                        {
                            throw new DeploymentException("Index already contains entry for given key!");
                        }

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = string.Format("{0}/{1}/{2}", STR_VERSIONS_CONTAINER_PATH, version.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }

                        index.Entries.Add(new Internal.EntityIndexEntry() { Key = version.Key, Name = CreateVersionIndexName(version) });
                        Internal.Indexes.NameSortIndex(index, true);
                        indexesController.UpdateIndex(indexPath, index);
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed creating version.", awsEx);
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed creating version.", ex);
            }
        }

        public void DeleteVersion(Guid key)
        {
            var version = GetVersion(key);
            try
            {
                Plywood.Internal.AwsHelpers.SoftDeleteFolders(Context, string.Format("{0}/{1}", STR_VERSIONS_CONTAINER_PATH, key.ToString("N")));

                var indexesController = new Internal.Indexes(Context);

                string indexPath = GetAppVersionsIndexPath(version.AppKey);
                var appIndex = indexesController.LoadIndex(indexPath);
                if (appIndex.Entries.Any(e => e.Key == key))
                {
                    appIndex.Entries.Remove(appIndex.Entries.Single(e => e.Key == key));
                    Internal.Indexes.NameSortIndex(appIndex, true);
                    indexesController.UpdateIndex(indexPath, appIndex);
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException(string.Format("Failed deleting version with key \"{0}\"", key), awsEx);
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed deleting version with key \"{0}\"", key), ex);
            }
        }

        public Version GetVersion(Guid key)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObject(new GetObjectRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = string.Format("{0}/{1}/{2}", STR_VERSIONS_CONTAINER_PATH, key.ToString("N"), STR_INFO_FILE_NAME),
                    }))
                    {
                        using (var stream = res.ResponseStream)
                        {
                            return new Version(stream);
                        }
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                if (awsEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new VersionNotFoundException(string.Format("Could not find the version with key: {0}", key), awsEx);
                }
                else
                {
                    throw new DeploymentException(string.Format("Failed getting version with key \"{0}\"", key), awsEx);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting version with key \"{0}\"", key), ex);
            }
        }

        public void PullVersion(Guid key, DirectoryInfo directory, bool mergeExistingFiles = false)
        {
            if (!directory.Exists)
                throw new ArgumentException("Directory must exist.", "directory");
            if (!VersionExists(key))
                throw new VersionNotFoundException(string.Format("Could not find the version with key: {0}", key));
            if (!mergeExistingFiles)
            {
                if (directory.EnumerateFileSystemInfos().Any())
                    throw new ArgumentException("Target directory is not empty.");
            }

            try
            {
                var ignorePaths = new string[1] { ".info" };
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    bool more = true;
                    string lastResult = null;
                    string prefix = string.Format("{0}/{1}/", STR_VERSIONS_CONTAINER_PATH, key.ToString("N"));
                    while (more)
                    {
                        using (var listResponse = client.ListObjects(new ListObjectsRequest()
                        {
                            BucketName = Context.BucketName,
                            Prefix = prefix,
                            Delimiter = lastResult,
                        }))
                        {
                            listResponse.S3Objects.Where(obj => !ignorePaths.Any(ignore => obj.Key == String.Format("{0}{1}", prefix, ignore)))
                                .AsParallel().ForAll(s3obj =>
                                {
                                    using (var getResponse = client.GetObject(new GetObjectRequest()
                                    {
                                        BucketName = Context.BucketName,
                                        Key = s3obj.Key,
                                    }))
                                    {
                                        getResponse.WriteResponseStreamToFile(Utils.Files.GetLocalAbsolutePath(s3obj.Key, prefix, directory.FullName));
                                    }
                                });
                            if (listResponse.IsTruncated)
                            {
                                more = true;
                            }
                            more = listResponse.IsTruncated;
                            lastResult = listResponse.S3Objects.Last().Key;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed pushing to version with key \"{0}\"", key), ex);
            }
        }

        public void PushVersion(DirectoryInfo directory, Guid key)
        {
            if (!directory.Exists)
                throw new ArgumentException("Directory must exist.", "directory");

            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    var files = directory.EnumerateFiles("*", SearchOption.AllDirectories);
                    files.AsParallel().ForAll(f =>
                        {
                            string relativePath = Utils.Files.GetRelativePath(f.FullName, directory.FullName);
                            // Skip dot (hidden) files.
                            if (!relativePath.StartsWith("."))
                            {
                                using (var putResponse = client.PutObject(new PutObjectRequest()
                                {
                                    BucketName = Context.BucketName,
                                    Key = string.Format("{0}/{1}/{2}", STR_VERSIONS_CONTAINER_PATH, key.ToString("N"), relativePath),
                                    FilePath = f.FullName,
                                    GenerateMD5Digest = true,
                                })) { }
                            }
                        });
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed pushing to version with key \"{0}\"", key), ex);
            }
        }

        public VersionList SearchAppVersions(Guid appKey, DateTime? fromDate = null, DateTime? toDate = null, string query = null, int offset = 0, int pageSize = 50)
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
                var index = indexesController.LoadIndex(GetAppVersionsIndexPath(appKey));

                var filteredIndex = index.Entries.AsQueryable();

                if (fromDate != null)
                {
                    filteredIndex = filteredIndex.Where(e => DateTime.Parse(e.Name.Substring(0, e.Name.IndexOf(' '))) >= fromDate);
                }
                if (toDate != null)
                {
                    filteredIndex = filteredIndex.Where(e => DateTime.Parse(e.Name.Substring(0, e.Name.IndexOf(' '))) <= toDate);
                }

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var queryParts = query.ToLower().Split(new char[] { ' ', '\t', ',' }).Where(qp => !string.IsNullOrWhiteSpace(qp)).ToArray();
                    filteredIndex = filteredIndex.Where(e => queryParts.Any(q => e.Name.ToLower().Contains(q)));
                }

                var count = filteredIndex.Count();
                var listItems = filteredIndex.Skip(offset).Take(pageSize).Select(e =>
                    new VersionListItem()
                    {
                        Key = e.Key,
                        Timestamp = DateTime.Parse(e.Name.Substring(0, e.Name.IndexOf(' '))),
                        VersionNumber = e.Name.Substring(e.Name.IndexOf(' ') + 1, e.Name.IndexOf(' ', e.Name.IndexOf(' ') + 1) - (e.Name.IndexOf(' ') + 1)),
                        Comment = e.Name.Substring(e.Name.IndexOf(' ', e.Name.IndexOf(' ') + 1) + 1),
                    }).ToList();

                var list = new VersionList()
                {
                    AppKey = appKey,
                    Versions = listItems,
                    Offset = offset,
                    PageSize = pageSize,
                    TotalCount = count,
                };

                return list;
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed searcing groups.", awsEx);
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed searcing groups.", ex);
            }
        }

        public void UpdateVersion(Version version)
        {
            if (version == null)
                throw new ArgumentNullException("version", "Version cannot be null.");
            if (version.Key == Guid.Empty)
                throw new ArgumentException("Version key cannot be empty.", "version.Key");
            // Disabled these checks as we automatically resolve them for now.
            //if (version.AppKey == Guid.Empty)
            //    throw new ArgumentException("Version app key cannot be empty.", "version.AppKey");
            //if (version.GroupKey == Guid.Empty)
            //    throw new ArgumentException("Version group key cannot be empty.", "version.GroupKey");

            var existingVersion = GetVersion(version.Key);
            // Do not allow moving between apps & groups.
            version.AppKey = existingVersion.AppKey;
            version.GroupKey = existingVersion.GroupKey;

            using (var stream = version.Serialise())
            {
                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        var indexesController = new Internal.Indexes(Context);

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = string.Format("{0}/{1}/{2}", STR_VERSIONS_CONTAINER_PATH, version.Key.ToString("N"), STR_INFO_FILE_NAME),
                            InputStream = stream,
                        })) { }

                        string indexPath = GetAppVersionsIndexPath(version.AppKey);
                        indexesController.PutIndexEntry(indexPath, new Internal.EntityIndexEntry() { Key = version.Key, Name = CreateVersionIndexName(version) }, true);
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed deleting version.", awsEx);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed deleting version.", ex);
                }
            }
        }

        internal bool VersionExists(Guid key)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObjectMetadata(new GetObjectMetadataRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = string.Format("{0}/{1}/{2}", STR_VERSIONS_CONTAINER_PATH, key.ToString("N"), STR_INFO_FILE_NAME),
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
                    throw new DeploymentException(string.Format("Failed checking if version with key \"{0}\" exists.", key), awsEx);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed checking if version with key \"{0}\" exists.", key), ex);
            }
        }

        public static string GetAppVersionsIndexPath(Guid appKey)
        {
            return string.Format("{0}/{1}/{2}", Apps.STR_APPS_CONTAINER_PATH, appKey.ToString("N"), STR_VERSION_INDEX_PATH);
        }

        public static string CreateVersionIndexName(Version version)
        {
            return String.Format("{0:s} {1} {2}", version.Timestamp, version.VersionNumber, version.Comment);
        }

    }
}
