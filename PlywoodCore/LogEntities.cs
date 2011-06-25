using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using System.IO;

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
            : base()
        {
            Extend(LogEntry.Parse(source));
        }

        public LogEntry(Stream source)
            : base()
        {
            Extend(LogEntry.Parse(source));
        }

        public LogEntry(TextReader source)
            : base()
        {
            Extend(LogEntry.Parse(source));
        }

        #endregion

        public DateTime Timestamp { get; set; }
        public LogStatus Status { get; set; }
        public Guid InstanceKey { get; set; }
        public string LogContent { get; set; }

        private void Extend(LogEntry prototype)
        {
            this.Timestamp = prototype.Timestamp;
            this.Status = prototype.Status;
            this.InstanceKey = prototype.InstanceKey;
            this.LogContent = prototype.LogContent;
        }

        public Stream Serialise()
        {
            return LogEntry.Serialise(this);
        }

        #region Static Serialisation Methods

        public static Stream Serialise(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException("logEntry", "Version cannot be null.");

            var values = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("Timestamp", Serialisation.SerialiseDateReversed(logEntry.Timestamp)),
                new KeyValuePair<string,string>("Status", logEntry.Status.ToString()),
                new KeyValuePair<string,string>("InstanceKey", logEntry.InstanceKey.ToString("N")),
                new KeyValuePair<string,string>("LogContent", logEntry.LogContent),
            };

            return Serialisation.Serialise(values);
        }

        public static LogEntry Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static LogEntry Parse(Stream source)
        {
            return Parse(new StreamReader(source));
        }

        public static LogEntry Parse(TextReader source)
        {
            var logEntry = new LogEntry();
            var properties = Serialisation.ReadProperties(source);

            if (!properties.ContainsKey("Timestamp"))
                throw new DeserialisationException("Failed deserialising instance: missing property \"Timestamp\"");
            if (!properties.ContainsKey("Status"))
                throw new DeserialisationException("Failed deserialising instance: missing property \"Status\"");
            if (!properties.ContainsKey("InstanceKey"))
                throw new DeserialisationException("Failed deserialising instance: missing property \"InstanceKey\"");
            if (!properties.ContainsKey("LogContent"))
                throw new DeserialisationException("Failed deserialising instance: missing property \"LogContent\"");

            DateTime timestamp;
            LogStatus status;
            Guid instanceKey;

            try
            {
                timestamp = Serialisation.DeserialiseReversedDate(properties["Timestamp"]);
            }
            catch (Exception)
            {
                throw new DeserialisationException("Failed deserialising version: invalid property value for \"Timestamp\"");
            }

            if (!Enum.TryParse<LogStatus>(properties["Status"], true, out status))
                throw new DeserialisationException("Failed deserialising version: invalid property value for \"Status\"");
            if (!Guid.TryParseExact(properties["InstanceKey"], "N", out instanceKey))
                throw new DeserialisationException("Failed deserialising version: invalid property value for \"InstanceKey\"");

            logEntry.Timestamp = timestamp;
            logEntry.Status = status;
            logEntry.InstanceKey = instanceKey;
            logEntry.LogContent = properties["LogContent"];

            return logEntry;
        }

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
