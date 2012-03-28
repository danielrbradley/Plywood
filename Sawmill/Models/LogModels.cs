using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Plywood.Sawmill.Models
{
    public class LogIndex
    {
        public LogEntryPage LogEntryPage { get; set; }
        public Instance Instance { get; set; }
        public Target Target { get; set; }
        public Group Group { get; set; }
    }

    public class LogDetails
    {
        public LogEntry LogEntry { get; set; }
        public Instance Instance { get; set; }
        public Target Target { get; set; }
        public Group Group { get; set; }
    }
}