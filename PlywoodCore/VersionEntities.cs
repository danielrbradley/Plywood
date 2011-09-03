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
using Plywood.Indexes;

namespace Plywood
{
    public class Version : IIndexableEntity
    {
        #region Constructors

        public Version()
        {
            Key = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            Tags = new Dictionary<string, string>();
        }

        public Version(string source)
            : this(Version.Parse(source)) { }

        public Version(Stream source)
            : this(Version.Parse(source)) { }

        public Version(TextReader source)
            : this(Version.Parse(source)) { }

        public Version(XmlTextReader source)
            : this(Version.Parse(source)) { }

        private Version(Version other)
        {
            this.Key = other.Key;
            this.AppKey = other.AppKey;
            this.GroupKey = other.GroupKey;
            this.VersionNumber = other.VersionNumber;
            this.Comment = other.Comment;
            this.Timestamp = other.Timestamp;
            this.Tags = other.Tags;
        }

        #endregion

        public Guid Key { get; set; }
        public Guid AppKey { get; set; }
        public Guid GroupKey { get; set; }
        [Obsolete]
        public string Name { get { return string.Format("{0} {1}", this.VersionNumber, this.Comment); } }
        public string VersionNumber { get; set; }
        public string Comment { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        public Stream Serialise()
        {
            return Version.Serialise(this);
        }

        #region Static Serialisation Methods

        public static Stream Serialise(Version version)
        {
            if (version == null)
                throw new ArgumentNullException("version", "Version cannot be null.");
            if (!Validation.IsNameValid(version.Comment))
                throw new ArgumentException("Comment must be valid (not blank & only a single line).");
            if (!Validation.IsMajorVersionValid(version.VersionNumber))
                throw new ArgumentException("Version number must be numbers separated by dots (.)");

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("version",
                    new XAttribute("key", version.Key),
                    new XElement("groupKey", version.GroupKey),
                    new XElement("appKey", version.AppKey),
                    new XElement("timestamp", version.Timestamp),
                    new XElement("versionNumber", version.VersionNumber),
                    new XElement("comment", version.Comment),
                    new XElement("tags")));

            if (version.Tags != null && version.Tags.Count > 0)
            {
                doc.Root.Element("tags").Add(
                    version.Tags.Select(t =>
                        new XElement("tag",
                            new XAttribute("key", t.Key),
                            t.Value
                            )));
            }

            return Serialisation.Serialise(doc);
        }

        public static Version Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Version Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Version Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static Version Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising version.", ex);
            }

            if (!ValidateVersionXml(doc))
                throw new DeserialisationException("Serialised version xml is not valid.");

            Guid key, groupKey, appKey;
            DateTime localTimestamp;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised version key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised version group key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("appKey").Value, out appKey))
                throw new DeserialisationException("Serialised version app key is not a valid guid.");
            if (!DateTime.TryParse(doc.Root.Element("timestamp").Value, out localTimestamp))
                throw new DeserialisationException("Serialised version timestamp is not a valid datetime.");

            var version = new Version()
            {
                Key = key,
                GroupKey = groupKey,
                AppKey = appKey,
                Timestamp = localTimestamp.ToUniversalTime(),
                VersionNumber = doc.Root.Element("versionNumber").Value,
                Comment = doc.Root.Element("comment").Value,
            };

            var tagsElement = doc.Root.Element("tags");
            if (tagsElement != null && tagsElement.HasElements)
            {
                version.Tags = tagsElement.Elements("tag").ToDictionary(t => t.Attribute("key").Value, t => t.Value);
            }
            else
            {
                version.Tags = new Dictionary<string, string>();
            }

            return version;
        }

        public static bool ValidateVersionXml(XDocument targetDoc)
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
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.Version.xsd"))
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

        [Obsolete]
        internal Indexes.IndexEntry GetIndexEntry()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetIndexEntries()
        {
            var filename = string.Format("{0}-{1}-{2}-{3}-{4}",
                Hashing.CreateVersionHash(VersionNumber), Utils.Indexes.EncodeGuid(Key), Utils.Indexes.EncodeText(Comment), Utils.Indexes.EncodeText(VersionNumber), Utils.Indexes.EncodeText(Timestamp.ToString("s")));

            SimpleTokeniser tokeniser = new SimpleTokeniser();
            var tokens = tokeniser.Tokenise(Comment).ToList();
            tokens.AddRange((new VersionTokeniser()).Tokenise(VersionNumber));

            var entries = new List<string>(tokens.Count() + 1);

            // App specific index
            entries.Add(string.Format("a/{0}/vi/e/{1}", Utils.Indexes.EncodeGuid(AppKey), filename));
            entries.AddRange(tokens.Select(token =>
                string.Format("a/{0}/vi/t/{1}/{2}", Utils.Indexes.EncodeGuid(AppKey), Indexes.IndexEntries.GetTokenHash(token), filename)));

            return entries;
        }
    }

    public class VersionList
    {
        public Guid AppKey { get; set; }
        public IEnumerable<VersionListItem> Versions { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class VersionListItem
    {
        public VersionListItem()
        {
        }

        public VersionListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 5)
                throw new ArgumentException("A version path index entry must contain exactly 5 segments.", "path");

            Marker = segments[0];
            Key = Utils.Indexes.DecodeGuid(segments[1]);
            Comment = Utils.Indexes.DecodeText(segments[2]);
            VersionNumber = Utils.Indexes.DecodeText(segments[3]);
            Timestamp = DateTime.Parse(Utils.Indexes.DecodeText(segments[4]));
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public DateTime Timestamp { get; set; }
        [Obsolete]
        public string Name { get { return string.Format("{0} {1}", this.VersionNumber, this.Comment); } }
        public string VersionNumber { get; set; }
        public string Comment { get; set; }
    }
}
