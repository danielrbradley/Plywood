using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public class LogEntry
    {
        public LogEntry()
        {
            Timestamp = DateTime.UtcNow;
            Status = LogStatus.Ok;
        }

        public DateTime Timestamp { get; set; }
        public LogStatus Status { get; set; }
        public Guid InstanceKey { get; set; }
        public string LogContent { get; set; }
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
