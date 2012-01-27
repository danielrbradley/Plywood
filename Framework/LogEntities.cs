using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Reflection;
using System.Xml;
using Plywood.Indexes;

namespace Plywood
{
    public class LogEntry : IIndexableEntity
    {
        #region Constructors

        public LogEntry()
        {
            Key = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            Status = LogStatus.Ok;
        }

        public LogEntry(string source)
            : this(LogEntry.Parse(source)) { }

        public LogEntry(Stream source)
            : this(LogEntry.Parse(source)) { }

        public LogEntry(TextReader source)
            : this(LogEntry.Parse(source)) { }

        public LogEntry(XmlTextReader source)
            : this(LogEntry.Parse(source)) { }

        public LogEntry(LogEntry prototype)
        {
            this.Key = prototype.Key;
            this.Timestamp = prototype.Timestamp;
            this.Status = prototype.Status;
            this.GroupKey = prototype.GroupKey;
            this.RoleKey = prototype.RoleKey;
            this.ServerKey = prototype.ServerKey;
            this.LogContent = prototype.LogContent;
        }

        #endregion

        public Guid Key { get; set; }
        public DateTime Timestamp { get; set; }
        public LogStatus Status { get; set; }
        public Guid GroupKey { get; set; }
        public Guid RoleKey { get; set; }
        public Guid ServerKey { get; set; }
        public string LogContent { get; set; }

        public Stream Serialise()
        {
            return LogEntry.Serialise(this);
        }

        #region Static Serialisation Methods

        public static Stream Serialise(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException("logEntry", "Version cannot be null.");

            DateTime timestamp = logEntry.Timestamp;
            if (timestamp.Kind != DateTimeKind.Utc)
            {
                timestamp = timestamp.ToUniversalTime();
            }

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("logEntry",
                    new XAttribute("key", logEntry.Key),
                    new XElement("timestamp", timestamp),
                    new XElement("status", logEntry.Status),
                    new XElement("groupKey", logEntry.GroupKey),
                    new XElement("targetKey", logEntry.RoleKey),
                    new XElement("instanceKey", logEntry.ServerKey),
                    new XElement("logContent", logEntry.LogContent)));

            return Serialisation.Serialise(doc);
        }

        public static LogEntry Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static LogEntry Parse(Stream source)
        {
            return Parse(new XmlTextReader(source));
        }
        
        public static LogEntry Parse(TextReader source)
        {
            return Parse(new XmlTextReader(source));
        }

        public static LogEntry Parse(XmlReader source)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(source);
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed deserialising log entry.", ex);
            }

            if (!ValidateInstanceXml(doc))
                throw new DeserialisationException("Serialised log entry xml is not valid.");

            DateTime timestamp;
            LogStatus status;
            Guid key, groupKey, targetKey, instanceKey;

            if (!Guid.TryParse(doc.Root.Attribute("key").Value, out key))
                throw new DeserialisationException("Serialised log entry key is not a valid guid.");
            if (!DateTime.TryParse(doc.Root.Element("timestamp").Value, out timestamp))
                throw new DeserialisationException("Serialised log entry timestamp is not a valid datetime.");
            if (!Enum.TryParse(doc.Root.Element("status").Value, out status))
                throw new DeserialisationException("Serialised log entry status is not a valid log status.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised log entry group key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("targetKey").Value, out targetKey))
                throw new DeserialisationException("Serialised log entry target key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("instanceKey").Value, out instanceKey))
                throw new DeserialisationException("Serialised log entry instance key is not a valid guid.");

            var logEntry = new LogEntry()
            {
                Key = key,
                Timestamp = timestamp.ToUniversalTime(),
                Status = status,
                GroupKey = groupKey,
                RoleKey = targetKey,
                ServerKey = instanceKey,
                LogContent = doc.Root.Element("logContent").Value,
            };

            return logEntry;
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
                            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.Schemas.LogEntry.xsd"))
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
            var filename = string.Format(
                "{0}-{1}-{2}",
                Hashing.CreateHash(Timestamp),
                (char)Status,
                Utils.Indexes.EncodeGuid(Key));

            var entries = new List<string>(1);
            var tokens = (new SimpleTokeniser()).Tokenise(Enum.GetName(Status.GetType(), Status)).ToList();

            // Server specific index
            entries.Add(string.Format("s/{0}/li/e/{1}", Utils.Indexes.EncodeGuid(ServerKey), filename));
            entries.AddRange(
                tokens.Select(
                    token =>
                        string.Format("s/{0}/li/t/{1}/{2}",
                         Utils.Indexes.EncodeGuid(ServerKey),
                         Indexes.IndexEntries.GetTokenHash(token),
                         filename)));

            return entries;
        }
    }

    public class LogEntryPage
    {
        public Guid ServerKey { get; set; }
        public IEnumerable<LogEntryListItem> Items { get; set; }
        public string Marker { get; set; }
        public string NextMarker { get; set; }
        public int PageSize { get; set; }
        public bool IsTruncated { get; set; }
    }

    public class LogEntryListItem
    {
        public LogEntryListItem()
        {
        }

        public LogEntryListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 3)
                throw new ArgumentException("A log entry path index entry must contain exactly 3 segments.", "path");

            Marker = Utils.Indexes.GetIndexFileName(path);
            Timestamp = Indexes.Hashing.UnHashDate(segments[0]);
            Status = (LogStatus)segments[1][0];
            Key = Utils.Indexes.DecodeGuid(segments[2]);
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public DateTime Timestamp { get; set; }
        public LogStatus Status { get; set; }
    }

    public enum LogStatus
    {
        Ok = 'a',
        Warning = 'b',
        Error = 'c',
        Fatal = 'd',
    }
}
