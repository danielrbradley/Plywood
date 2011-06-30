using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Reflection;
using System.Xml;

namespace Plywood
{
    public class Instance
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

    }

    public class InstanceList
    {
        public Guid TargetKey { get; set; }
        public IEnumerable<InstanceListItem> Instances { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class InstanceListItem
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
