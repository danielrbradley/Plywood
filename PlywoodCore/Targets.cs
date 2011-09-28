using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Amazon.S3;
using Amazon.S3.Model;
using Plywood.Indexes;
using System.Xml.Schema;
using System.Xml;
using System.Reflection;
using System.Xml.Linq;
using System.IO;

namespace Plywood
{
    public class Targets : ControllerBase
    {
        [Obsolete]
        public const string STR_TARGET_INDEX_PATH = ".targets.index";
        [Obsolete]
        public const string STR_TARGETS_CONTAINER_PATH = "targets";

        public Targets(IStorageProvider provider) : base(provider) { }

        public void CreateTarget(Target target)
        {
            if (target == null)
                throw new ArgumentNullException("target", "Target cannot be null.");
            if (target.GroupKey == Guid.Empty)
                throw new ArgumentException("Group key cannot be empty.", "target.GroupKey");

            using (var stream = target.Serialise())
            {
                try
                {
                    var groupsController = new Groups(StorageProvider);
                    if (!groupsController.GroupExists(target.GroupKey))
                        throw new GroupNotFoundException(String.Format("Group with the key \"{0}\" could not be found.", target.GroupKey));

                    StorageProvider.PutFile(Paths.GetTargetDetailsKey(target.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.PutEntity(target);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating target.", ex);
                }
            }
        }

        public void DeleteTarget(Guid key)
        {
            var target = GetTarget(key);
            try
            {
                var indexEntries = new IndexEntries(StorageProvider);
                indexEntries.DeleteEntity(target);

                // TODO: Refactor the solf-delete functionality.
                StorageProvider.MoveFile(Paths.GetTargetDetailsKey(key), string.Concat(".recycled/", Paths.GetTargetDetailsKey(key)));
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting target.", ex);
            }
        }

        public Target GetTarget(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetTargetDetailsKey(key)))
                {
                    return new Target(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting target with key \"{0}\"", key), ex);
            }
        }

        public TargetList SearchTargets(Guid? groupKey, string query = null, string marker = null, int pageSize = 50)
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
                startLocations[0] = "ti";
                if (groupKey.HasValue)
                    startLocations[1] = string.Format("gi/{0}/ti");

                IEnumerable<string> basePaths;

                if (!string.IsNullOrWhiteSpace(query))
                    basePaths = new SimpleTokeniser().Tokenise(query).SelectMany(token =>
                        startLocations.Select(l => string.Format("{0}/t/{1}", l, Indexes.IndexEntries.GetTokenHash(token))));
                else
                    basePaths = startLocations.Select(l => string.Format("{0}/e", l));

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var targets = rawResults.FileNames.Select(fileName => new TargetListItem(fileName));
                var list = new TargetList()
                {
                    Targets = targets,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = targets.Last().Marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed searcing targets.", ex);
            }
        }

        public void UpdateTarget(Target target)
        {
            if (target == null)
                throw new ArgumentNullException("target", "Target cannot be null.");

            var existingTarget = GetTarget(target.Key);
            // Don't allow moving between groups.
            target.GroupKey = existingTarget.GroupKey;

            using (var stream = target.Serialise())
            {
                try
                {

                    StorageProvider.PutFile(Paths.GetTargetDetailsKey(target.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.UpdateEntity(existingTarget, target);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed updating target.", ex);
                }
            }
        }

        [Obsolete]
        public string GetGroupTargetsIndexPath(Guid groupKey)
        {
            return string.Format("{0}/{1}/{2}", Groups.STR_GROUPS_CONTAINER_PATH, groupKey.ToString("N"), STR_TARGET_INDEX_PATH);
        }

        public bool TargetExists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Paths.GetTargetDetailsKey(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting target with key \"{0}\"", key), ex);
            }
        }
    }

    #region Entities

    public class Target : IIndexableEntity
    {
        #region Constructors

        public Target()
        {
            Key = Guid.NewGuid();
            Tags = new Dictionary<string, string>();
        }

        public Target(string source)
            : this(Target.Parse(source)) { }

        public Target(Stream source)
            : this(Target.Parse(source)) { }

        public Target(TextReader source)
            : this(Target.Parse(source)) { }

        public Target(XmlTextReader source)
            : this(Target.Parse(source)) { }

        public Target(Target prototype)
        {
            this.Key = prototype.Key;
            this.Name = prototype.Name;
            this.Tags = prototype.Tags;
            this.GroupKey = prototype.GroupKey;
        }

        #endregion

        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public Stream Serialise()
        {
            return Target.Serialise(this);
        }

        #region Static Serialisation Methods

        public static Stream Serialise(Target target)
        {
            if (target == null)
                throw new ArgumentNullException("target", "Target cannot be null.");
            if (!Validation.IsNameValid(target.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("target",
                    new XAttribute("key", target.Key),
                    new XElement("groupKey", target.GroupKey),
                    new XElement("name", target.Name),
                    new XElement("tags")));

            if (target.Tags != null && target.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    target.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static Target Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Target Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Target Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Target Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising target.", ex);
            }

            if (!ValidateTargetXml(doc))
                throw new DeserialisationException("Serialised target xml is not valid.");

            Guid key, groupKey;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised target key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised target group key is not a valid guid.");

            var target = new Target()
            {
                Key = key,
                GroupKey = groupKey,
                Name = doc.Root.Element("name").Value,
            };

            var tagsElement = doc.Root.Element("tags");
            if (tagsElement != null && tagsElement.HasElements)
            {
                target.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }
            else
            {
                target.Tags = new Dictionary<string, string>();
            }

            return target;
        }

        public static bool ValidateTargetXml(XDocument targetDoc)
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
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.Target.xsd"))
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
                Hashing.CreateHash(Name), Utils.Indexes.EncodeGuid(Key), Utils.Indexes.EncodeGuid(GroupKey), Utils.Indexes.EncodeText(Name));

            var tokens = (new SimpleTokeniser()).Tokenise(Name).ToList();
            var entries = new List<string>(tokens.Count() + 1);

            // Group specific index
            entries.Add(string.Format("g/{0}/ti/e/{1}", Utils.Indexes.EncodeGuid(GroupKey), filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("g/{0}/ti/t/{1}/{2}", Utils.Indexes.EncodeGuid(GroupKey), Indexes.IndexEntries.GetTokenHash(token), filename)));

            // Global index
            entries.Add(string.Format("ti/e/{0}", filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("ti/t/{0}/{1}", Indexes.IndexEntries.GetTokenHash(token), filename)));

            return entries;
        }
    }

    public class TargetList
    {
        public Guid GroupKey { get; set; }
        public IEnumerable<TargetListItem> Targets { get; set; }
        public string Query { get; set; }
        public string Marker { get; set; }
        public int PageSize { get; set; }
        public string NextMarker { get; set; }
        public bool IsTruncated { get; set; }
    }

    public class TargetListItem
    {
        public TargetListItem()
        {
        }

        public TargetListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 4)
                throw new ArgumentException("A target entity path index entry must contain exactly 4 segments.", "path");

            Marker = segments[0];
            Key = Utils.Indexes.DecodeGuid(segments[1]);
            GroupKey = Utils.Indexes.DecodeGuid(segments[2]);
            Name = Utils.Indexes.DecodeText(segments[3]);
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
    }

    #endregion
}
