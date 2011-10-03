using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Plywood.Indexes;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Reflection;

namespace Plywood
{
    public class Packages : ControllerBase
    {
        public Packages(IStorageProvider provider) : base(provider) { }

        public void Create(Package package)
        {
            if (package == null)
                throw new ArgumentException("Package cannot be null.", "package");
            if (package.GroupKey == Guid.Empty)
                throw new ArgumentException("Group key cannot be empty.", "package.GroupKey");

            using (var stream = package.Serialise())
            {
                try
                {
                    var indexEntries = new IndexEntries(StorageProvider);
                    // TODO: Check key, name and deployment directory are unique.

                    StorageProvider.PutFile(Paths.GetPackageDetailsKey(package.Key), stream);

                    indexEntries.PutEntity(package);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating package.", ex);
                }
            }
        }

        public void Delete(Guid key)
        {
            var app = Get(key);
            try
            {
                var indexEntries = new IndexEntries(StorageProvider);

                indexEntries.DeleteEntity(app);

                // TODO: Refactor the solf-delete functionality.
                StorageProvider.MoveFile(Paths.GetPackageDetailsKey(key), string.Concat("deleted/", Paths.GetPackageDetailsKey(key)));
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting package.", ex);
            }
        }

        public bool Exists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Paths.GetPackageDetailsKey(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting package with key \"{0}\"", key), ex);
            }
        }

        public Package Get(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetPackageDetailsKey(key)))
                {
                    return new Package(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting package with key \"{0}\"", key), ex);
            }
        }

        public string PushRevision(Guid packageKey)
        {
            var package = Get(packageKey);
            var thisRevision = String.Format("{0}.{1}", package.MajorVersion, package.Revision);
            package.Revision += 1;
            Update(package);
            return thisRevision;
        }

        public PackageList Search(Guid? groupKey = null, string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 0.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                string[] startLocations;
                if (groupKey.HasValue)
                    startLocations = new string[2];
                else
                    startLocations = new string[1];
                startLocations[0] = "pi";
                if (groupKey.HasValue)
                    startLocations[1] = string.Format("g/{0}/pi", Utils.Indexes.EncodeGuid(groupKey.Value));

                IEnumerable<string> basePaths;

                if (!string.IsNullOrWhiteSpace(query))
                    basePaths = new SimpleTokeniser().Tokenise(query).SelectMany(token =>
                        startLocations.Select(l => string.Format("{0}/t/{1}", l, Indexes.IndexEntries.GetTokenHash(token))));
                else
                    basePaths = startLocations.Select(l => string.Format("{0}/e", l));

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var apps = rawResults.FileNames.Select(fileName => new PackageListItem(fileName));
                var list = new PackageList()
                {
                    Items = apps,
                    GroupKey = groupKey,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = apps.Any() ? apps.Last().Marker : marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed searcing packages.", ex);
            }
        }

        public void Update(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package", "Package cannot be null.");

            var existingPackage = Get(package.Key);
            // Don't allow moving between groups right now as would have to recursively update references from versions and targets within app.
            package.GroupKey = existingPackage.GroupKey;

            using (var stream = package.Serialise())
            {
                try
                {
                    // This will not currently get called.
                    if (existingPackage.GroupKey != package.GroupKey)
                    {
                        var groupsController = new Groups(StorageProvider);
                        if (!groupsController.Exists(package.GroupKey))
                            throw new GroupNotFoundException(string.Format("Group with key \"{0}\" to move package into cannot be found.", package.GroupKey));
                    }

                    StorageProvider.PutFile(Paths.GetPackageDetailsKey(package.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.UpdateEntity(existingPackage, package);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed updating package.", ex);
                }
            }
        }
    }

    #region Entities

    public class Package : IIndexableEntity
    {

        #region Constructors

        public Package()
        {
            Key = Guid.NewGuid();
            MajorVersion = "0.1";
            Tags = new Dictionary<string, string>();
        }

        public Package(string source)
            : this(Package.Parse(source)) { }

        public Package(Stream source)
            : this(Package.Parse(source)) { }

        public Package(TextReader source)
            : this(Package.Parse(source)) { }

        public Package(XmlReader source)
            : this(Package.Parse(source)) { }

        public Package(Package other)
        {
            this.DeploymentDirectory = other.DeploymentDirectory;
            this.GroupKey = other.GroupKey;
            this.Key = other.Key;
            this.MajorVersion = other.MajorVersion;
            this.Name = other.Name;
            this.Revision = other.Revision;
            this.Tags = other.Tags;
        }

        #endregion

        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public string DeploymentDirectory { get; set; }
        public string MajorVersion { get; set; }
        public int Revision { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public Stream Serialise()
        {
            return Package.Serialise(this);
        }

        #region Static Serialisation

        public static Package Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising package.", ex);
            }

            if (!ValidateXml(doc))
                throw new DeserialisationException("Serialised package xml is not valid.");

            Guid key, groupKey;
            int revision;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised package key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised package group key is not a valid guid.");
            if (!int.TryParse(doc.Root.Element("revision").Value, out revision))
                throw new DeserialisationException("Serialised package revision is not a valid integer.");

            var package = new Package()
            {
                Key = key,
                GroupKey = groupKey,
                Name = doc.Root.Element("name").Value,
                DeploymentDirectory = doc.Root.Element("deploymentDirectory").Value,
                MajorVersion = doc.Root.Element("majorVersion").Value,
                Revision = revision,
            };

            var tagsElement = doc.Root.Element("tags");
            if (tagsElement != null && tagsElement.HasElements)
            {
                package.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }

            return package;
        }

        public static Package Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Package Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Package Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Stream Serialise(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package", "Package cannot be null.");
            if (!Validation.IsNameValid(package.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");
            if (!Validation.IsDirectoryNameValid(package.DeploymentDirectory))
                throw new ArgumentException("Deployment directory must be a valid directory name.");
            if (!Validation.IsMajorVersionValid(package.MajorVersion))
                throw new ArgumentException("Major version must be numbers separated by '.'");
            if (package.Revision < 0)
                throw new ArgumentOutOfRangeException("package.Revision", package.Revision, "Package revision must be a positive integer.");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("package",
                    new XAttribute("key", package.Key),
                    new XElement("groupKey", package.GroupKey),
                    new XElement("name", package.Name),
                    new XElement("deploymentDirectory", package.DeploymentDirectory),
                    new XElement("majorVersion", package.MajorVersion),
                    new XElement("revision", package.Revision),
                    new XElement("tags")));

            if (package.Tags != null && package.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    package.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static bool ValidateXml(XDocument appDoc)
        {
            bool valid = true;
            appDoc.Validate(Schemas, (o, e) =>
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
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.Package.xsd"))
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
            var filename = string.Format("{0}-{1}-{2}-{3}",
                Hashing.CreateHash(Name), Utils.Indexes.EncodeGuid(Key), Utils.Indexes.EncodeText(Name), Utils.Indexes.EncodeText(MajorVersion));

            var tokens = (new SimpleTokeniser()).Tokenise(Name).ToList();
            var entries = new List<string>(tokens.Count() + 1);

            // TODO: Could also index the package major version numbers?

            // Group specific index
            entries.Add(string.Format("g/{0}/pi/e/{1}", Utils.Indexes.EncodeGuid(GroupKey), filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("g/{0}/pi/t/{1}/{2}", Utils.Indexes.EncodeGuid(GroupKey), Indexes.IndexEntries.GetTokenHash(token), filename)));

            // Global index
            entries.Add(string.Format("pi/e/{0}", filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("pi/t/{0}/{1}", Indexes.IndexEntries.GetTokenHash(token), filename)));

            return entries;
        }
    }

    public class PackageList
    {
        public Guid? GroupKey { get; set; }
        public IEnumerable<PackageListItem> Items { get; set; }
        public string Query { get; set; }
        public string Marker { get; set; }
        public int PageSize { get; set; }
        public string NextMarker { get; set; }
        public bool IsTruncated { get; set; }
    }

    public class PackageListItem
    {
        public PackageListItem()
        {
        }

        public PackageListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 4)
                throw new ArgumentException("A package path index entry must contain exactly 4 segments.", "path");

            Marker = Utils.Indexes.GetIndexFileName(path);
            Key = Utils.Indexes.DecodeGuid(segments[1]);
            Name = Utils.Indexes.DecodeText(segments[2]);
            MajorVersion = Utils.Indexes.DecodeText(segments[3]);
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; }
        public string MajorVersion { get; set; }
    }

    #endregion
}
