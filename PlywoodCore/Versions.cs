using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using Plywood.Indexes;
using System.Xml.Schema;
using System.Xml;
using System.Reflection;
using System.Xml.Linq;

namespace Plywood
{
    public class Versions : ControllerBase
    {
        [Obsolete]
        public const string STR_VERSION_INDEX_PATH = ".versions.index";
        [Obsolete]
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

                    using (var stream = version.Serialise())
                    {
                        var indexEntries = new IndexEntries(Context);
                        // TODO: Check key and version number are unique.

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = Paths.GetVersionDetailsKey(version.Key),
                            InputStream = stream,
                        })) { }

                        indexEntries.PutEntity(version);
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
                Plywood.Internal.AwsHelpers.SoftDeleteFolders(Context, Paths.GetVersionDetailsKey(version.Key));

                var indexEntries = new IndexEntries(Context);

                string indexPath = GetAppVersionsIndexPath(version.AppKey);

                indexEntries.DeleteEntity(version);
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
                        Key = Paths.GetVersionDetailsKey(key),
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
                    string prefix = string.Format("v/{0}/c/", key.ToString("N"));
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
                                    Key = string.Format("v/{0}/c/{1}", key.ToString("N"), relativePath),
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

        public VersionList SearchAppVersions(Guid appKey, string query = null, string marker = null, int pageSize = 50)
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
                    var versionTokeniser = new VersionTokeniser();
                    // Tokenise any tokens that look like versions
                    tokens.AddRange(tokens.Where(t => Utils.Validation.IsMajorVersionValid(t)).SelectMany(t => versionTokeniser.Tokenise(t)));

                    basePaths = tokens.Distinct().Select(token =>
                        string.Format("a/{0}/vi/t/{1}", Utils.Indexes.EncodeGuid(appKey), Indexes.IndexEntries.GetTokenHash(token)));
                }
                else
                    basePaths = new List<string>() { string.Format("a/{0}/vi/e", Utils.Indexes.EncodeGuid(appKey)) };

                var indexEntries = new IndexEntries(Context);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var apps = rawResults.FileNames.Select(fileName => new VersionListItem(fileName));
                var list = new VersionList()
                {
                    Versions = apps,
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
                        var indexEntries = new IndexEntries(Context);

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = Paths.GetVersionDetailsKey(version.Key),
                            InputStream = stream,
                        })) { }

                        indexEntries.UpdateEntity(existingVersion, version);
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
                        Key = Paths.GetVersionDetailsKey(key),
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

        [Obsolete]
        public static string GetAppVersionsIndexPath(Guid appKey)
        {
            return string.Format("{0}/{1}/{2}", Apps.STR_APPS_CONTAINER_PATH, appKey.ToString("N"), STR_VERSION_INDEX_PATH);
        }

        [Obsolete]
        public static string CreateVersionIndexName(Version version)
        {
            return String.Format("{0:s} {1} {2}", version.Timestamp, version.VersionNumber, version.Comment);
        }

    }

    #region Entities

    public class Version : IIndexableEntity
    {
        #region Constructors

        public Version()
        {
            Key = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            Tags = new Dictionary<string, string>();
        }

        public Version(string source)
            : this(Version.Parse(source)) { }

        public Version(Stream source)
            : this(Version.Parse(source)) { }

        public Version(TextReader source)
            : this(Version.Parse(source)) { }

        public Version(XmlTextReader source)
            : this(Version.Parse(source)) { }

        private Version(Version other)
        {
            this.Key = other.Key;
            this.AppKey = other.AppKey;
            this.GroupKey = other.GroupKey;
            this.VersionNumber = other.VersionNumber;
            this.Comment = other.Comment;
            this.Timestamp = other.Timestamp;
            this.Tags = other.Tags;
        }

        #endregion

        public Guid Key { get; set; }
        public Guid AppKey { get; set; }
        public Guid GroupKey { get; set; }
        [Obsolete]
        public string Name { get { return string.Format("{0} {1}", this.VersionNumber, this.Comment); } }
        public string VersionNumber { get; set; }
        public string Comment { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public Stream Serialise()
        {
            return Version.Serialise(this);
        }

        #region Static Serialisation Methods

        public static Stream Serialise(Version version)
        {
            if (version == null)
                throw new ArgumentNullException("version", "Version cannot be null.");
            if (!Validation.IsNameValid(version.Comment))
                throw new ArgumentException("Comment must be valid (not blank & only a single line).");
            if (!Validation.IsMajorVersionValid(version.VersionNumber))
                throw new ArgumentException("Version number must be numbers separated by dots (.)");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("version",
                    new XAttribute("key", version.Key),
                    new XElement("groupKey", version.GroupKey),
                    new XElement("appKey", version.AppKey),
                    new XElement("timestamp", version.Timestamp),
                    new XElement("versionNumber", version.VersionNumber),
                    new XElement("comment", version.Comment),
                    new XElement("tags")));

            if (version.Tags != null && version.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    version.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static Version Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Version Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Version Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Version Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising version.", ex);
            }

            if (!ValidateVersionXml(doc))
                throw new DeserialisationException("Serialised version xml is not valid.");

            Guid key, groupKey, appKey;
            DateTime localTimestamp;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised version key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised version group key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("appKey").Value, out appKey))
                throw new DeserialisationException("Serialised version app key is not a valid guid.");
            if (!DateTime.TryParse(doc.Root.Element("timestamp").Value, out localTimestamp))
                throw new DeserialisationException("Serialised version timestamp is not a valid datetime.");

            var version = new Version()
            {
                Key = key,
                GroupKey = groupKey,
                AppKey = appKey,
                Timestamp = localTimestamp.ToUniversalTime(),
                VersionNumber = doc.Root.Element("versionNumber").Value,
                Comment = doc.Root.Element("comment").Value,
            };

            var tagsElement = doc.Root.Element("tags");
            if (tagsElement != null && tagsElement.HasElements)
            {
                version.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }
            else
            {
                version.Tags = new Dictionary<string, string>();
            }

            return version;
        }

        public static bool ValidateVersionXml(XDocument targetDoc)
        {
            bool valid = true;
            targetDoc.Validate(Schemas, (o, e) =>
            {
                valid = false;
            });
            return valid;
        }

        public static XmlSchemaSet Schemas
        {
            get
            {
                if (schemas == null)
                {
                    lock (schemasLock)
                    {
                        if (schemas == null)
                        {
                            var newSchemas = new XmlSchemaSet();
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.Version.xsd"))
                            {
                                newSchemas.Add("", XmlReader.Create(stream));
                            }
                            schemas = newSchemas;
                        }
                    }
                }
                return schemas;
            }
        }

        private static XmlSchemaSet schemas;
        private static object schemasLock = new object();

        #endregion

        [Obsolete]
        internal Indexes.IndexEntry GetIndexEntry()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetIndexEntries()
        {
            var filename = string.Format("{0}-{1}-{2}-{3}-{4}",
                Hashing.CreateVersionHash(VersionNumber), Utils.Indexes.EncodeGuid(Key), Utils.Indexes.EncodeText(Comment), Utils.Indexes.EncodeText(VersionNumber), Utils.Indexes.EncodeText(Timestamp.ToString("s")));

            SimpleTokeniser tokeniser = new SimpleTokeniser();
            var tokens = tokeniser.Tokenise(Comment).ToList();
            tokens.AddRange((new VersionTokeniser()).Tokenise(VersionNumber));

            var entries = new List<string>(tokens.Count() + 1);

            // App specific index
            entries.Add(string.Format("a/{0}/vi/e/{1}", Utils.Indexes.EncodeGuid(AppKey), filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("a/{0}/vi/t/{1}/{2}", Utils.Indexes.EncodeGuid(AppKey), Indexes.IndexEntries.GetTokenHash(token), filename)));

            return entries;
        }
    }

    public class VersionList
    {
        public Guid AppKey { get; set; }
        public IEnumerable<VersionListItem> Versions { get; set; }
        public string Query { get; set; }
        public string Marker { get; set; }
        public int PageSize { get; set; }
        public string NextMarker { get; set; }
        public bool IsTruncated { get; set; }
    }

    public class VersionListItem
    {
        public VersionListItem()
        {
        }

        public VersionListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 5)
                throw new ArgumentException("A version path index entry must contain exactly 5 segments.", "path");

            Marker = segments[0];
            Key = Utils.Indexes.DecodeGuid(segments[1]);
            Comment = Utils.Indexes.DecodeText(segments[2]);
            VersionNumber = Utils.Indexes.DecodeText(segments[3]);
            Timestamp = DateTime.Parse(Utils.Indexes.DecodeText(segments[4]));
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public DateTime Timestamp { get; set; }
        [Obsolete]
        public string Name { get { return string.Format("{0} {1}", this.VersionNumber, this.Comment); } }
        public string VersionNumber { get; set; }
        public string Comment { get; set; }
    }

    #endregion
}
