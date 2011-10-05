using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Plywood.Indexes;
using System.Xml.Schema;
using System.Xml;
using System.Reflection;
using System.Xml.Linq;
using System.IO;

namespace Plywood
{
    public class Roles : ControllerBase
    {
        public Roles(IStorageProvider provider) : base(provider) { }

        public void Create(Role role)
        {
            if (role == null)
                throw new ArgumentNullException("role", "Role cannot be null.");
            if (role.GroupKey == Guid.Empty)
                throw new ArgumentException("Group key cannot be empty.", "role.GroupKey");

            using (var stream = role.Serialise())
            {
                try
                {
                    var groupsController = new Groups(StorageProvider);
                    if (!groupsController.Exists(role.GroupKey))
                        throw new GroupNotFoundException(String.Format("Group with the key \"{0}\" could not be found.", role.GroupKey));

                    StorageProvider.PutFile(Paths.GetRoleDetailsKey(role.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.PutEntity(role);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating role.", ex);
                }
            }
        }

        public void Delete(Guid key)
        {
            var role = Get(key);
            try
            {
                var indexEntries = new IndexEntries(StorageProvider);
                indexEntries.DeleteEntity(role);

                // TODO: Refactor the self-delete functionality.
                StorageProvider.MoveFile(Paths.GetRoleDetailsKey(key), string.Concat("deleted/", Paths.GetRoleDetailsKey(key)));
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting role.", ex);
            }
        }

        public bool Exists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Paths.GetRoleDetailsKey(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting role with key \"{0}\"", key), ex);
            }
        }

        public Role Get(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetRoleDetailsKey(key)))
                {
                    return new Role(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting role with key \"{0}\"", key), ex);
            }
        }

        public RoleList Search(Guid? groupKey = null, string query = null, string marker = null, int pageSize = 50)
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
                startLocations[0] = "ri";
                if (groupKey.HasValue)
                    startLocations[1] = string.Format("gi/{0}/ri", Utils.Indexes.EncodeGuid(groupKey.Value));

                IEnumerable<string> basePaths;

                if (!string.IsNullOrWhiteSpace(query))
                    basePaths = new SimpleTokeniser().Tokenise(query).SelectMany(token =>
                        startLocations.Select(l => string.Format("{0}/t/{1}", l, Indexes.IndexEntries.GetTokenHash(token))));
                else
                    basePaths = startLocations.Select(l => string.Format("{0}/e", l));

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var items = rawResults.FileNames.Select(fileName => new RoleListItem(fileName));

                var list = new RoleList()
                {
                    Items = items,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = items.Any() ? items.Last().Marker : marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed searcing targets.", ex);
            }
        }

        public void Update(Role role)
        {
            if (role == null)
                throw new ArgumentNullException("role", "Role cannot be null.");

            var existingRole = Get(role.Key);
            // Don't allow moving between groups.
            role.GroupKey = existingRole.GroupKey;

            using (var stream = role.Serialise())
            {
                try
                {

                    StorageProvider.PutFile(Paths.GetRoleDetailsKey(role.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.UpdateEntity(existingRole, role);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed updating role.", ex);
                }
            }
        }
    }

    #region Entities

    public class Role : IIndexableEntity
    {
        #region Constructors

        public Role()
        {
            Key = Guid.NewGuid();
            Tags = new Dictionary<string, string>();
        }

        public Role(string source)
            : this(Role.Parse(source)) { }

        public Role(Stream source)
            : this(Role.Parse(source)) { }

        public Role(TextReader source)
            : this(Role.Parse(source)) { }

        public Role(XmlTextReader source)
            : this(Role.Parse(source)) { }

        public Role(Role prototype)
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
            return Role.Serialise(this);
        }

        #region Static Serialisation Methods

        public static Stream Serialise(Role role)
        {
            if (role == null)
                throw new ArgumentNullException("role", "Role cannot be null.");
            if (!Validation.IsNameValid(role.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("role",
                    new XAttribute("key", role.Key),
                    new XElement("groupKey", role.GroupKey),
                    new XElement("name", role.Name),
                    new XElement("tags")));

            if (role.Tags != null && role.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    role.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static Role Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Role Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Role Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Role Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising role.", ex);
            }

            if (!ValidateTargetXml(doc))
                throw new DeserialisationException("Serialised role xml is not valid.");

            Guid key, groupKey;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised role key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised role group key is not a valid guid.");

            var role = new Role()
            {
                Key = key,
                GroupKey = groupKey,
                Name = doc.Root.Element("name").Value,
            };

            var tagsElement = doc.Root.Element("tags");
            if (tagsElement != null && tagsElement.HasElements)
            {
                role.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }
            else
            {
                role.Tags = new Dictionary<string, string>();
            }

            return role;
        }

        public static bool ValidateTargetXml(XDocument roleDoc)
        {
            bool valid = true;
            roleDoc.Validate(Schemas, (o, e) =>
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
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.Role.xsd"))
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
            entries.Add(string.Format("g/{0}/ri/e/{1}", Utils.Indexes.EncodeGuid(GroupKey), filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("g/{0}/ri/t/{1}/{2}", Utils.Indexes.EncodeGuid(GroupKey), Indexes.IndexEntries.GetTokenHash(token), filename)));

            // Global index
            entries.Add(string.Format("ri/e/{0}", filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("ri/t/{0}/{1}", Indexes.IndexEntries.GetTokenHash(token), filename)));

            return entries;
        }
    }

    public class RoleList
    {
        public Guid GroupKey { get; set; }
        public IEnumerable<RoleListItem> Items { get; set; }
        public string Query { get; set; }
        public string Marker { get; set; }
        public int PageSize { get; set; }
        public string NextMarker { get; set; }
        public bool IsTruncated { get; set; }
    }

    public class RoleListItem
    {
        public RoleListItem()
        {
        }

        public RoleListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 4)
                throw new ArgumentException("A target entity path index entry must contain exactly 4 segments.", "path");

            Marker = Utils.Indexes.GetIndexFileName(path);
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
