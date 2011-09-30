using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Plywood.Utils;
using Plywood.Indexes;
using System.Xml.Schema;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

namespace Plywood
{
    public class Groups : ControllerBase
    {
        public Groups(IStorageProvider provider) : base(provider) { }

        public void Create(Group group)
        {
            if (group == null)
                throw new ArgumentNullException("group", "Group cannot be null.");

            using (var stream = group.Serialise())
            {
                try
                {
                    var indexEntries = new IndexEntries(StorageProvider);

                    StorageProvider.PutFile(Paths.GetGroupDetailsKey(group.Key), stream);
                    indexEntries.PutEntity(group);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating group.", ex);
                }
            }
        }

        public void Delete(Guid key)
        {
            var group = Get(key);
            try
            {
                var indexEntries = new IndexEntries(StorageProvider);
                indexEntries.DeleteEntity(group);

                // TODO: Refactor the self-delete functionality.
                StorageProvider.MoveFile(Paths.GetGroupDetailsKey(key), string.Concat("deleted/", Paths.GetGroupDetailsKey(key)));
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting group.", ex);
            }
        }

        public bool Exists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Paths.GetGroupDetailsKey(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting group with key \"{0}\"", key), ex);
            }
        }

        public Group Get(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetGroupDetailsKey(key)))
                {
                    return new Group(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting group with key \"{0}\"", key), ex);
            }
        }

        public GroupList Search(string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 0.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                IEnumerable<string> basePaths;

                if (!string.IsNullOrWhiteSpace(query))
                    basePaths = new SimpleTokeniser().Tokenise(query).Select(token =>
                        string.Format("gi/t/{0}", Indexes.IndexEntries.GetTokenHash(token)));
                else
                    basePaths = new List<string>()
                    {
                        "gi/e",
                    };

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                IEnumerable<GroupListItem> groups = rawResults.FileNames.Select(fileName => new GroupListItem(fileName));
                var list = new GroupList()
                {
                    Groups = groups,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = groups.Any() ? groups.Last().Marker : marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed searching groups.", ex);
            }
        }

        public void Update(Group group)
        {
            if (group == null)
                throw new ArgumentNullException("group", "Group cannot be null.");

            var oldGroup = Get(group.Key);

            using (var stream = group.Serialise())
            {
                try
                {
                    StorageProvider.PutFile(Paths.GetGroupDetailsKey(group.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.UpdateEntity(oldGroup, group);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed updating group.", ex);
                }
            }
        }
    }

    public class Group : IIndexableEntity
    {

        #region Constructors

        public Group()
        {
            Key = Guid.NewGuid();
        }

        public Group(string source)
            : this(Group.Parse(source)) { }

        public Group(Stream source)
            : this(Group.Parse(source)) { }

        public Group(TextReader source)
            : this(Group.Parse(source)) { }

        public Group(XmlTextReader source)
            : this(Group.Parse(source)) { }

        public Group(Group other)
        {
            this.Key = other.Key;
            this.Name = other.Name;
            this.Tags = other.Tags;
        }

        #endregion

        public Guid Key { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public Stream Serialise()
        {
            return Group.Serialise(this);
        }

        public IEnumerable<string> GetIndexEntries()
        {
            var filename = string.Format("{0}-{1}-{2}",
                Hashing.CreateHash(Name), Utils.Indexes.EncodeGuid(Key), Utils.Indexes.EncodeText(Name));

            var tokens = (new SimpleTokeniser()).Tokenise(Name).ToList();
            var entries = new List<string>(tokens.Count() + 1);

            entries.Add(string.Format("gi/e/{0}", filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("gi/t/{0}/{1}", Indexes.IndexEntries.GetTokenHash(token), filename)));

            return entries;
        }

        #region Static Serialisation Methods

        public static Stream Serialise(Group group)
        {
            if (group == null)
                throw new ArgumentNullException("group", "Group cannot be null.");
            if (!Validation.IsNameValid(group.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("group",
                    new XAttribute("key", group.Key),
                    new XElement("name", group.Name),
                    new XElement("tags")));

            if (group.Tags != null && group.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    group.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static Group Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Group Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Group Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Group Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising group.", ex);
            }

            if (!ValidateGroupXml(doc))
                throw new DeserialisationException("Serialised group xml is not valid.");

            Guid key;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised group key is not a valid guid.");

            var group = new Group()
            {
                Key = key,
                Name = doc.Root.Element("name").Value,
            };

            var tagsElement = doc.Root.Element("tags");
            if (tagsElement != null && tagsElement.HasElements)
            {
                group.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }
            else
            {
                group.Tags = new Dictionary<string, string>();
            }

            return group;
        }

        public static bool ValidateGroupXml(XDocument groupDoc)
        {
            bool valid = true;
            groupDoc.Validate(Schemas, (o, e) =>
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
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.Group.xsd"))
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

    }

    public class GroupList
    {
        public IEnumerable<GroupListItem> Groups { get; set; }
        public string Query { get; set; }
        public string Marker { get; set; }
        public string NextMarker { get; set; }
        public int PageSize { get; set; }
        public bool IsTruncated { get; set; }
    }

    public class GroupListItem
    {
        public GroupListItem()
        {
        }

        public GroupListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 3)
                throw new ArgumentException("A group path index entry does not contain exactly 3 segments.", "path");

            Marker = segments[0];
            Key = Utils.Indexes.DecodeGuid(segments[1]);
            Name = Utils.Indexes.DecodeText(segments[2]);
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
