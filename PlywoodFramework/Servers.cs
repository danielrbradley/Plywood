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
    public class Servers : ControllerBase
    {
        public Servers(IStorageProvider provider) : base(provider) { }

        public void Create(Server server)
        {
            if (server == null)
                throw new ArgumentNullException("server", "Server cannot be null.");
            if (server.RoleKey == Guid.Empty)
                throw new ArgumentException("Role key cannot be empty.", "server.RoleKey");

            using (var stream = server.Serialise())
            {
                try
                {
                    var roles = new Roles(StorageProvider);
                    if (!roles.Exists(server.RoleKey))
                        throw new RoleNotFoundException(String.Format("Role with the key \"{0}\" could not be found.", server.RoleKey));

                    var indexEntries = new IndexEntries(StorageProvider);

                    StorageProvider.PutFile(Paths.GetServerDetailsKey(server.Key), stream);

                    indexEntries.PutEntity(server);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating server.", ex);
                }
            }
        }

        public void Delete(Guid key)
        {
            var server = Get(key);
            try
            {
                var indexEntries = new IndexEntries(StorageProvider);
                indexEntries.DeleteEntity(server);

                // TODO: Refactor the solf-delete functionality.
                StorageProvider.MoveFile(Paths.GetServerDetailsKey(key), string.Concat("deleted/", Paths.GetServerDetailsKey(key)));
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting server.", ex);
            }
        }

        public bool Exists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Paths.GetServerDetailsKey(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting server with key \"{0}\"", key), ex);
            }
        }

        public Server Get(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetServerDetailsKey(key)))
                {
                    return new Server(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting server with key \"{0}\"", key), ex);
            }
        }

        public ServerList Search(Guid roleKey, string query = null, string marker = null, int pageSize = 50)
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
                        string.Format("r/{0}/si/t/{1}", Utils.Indexes.EncodeGuid(roleKey), Indexes.IndexEntries.GetTokenHash(token)));
                }
                else
                    basePaths = new List<string>() { string.Format("r/{0}/si/e", Utils.Indexes.EncodeGuid(roleKey)) };

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var servers = rawResults.FileNames.Select(fileName => new ServerListItem(fileName));
                var list = new ServerList()
                {
                    Items = servers,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = servers.Any() ? servers.Last().Marker : marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed searcing instances.", ex);
            }
        }

        public void Update(Server updatedServer)
        {
            if (updatedServer == null)
                throw new ArgumentNullException("updatedInstance", "Instance cannot be null.");

            var existingServer = Get(updatedServer.Key);
            // Don't allow moving between roles.
            updatedServer.RoleKey = existingServer.RoleKey;

            using (var stream = updatedServer.Serialise())
            {
                try
                {
                    StorageProvider.PutFile(Paths.GetServerDetailsKey(updatedServer.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.UpdateEntity(existingServer, updatedServer);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed updating instance.", ex);
                }
            }
        }
    }

    #region Entiies

    public class Server : IIndexableEntity
    {
        #region Constructors

        public Server()
        {
            Key = Guid.NewGuid();
            Name = "New Server " + DateTime.UtcNow.ToString("r");
            Tags = new Dictionary<string, string>();
        }

        public Server(string source)
            : this(Server.Parse(source)) { }

        public Server(Stream source)
            : this(Server.Parse(source)) { }

        public Server(TextReader source)
            : this(Server.Parse(source)) { }

        public Server(XmlTextReader source)
            : this(Server.Parse(source)) { }

        private Server(Server other)
        {
            this.Key = other.Key;
            this.GroupKey = other.GroupKey;
            this.RoleKey = other.RoleKey;
            this.Name = other.Name;
            this.Tags = other.Tags;
        }

        #endregion

        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public Guid RoleKey { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public Stream Serialise()
        {
            return Server.Serialise(this);
        }

        #region Static Serialisation Methods

        public static Stream Serialise(Server server)
        {
            if (server == null)
                throw new ArgumentNullException("server", "Server cannot be null.");
            if (!Validation.IsNameValid(server.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("server",
                    new XAttribute("key", server.Key),
                    new XElement("groupKey", server.GroupKey),
                    new XElement("roleKey", server.RoleKey),
                    new XElement("name", server.Name),
                    new XElement("tags")));

            if (server.Tags != null && server.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    server.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static Server Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Server Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Server Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Server Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising server.", ex);
            }

            if (!ValidateInstanceXml(doc))
                throw new DeserialisationException("Serialised server xml is not valid.");

            Guid key, groupKey, targetKey;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised server key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised server group key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("roleKey").Value, out targetKey))
                throw new DeserialisationException("Serialised server role key is not a valid guid.");

            var server = new Server()
            {
                Key = key,
                GroupKey = groupKey,
                RoleKey = targetKey,
                Name = doc.Root.Element("name").Value,
            };

            var tagsElement = doc.Root.Element("tags");
            if (tagsElement != null && tagsElement.HasElements)
            {
                server.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }
            else
            {
                server.Tags = new Dictionary<string, string>();
            }

            return server;
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
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.Server.xsd"))
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

            // Role specific index
            entries.Add(string.Format("r/{0}/si/e/{1}", Utils.Indexes.EncodeGuid(RoleKey), filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("r/{0}/si/t/{1}/{2}", Utils.Indexes.EncodeGuid(RoleKey), Indexes.IndexEntries.GetTokenHash(token), filename)));

            // Global index
            entries.Add(string.Format("si/e/{0}", filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("si/t/{0}/{1}", Indexes.IndexEntries.GetTokenHash(token), filename)));

            return entries;
        }
    }

    public class ServerList
    {
        public Guid RoleKey { get; set; }
        public IEnumerable<ServerListItem> Items { get; set; }
        public string Query { get; set; }
        public string Marker { get; set; }
        public int PageSize { get; set; }
        public string NextMarker { get; set; }
        public bool IsTruncated { get; set; }
    }

    public class ServerListItem
    {
        public ServerListItem()
        {
        }

        public ServerListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 3)
                throw new ArgumentException("An server path index entry must contain exactly 3 segments.", "path");

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
