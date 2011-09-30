using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Amazon.S3;
using Amazon.S3.Model;
using Plywood.Indexes;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
using System.Reflection;

namespace Plywood
{
    public class Apps : ControllerBase
    {
        public Apps(IStorageProvider provider) : base(provider) { }

        internal bool AppExists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Paths.GetAppDetailsKey(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting app with key \"{0}\"", key), ex);
            }
        }

        public void CreateApp(App app)
        {
            if (app == null)
                throw new ArgumentException("App cannot be null.", "app");
            if (app.GroupKey == Guid.Empty)
                throw new ArgumentException("Group key cannot be empty.", "app.GroupKey");

            using (var stream = app.Serialise())
            {
                try
                {
                    var indexEntries = new IndexEntries(StorageProvider);
                    // TODO: Check key, name and deployment directory are unique.

                    StorageProvider.PutFile(Paths.GetAppDetailsKey(app.Key), stream);

                    indexEntries.PutEntity(app);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating app.", ex);
                }
            }
        }

        public void DeleteApp(Guid key)
        {
            var app = GetApp(key);
            try
            {
                var indexEntries = new IndexEntries(StorageProvider);

                indexEntries.DeleteEntity(app);

                // TODO: Refactor the solf-delete functionality.
                StorageProvider.MoveFile(Paths.GetAppDetailsKey(key), string.Concat(".recycled/", Paths.GetAppDetailsKey(key)));
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting app.", ex);
            }
        }

        public App GetApp(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetAppDetailsKey(key)))
                {
                    return new App(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting app with key \"{0}\"", key), ex);
            }
        }

        public string PushAppRevision(Guid appKey)
        {
            var app = GetApp(appKey);
            var thisRevision = String.Format("{0}.{1}", app.MajorVersion, app.Revision);
            app.Revision += 1;
            UpdateApp(app);
            return thisRevision;
        }

        public AppList SearchApps(Guid? groupKey = null, string query = null, string marker = null, int pageSize = 50)
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
                startLocations[0] = "ai";
                if (groupKey.HasValue)
                    startLocations[1] = string.Format("g/{0}/ai", Utils.Indexes.EncodeGuid(groupKey.Value));

                IEnumerable<string> basePaths;

                if (!string.IsNullOrWhiteSpace(query))
                    basePaths = new SimpleTokeniser().Tokenise(query).SelectMany(token =>
                        startLocations.Select(l => string.Format("{0}/t/{1}", l, Indexes.IndexEntries.GetTokenHash(token))));
                else
                    basePaths = startLocations.Select(l => string.Format("{0}/e", l));

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var apps = rawResults.FileNames.Select(fileName => new AppListItem(fileName));
                var list = new AppList()
                {
                    Apps = apps,
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
                throw new DeploymentException("Failed searcing apps.", ex);
            }
        }

        public void UpdateApp(App app)
        {
            if (app == null)
                throw new ArgumentNullException("app", "App cannot be null.");

            var existingApp = GetApp(app.Key);
            // Don't allow moving between groups right now as would have to recursively update references from versions and targets within app.
            app.GroupKey = existingApp.GroupKey;

            using (var stream = app.Serialise())
            {
                try
                {
                    // This will not currently get called.
                    if (existingApp.GroupKey != app.GroupKey)
                    {
                        var groupsController = new Groups(StorageProvider);
                        if (!groupsController.GroupExists(app.GroupKey))
                            throw new GroupNotFoundException(string.Format("Group with key \"{0}\" to move app into cannot be found.", app.GroupKey));
                    }

                    StorageProvider.PutFile(Paths.GetAppDetailsKey(app.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.UpdateEntity(existingApp, app);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed updating app.", ex);
                }
            }
        }
    }

    #region Entities

    public class App : IIndexableEntity
    {

        #region Constructors

        public App()
        {
            Key = Guid.NewGuid();
            MajorVersion = "0.1";
            Tags = new Dictionary<string, string>();
        }

        public App(string source)
            : this(App.Parse(source)) { }

        public App(Stream source)
            : this(App.Parse(source)) { }

        public App(TextReader source)
            : this(App.Parse(source)) { }

        public App(XmlReader source)
            : this(App.Parse(source)) { }

        public App(App other)
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
            return App.Serialise(this);
        }

        #region Static Serialisation

        public static App Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising app.", ex);
            }

            if (!ValidateAppXml(doc))
                throw new DeserialisationException("Serialised app xml is not valid.");

            Guid key, groupKey;
            int revision;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised app key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised app group key is not a valid guid.");
            if (!int.TryParse(doc.Root.Element("revision").Value, out revision))
                throw new DeserialisationException("Serialised app revision is not a valid integer.");

            var app = new App()
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
                app.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }

            return app;
        }

        public static App Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static App Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static App Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Stream Serialise(App app)
        {
            if (app == null)
                throw new ArgumentNullException("app", "App cannot be null.");
            if (!Validation.IsNameValid(app.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");
            if (!Validation.IsDirectoryNameValid(app.DeploymentDirectory))
                throw new ArgumentException("Deployment directory must be a valid directory name.");
            if (!Validation.IsMajorVersionValid(app.MajorVersion))
                throw new ArgumentException("Major version must be numbers separated by '.'");
            if (app.Revision < 0)
                throw new ArgumentOutOfRangeException("app.Revision", app.Revision, "App revision must be a positive integer.");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("app",
                    new XAttribute("key", app.Key),
                    new XElement("groupKey", app.GroupKey),
                    new XElement("name", app.Name),
                    new XElement("deploymentDirectory", app.DeploymentDirectory),
                    new XElement("majorVersion", app.MajorVersion),
                    new XElement("revision", app.Revision),
                    new XElement("tags")));

            if (app.Tags != null && app.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    app.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static bool ValidateAppXml(XDocument appDoc)
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
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.App.xsd"))
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
                Hashing.CreateHash(Name), Utils.Indexes.EncodeGuid(Key), Utils.Indexes.EncodeGuid(GroupKey), Utils.Indexes.EncodeText(Name), Utils.Indexes.EncodeText(MajorVersion));

            var tokens = (new SimpleTokeniser()).Tokenise(Name).ToList();
            var entries = new List<string>(tokens.Count() + 1);

            // Group specific index
            entries.Add(string.Format("g/{0}/ai/e/{1}", Utils.Indexes.EncodeGuid(GroupKey), filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("g/{0}/ai/t/{1}/{2}", Utils.Indexes.EncodeGuid(GroupKey), Indexes.IndexEntries.GetTokenHash(token), filename)));

            // Global index
            entries.Add(string.Format("ai/e/{0}", filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("ai/t/{0}/{1}", Indexes.IndexEntries.GetTokenHash(token), filename)));

            return entries;
        }
    }

    public class AppList
    {
        public Guid GroupKey { get; set; }
        public IEnumerable<AppListItem> Apps { get; set; }
        public string Query { get; set; }
        public string Marker { get; set; }
        public int PageSize { get; set; }
        public string NextMarker { get; set; }
        public bool IsTruncated { get; set; }
    }

    public class AppListItem
    {
        public AppListItem()
        {
        }

        public AppListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 5)
                throw new ArgumentException("An app path index entry must contain exactly 5 segments.", "path");

            Marker = segments[0];
            Key = Utils.Indexes.DecodeGuid(segments[1]);
            GroupKey = Utils.Indexes.DecodeGuid(segments[2]);
            Name = Utils.Indexes.DecodeText(segments[3]);
            MajorVersion = Utils.Indexes.DecodeText(segments[4]);
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public string MajorVersion { get; set; }
    }

    #endregion
}
