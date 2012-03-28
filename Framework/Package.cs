using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using Plywood.Indexes;
using Plywood.Utils;

namespace Plywood
{
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
}
