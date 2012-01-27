﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
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
        public Versions(IStorageProvider provider) : base(provider) { }

        public void Create(Version version)
        {
            if (version == null)
                throw new ArgumentNullException("version", "Version cannot be null.");
            if (version.PackageKey == Guid.Empty)
                throw new ArgumentException("Package key cannot be empty.", "version.PackageKey");

            try
            {
                var packagesController = new Packages(StorageProvider);
                var package = packagesController.Get(version.PackageKey);
                version.GroupKey = package.GroupKey;

                using (var stream = version.Serialise())
                {
                    var indexEntries = new IndexEntries(StorageProvider);
                    // TODO: Check key and version number are unique.

                    StorageProvider.PutFile(Paths.GetVersionDetailsKey(version.Key), stream);

                    indexEntries.PutEntity(version);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed creating version.", ex);
            }
        }

        public void Delete(Guid key)
        {
            var version = Get(key);
            try
            {
                var indexEntries = new IndexEntries(StorageProvider);
                indexEntries.DeleteEntity(version);

                // TODO: Refactor the solf-delete functionality.
                StorageProvider.MoveFile(Paths.GetVersionDetailsKey(key), string.Concat("deleted/", Paths.GetVersionDetailsKey(key)));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed deleting version with key \"{0}\"", key), ex);
            }
        }

        public bool Exists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Paths.GetVersionDetailsKey(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed checking if version with key \"{0}\" exists.", key), ex);
            }
        }

        public Version Get(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetVersionDetailsKey(key)))
                {
                    return new Version(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting version with key \"{0}\"", key), ex);
            }
        }

        public void Pull(Guid key, DirectoryInfo directory, bool mergeExistingFiles = false)
        {
            if (!directory.Exists)
                throw new ArgumentException("Directory must exist.", "directory");
            if (!Exists(key))
                throw new VersionNotFoundException(string.Format("Could not find the version with key: {0}", key));
            if (!mergeExistingFiles)
            {
                if (directory.EnumerateFileSystemInfos().Any())
                    throw new ArgumentException("Target directory is not empty.");
            }

            try
            {
                var ignorePaths = new string[1] { ".info" };
                bool more = true;
                string lastResult = null;
                string prefix = string.Format("v/{0}/c/", key.ToString("N"));
                while (more)
                {
                    var listResponse = StorageProvider.ListFiles(prefix, lastResult, 100);
                    listResponse.Items.Where(obj => !ignorePaths.Any(ignore => obj == String.Format("{0}{1}", prefix, ignore)))
                        .AsParallel().ForAll(s3obj =>
                        {
                            using (var stream = StorageProvider.GetFile(s3obj))
                            {
                                using (var fileStream = File.Create(Utils.Files.GetLocalAbsolutePath(s3obj, prefix, directory.FullName)))
                                {
                                    stream.CopyTo(fileStream);
                                }
                            }
                        });
                    lastResult = listResponse.NextMarker;
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed pulling to version with key \"{0}\"", key), ex);
            }
        }

        public void Push(DirectoryInfo directory, Guid key)
        {
            if (!directory.Exists)
                throw new ArgumentException("Directory must exist.", "directory");

            try
            {
                var files = directory.EnumerateFiles("*", SearchOption.AllDirectories);
                files.AsParallel().ForAll(f =>
                    {
                        string relativePath = Utils.Files.GetRelativePath(f.FullName, directory.FullName);
                        // Skip dot (hidden) files.
                        if (!relativePath.StartsWith("."))
                        {
                            using (var stream = File.OpenRead(f.FullName))
                            {
                                StorageProvider.PutFile(string.Format("v/{0}/c/{1}", key.ToString("N"), relativePath), stream);
                            }
                        }
                    });
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed pushing to version with key \"{0}\"", key), ex);
            }
        }

        public VersionList Search(Guid packageKey, string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 0.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                IEnumerable<string> basePaths;

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var tokens = new SimpleTokeniser().Tokenise(query).ToList();

                    basePaths = tokens.Distinct().Select(token =>
                        string.Format("p/{0}/vi/t/{1}", Utils.Indexes.EncodeGuid(packageKey), Indexes.IndexEntries.GetTokenHash(token)));
                }
                else
                    basePaths = new List<string>() { string.Format("p/{0}/vi/e", Utils.Indexes.EncodeGuid(packageKey)) };

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var versions = rawResults.FileNames.Select(fileName => new VersionListItem(fileName));
                var list = new VersionList()
                {
                    PackageKey = packageKey,
                    Items = versions,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = versions.Any() ? versions.Last().Marker : marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed searching versions.", ex);
            }
        }

        public void Update(Version version)
        {
            if (version == null)
                throw new ArgumentNullException("version", "Version cannot be null.");
            if (version.Key == Guid.Empty)
                throw new ArgumentException("Version key cannot be empty.", "version.Key");

            var existingVersion = Get(version.Key);
            // Do not allow moving between apps & groups.
            version.PackageKey = existingVersion.PackageKey;
            version.GroupKey = existingVersion.GroupKey;

            using (var stream = version.Serialise())
            {
                try
                {
                    StorageProvider.PutFile(Paths.GetVersionDetailsKey(version.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.UpdateEntity(existingVersion, version);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed updating version.", ex);
                }
            }
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
            this.PackageKey = other.PackageKey;
            this.GroupKey = other.GroupKey;
            this.VersionNumber = other.VersionNumber;
            this.Comment = other.Comment;
            this.Timestamp = other.Timestamp;
            this.Tags = other.Tags;
        }

        #endregion

        public Guid Key { get; set; }
        public Guid PackageKey { get; set; }
        public Guid GroupKey { get; set; }
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
                    new XElement("packageKey", version.PackageKey),
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

            Guid key, groupKey, packageKey;
            DateTime localTimestamp;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised version key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised version group key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("packageKey").Value, out packageKey))
                throw new DeserialisationException("Serialised version package key is not a valid guid.");
            if (!DateTime.TryParse(doc.Root.Element("timestamp").Value, out localTimestamp))
                throw new DeserialisationException("Serialised version timestamp is not a valid datetime.");

            var version = new Version()
            {
                Key = key,
                GroupKey = groupKey,
                PackageKey = packageKey,
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

        public IEnumerable<string> GetIndexEntries()
        {
            var filename = string.Format("{0}-{1}-{2}-{3}-{4}",
                Hashing.CreateVersionHash(VersionNumber), Utils.Indexes.EncodeGuid(Key), Utils.Indexes.EncodeText(Comment), Utils.Indexes.EncodeText(VersionNumber), Utils.Indexes.EncodeText(Timestamp.ToString("s")));

            SimpleTokeniser tokeniser = new SimpleTokeniser();
            var tokens = tokeniser.Tokenise(Comment).ToList();
            tokens.AddRange((new VersionTokeniser()).Tokenise(VersionNumber));

            var entries = new List<string>(tokens.Count() + 1);

            // Package specific index
            entries.Add(string.Format("p/{0}/vi/e/{1}", Utils.Indexes.EncodeGuid(PackageKey), filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("p/{0}/vi/t/{1}/{2}", Utils.Indexes.EncodeGuid(PackageKey), Indexes.IndexEntries.GetTokenHash(token), filename)));

            return entries;
        }
    }

    public class VersionList
    {
        public Guid PackageKey { get; set; }
        public IEnumerable<VersionListItem> Items { get; set; }
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

            Marker = Utils.Indexes.GetIndexFileName(path);
            Key = Utils.Indexes.DecodeGuid(segments[1]);
            Comment = Utils.Indexes.DecodeText(segments[2]);
            VersionNumber = Utils.Indexes.DecodeText(segments[3]);
            Timestamp = DateTime.Parse(Utils.Indexes.DecodeText(segments[4]));
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public DateTime Timestamp { get; set; }
        public string VersionNumber { get; set; }
        public string Comment { get; set; }
    }

    #endregion
}