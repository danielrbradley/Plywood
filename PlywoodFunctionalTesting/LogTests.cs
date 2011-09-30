using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Plywood.Tests.Functional
{
    static class LogTests
    {
        public static void Run(ControllerConfiguration context, Guid instanceKey)
        {
            var logsController = new Logs(context);

            var searchEmpty = logsController.GetLogEntryPage(instanceKey);
            Debug.Assert(searchEmpty.LogEntries.Count() == 0);
            Debug.Assert(searchEmpty.InstanceKey == instanceKey);

            var logEntry1 = new LogEntry() { InstanceKey = instanceKey, LogContent = "Now this is the story all about how\r\nMy life got flipped, turned upside down\r\nAnd I'd like to take a minute just sit right there\r\nI'll tell you how I became the prince of a town called Bel-air" };
            logsController.Create(logEntry1);

            var createdLogEntry1 = logsController.Get(instanceKey, logEntry1.Timestamp, logEntry1.Status);
            Debug.Assert(createdLogEntry1.Timestamp == logEntry1.Timestamp);
            Debug.Assert(createdLogEntry1.InstanceKey == logEntry1.InstanceKey);
            Debug.Assert(createdLogEntry1.Status == logEntry1.Status);
            Debug.Assert(createdLogEntry1.LogContent == logEntry1.LogContent);

            var searchSingle = logsController.GetLogEntryPage(instanceKey);
            Debug.Assert(searchSingle.LogEntries.Count() == 1);
            Debug.Assert(searchSingle.LogEntries.ElementAt(0).Timestamp == logEntry1.Timestamp);
            Debug.Assert(searchSingle.LogEntries.ElementAt(0).Status == LogStatus.Ok);

            var logEntry2 = new LogEntry() { InstanceKey = instanceKey, Status = LogStatus.Warning, LogContent = "In west Philadelphia born and raised\r\nOn the playground where I spent most of my days\r\nChilling out, maxing, relaxing all cool\r\nAnd all shooting some b-ball outside of the school\r\nWhen a couple of guys, they were up to no good\r\nStarted making trouble in my neighbourhood\r\nI got in one little fight and my mom got scared\r\nAnd said \"You're moving with your auntie and uncle in Bel-air\"" };
            logsController.Create(logEntry2);

            var createdLogEntry2 = logsController.Get(instanceKey, logEntry2.Timestamp, logEntry2.Status);
            Debug.Assert(createdLogEntry2.Timestamp == logEntry2.Timestamp);
            Debug.Assert(createdLogEntry2.InstanceKey == logEntry2.InstanceKey);
            Debug.Assert(createdLogEntry2.Status == logEntry2.Status);
            Debug.Assert(createdLogEntry2.LogContent == logEntry2.LogContent);

            var search2 = logsController.GetLogEntryPage(instanceKey);
            Debug.Assert(search2.LogEntries.Count() == 2);
            Debug.Assert(search2.LogEntries.ElementAt(0).Timestamp == logEntry2.Timestamp);
            Debug.Assert(search2.LogEntries.ElementAt(0).Status == LogStatus.Warning);
            Debug.Assert(search2.LogEntries.ElementAt(1).Timestamp == logEntry1.Timestamp);

            var searchFirst = logsController.GetLogEntryPage(instanceKey, pageSize: 1);
            Debug.Assert(searchFirst.PageSize == 1);
            Debug.Assert(searchFirst.LogEntries.Count() == 1);
            Debug.Assert(searchFirst.LogEntries.ElementAt(0).Timestamp == logEntry2.Timestamp);

            var searchNext = logsController.GetLogEntryPage(instanceKey, searchFirst.NextMarker);
            Debug.Assert(searchNext.StartMarker == searchFirst.NextMarker);
            Debug.Assert(searchNext.LogEntries.Count() == 1);
            Debug.Assert(searchNext.LogEntries.ElementAt(0).Timestamp == logEntry1.Timestamp);

        }
    }
}
