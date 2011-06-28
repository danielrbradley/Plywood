using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml;
using System.Reflection;

namespace Plywood
{
    public class Target
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
                            schemas = new XmlSchemaSet();
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.Target.xsd"))
                            {
                                schemas.Add("", XmlReader.Create(stream));
                            }
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

    public class TargetList
    {
        public Guid GroupKey { get; set; }
        public IEnumerable<TargetListItem> Targets { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class TargetListItem
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }

    public class TargetAppList
    {
        public Guid TargetKey { get; set; }
        public IEnumerable<AppListItem> Apps { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class TargetAppVersion
    {
        public string ETag { get; set; }
        public Guid Key { get; set; }
    }

    public enum VersionCheckResult
    {
        NotChanged,
        Changed,
        NotSet
    }
}
