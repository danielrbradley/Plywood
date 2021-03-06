﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Reflection;
using System.Xml;

namespace Plywood
{
    public class LogEntry
    {
        #region Constructors

        public LogEntry()
        {
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
            this.Timestamp = prototype.Timestamp;
            this.Status = prototype.Status;
            this.GroupKey = prototype.GroupKey;
            this.TargetKey = prototype.TargetKey;
            this.InstanceKey = prototype.InstanceKey;
            this.LogContent = prototype.LogContent;
        }

        #endregion

        public DateTime Timestamp { get; set; }
        public LogStatus Status { get; set; }
        public Guid GroupKey { get; set; }
        public Guid TargetKey { get; set; }
        public Guid InstanceKey { get; set; }
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

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", "yes"),
                new XElement("logEntry",
                    new XAttribute("timestamp", logEntry.Timestamp),
                    new XAttribute("status", logEntry.Status),
                    new XElement("groupKey", logEntry.GroupKey),
                    new XElement("targetKey", logEntry.TargetKey),
                    new XElement("instanceKey", logEntry.InstanceKey),
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
            Guid groupKey, targetKey, instanceKey;

            if (!DateTime.TryParse(doc.Root.Attribute("timestamp").Value, out timestamp))
                throw new DeserialisationException("Serialised log entry timestamp is not a valid datetime.");
            if (!Enum.TryParse(doc.Root.Attribute("status").Value, out status))
                throw new DeserialisationException("Serialised log entry status is not a valid log status.");
            if (!Guid.TryParse(doc.Root.Element("groupKey").Value, out groupKey))
                throw new DeserialisationException("Serialised log entry group key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("targetKey").Value, out targetKey))
                throw new DeserialisationException("Serialised log entry target key is not a valid guid.");
            if (!Guid.TryParse(doc.Root.Element("instanceKey").Value, out instanceKey))
                throw new DeserialisationException("Serialised log entry instance key is not a valid guid.");

            var logEntry = new LogEntry()
            {
                Timestamp = timestamp.ToUniversalTime(),
                Status = status,
                GroupKey = groupKey,
                TargetKey = targetKey,
                InstanceKey = instanceKey,
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

    }

    public class LogEntryPage
    {
        public Guid InstanceKey { get; set; }
        public IEnumerable<LogEntryListItem> LogEntries { get; set; }
        public string StartMarker { get; set; }
        public string NextMarker { get; set; }
        public int PageSize { get; set; }
    }

    public class LogEntryListItem
    {
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
