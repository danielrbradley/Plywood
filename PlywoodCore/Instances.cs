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
    public class Instances : ControllerBase
    {
        public Instances(IStorageProvider provider) : base(provider) { }

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
                    var targetsController = new Targets(StorageProvider);
                    if (!targetsController.TargetExists(instance.TargetKey))
                        throw new TargetNotFoundException(String.Format("Target with the key \"{0}\" could not be found.", instance.TargetKey));

                    var indexEntries = new IndexEntries(StorageProvider);

                    StorageProvider.PutFile(Paths.GetInstanceDetailsKey(instance.Key), stream);

                    indexEntries.PutEntity(instance);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating instance.", ex);
                }
            }
        }

        public void DeleteInstance(Guid key)
        {
            var instance = GetInstance(key);
            try
            {
                var indexEntries = new IndexEntries(StorageProvider);
                indexEntries.DeleteEntity(instance);

                // TODO: Refactor the solf-delete functionality.
                StorageProvider.MoveFile(Paths.GetGroupDetailsKey(key), string.Concat(".recycled/", Paths.GetGroupDetailsKey(key)));
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting instance.", ex);
            }
        }

        public bool InstanceExists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Paths.GetInstanceDetailsKey(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting instance with key \"{0}\"", key), ex);
            }
        }

        public Instance GetInstance(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetInstanceDetailsKey(key)))
                {
                    return new Instance(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting instance with key \"{0}\"", key), ex);
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

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var instances = rawResults.FileNames.Select(fileName => new InstanceListItem(fileName));
                var list = new InstanceList()
                {
                    Instances = instances,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = instances.Any() ? instances.Last().Marker : marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed searcing instances.", ex);
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
                        StorageProvider.PutFile(Paths.GetInstanceDetailsKey(updatedInstance.Key), stream);

                        var indexEntries = new IndexEntries(StorageProvider);
                        indexEntries.UpdateEntity(existingInstance, updatedInstance);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed updating instance.", ex);
                }
            }
        }
    }

    #region Entiies

    public class Instance : IIndexableEntity
    {
        #region Constructors

        public Instance()
        {
            Key = Guid.NewGuid();
            Name = "New Instance " + DateTime.UtcNow.ToString("r");
            Tags = new Dictionary<string, string>();
        }

        public Instance(string source)
            : this(Instance.Parse(source)) { }

        public Instance(Stream source)
            : this(Instance.Parse(source)) { }

        public Instance(TextReader source)
            : this(Instance.Parse(source)) { }

        public Instance(XmlTextReader source)
            : this(Instance.Parse(source)) { }

        private Instance(Instance other)
        {
            this.Key = other.Key;
            this.GroupKey = other.GroupKey;
            this.TargetKey = other.TargetKey;
            this.Name = other.Name;
            this.Tags = other.Tags;
        }

        #endregion

        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public Guid TargetKey { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public Stream Serialise()
        {
            return Instance.Serialise(this);
        }

        #region Static Serialisation Methods

        public static Stream Serialise(Instance instance)
        {
            if (instance == null)
                throw new ArgumentNullException("version", "Version cannot be null.");
            if (!Validation.IsNameValid(instance.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("instance",
                    new XAttribute("key", instance.Key),
                    new XElement("groupKey", instance.GroupKey),
                    new XElement("targetKey", instance.TargetKey),
                    new XElement("name", instance.Name),
                    new XElement("tags")));

            if (instance.Tags != null && instance.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    instance.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static Instance Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Instance Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Instance Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Instance Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising instance.", ex);
            }

            if (!ValidateInstanceXml(doc))
                throw new DeserialisationException("Serialised instance xml is not valid.");

            Guid key, groupKey, targetKey;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised instance key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised instance group key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("targetKey").Value, out targetKey))
                throw new DeserialisationException("Serialised instance target key is not a valid guid.");

            var instance = new Instance()
            {
                Key = key,
                GroupKey = groupKey,
                TargetKey = targetKey,
                Name = doc.Root.Element("name").Value,
            };

            var tagsElement = doc.Root.Element("tags");
            if (tagsElement != null && tagsElement.HasElements)
            {
                instance.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }
            else
            {
                instance.Tags = new Dictionary<string, string>();
            }

            return instance;
        }

        public static bool ValidateInstanceXml(XDocument targetDoc)
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
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.Instance.xsd"))
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
            var filename = string.Format("{0}-{1}-{2}",
                Hashing.CreateHash(Name), Utils.Indexes.EncodeGuid(Key), Utils.Indexes.EncodeText(Name));

            SimpleTokeniser tokeniser = new SimpleTokeniser();
            var tokens = tokeniser.Tokenise(Name).ToList();

            var entries = new List<string>(tokens.Count() + 1);

            // Target specific index
            entries.Add(string.Format("t/{0}/ii/e/{1}", Utils.Indexes.EncodeGuid(TargetKey), filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("t/{0}/ii/t/{1}/{2}", Utils.Indexes.EncodeGuid(TargetKey), Indexes.IndexEntries.GetTokenHash(token), filename)));

            // Global index
            entries.Add(string.Format("ii/e/{0}", filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("ii/t/{0}/{1}", Indexes.IndexEntries.GetTokenHash(token), filename)));

            return entries;
        }
    }

    public class InstanceList
    {
        public Guid TargetKey { get; set; }
        public IEnumerable<InstanceListItem> Instances { get; set; }
        public string Query { get; set; }
        public string Marker { get; set; }
        public int PageSize { get; set; }
        public string NextMarker { get; set; }
        public bool IsTruncated { get; set; }
    }

    public class InstanceListItem
    {
        public InstanceListItem()
        {
        }

        public InstanceListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 3)
                throw new ArgumentException("An instance path index entry must contain exactly 3 segments.", "path");

            Marker = segments[0];
            Key = Utils.Indexes.DecodeGuid(segments[1]);
            Name = Utils.Indexes.DecodeText(segments[2]);
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; }
    }

    #endregion
}
