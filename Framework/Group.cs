namespace Plywood
{
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
}
