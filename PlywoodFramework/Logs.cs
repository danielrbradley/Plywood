using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using System.Text.RegularExpressions;
using Plywood.Indexes;

namespace Plywood
{
    public class Logs : ControllerBase
    {
        public const string STR_LOGS_CONTAINER_PATH = "logs";

        public Logs(IStorageProvider provider) : base(provider) { }

        public void AddLogEntry(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException("logEntry", "You cannot add a null log entry.");
            if (logEntry.InstanceKey == Guid.Empty)
                throw new ArgumentException("Instance key must be set to a non-empty guid.");

            using (var stream = logEntry.Serialise())
            {
                var instancesController = new Instances(StorageProvider);
                if (!instancesController.InstanceExists(logEntry.InstanceKey))
                    throw new InstanceNotFoundException(String.Format("Instance with the key \"{0}\" could not be found.", logEntry.InstanceKey));

                try
                {
                    StorageProvider.PutFile(Paths.GetLogDetailsPath(logEntry.Key), stream);
                    var indexes = new Indexes.IndexEntries(StorageProvider);
                    indexes.PutEntity(logEntry);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating log entry.", ex);
                }
            }
        }

        public LogEntryPage SearchLogs(Guid instanceKey, string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 1.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                var startLocation = string.Format("t/{0}/ai", instanceKey.ToString("N"));

                var basePaths = new List<string>();
                if (string.IsNullOrWhiteSpace(query))
                {
                    basePaths.Add(string.Format("{0}/e", startLocation));
                }
                else
                {
                    basePaths.AddRange(
                        new SimpleTokeniser().Tokenise(query).Select(
                            token =>
                                string.Format("{0}/t/{1}", startLocation, Indexes.IndexEntries.GetTokenHash(token))));
                }

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var entries = rawResults.FileNames.Select(fileName => new LogEntryListItem(fileName));

                var logPage = new LogEntryPage()
                {
                    InstanceKey = instanceKey,
                    Marker = marker,
                    LogEntries = entries,
                    PageSize = pageSize,
                    NextMarker = entries.Any() ? entries.Last().Marker : marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return logPage;
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed listing logs.", ex);
            }
        }

        public LogEntry GetLogEntry(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetLogDetailsPath(key)))
                {
                    return new LogEntry(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting the log entry \"{0}\"", key), ex);
            }
        }

        public static DateTime GetTimestampFromKey(string key)
        {
            if (key == null)
                throw new ArgumentNullException("key", "Object key cannot be null.");

            DateTime timestamp;
            bool succeeded = TryGetTimestampFromKey(key, out timestamp);
            if (!succeeded)
                throw new FormatException("Failed parsing timestamp from S3 Object key.");
            return timestamp;
        }

        public static bool TryGetTimestampFromKey(string key, out DateTime timestamp)
        {
            if (key == null)
            {
                timestamp = DateTime.MinValue;
                return false;
            }

            int lastKeySeparatorIndex = key.LastIndexOf('/');

            if (lastKeySeparatorIndex < 0 || lastKeySeparatorIndex + 1 > key.Length - 1)
            {
                timestamp = DateTime.MinValue;
                return false;
            }

            // Must be 16 hex digits.
            if (key.Length - (lastKeySeparatorIndex + 1) < 16)
            {
                timestamp = DateTime.MinValue;
                return false;
            }

            string lastKeySection = key.Substring(lastKeySeparatorIndex + 1, 16);

            if (!Regex.IsMatch(lastKeySection, @"^[0-9a-f]{16}$", RegexOptions.IgnoreCase))
            {
                timestamp = DateTime.MinValue;
                return false;
            }

            try
            {
                timestamp = Serialisation.DeserialiseReversedDate(lastKeySection);
            }
            catch (Exception)
            {
                timestamp = DateTime.MinValue;
                return false;
            }
            return true;
        }

        public static bool TryGetStateFromKey(string key, out LogStatus status)
        {
            if (key == null)
            {
                status = LogStatus.Ok;
                return false;
            }

            // Just take last character.
            char stateChar = key.Last();

            try
            {
                status = (LogStatus)stateChar;
                return true;
            }
            catch (Exception)
            {
                status = LogStatus.Ok;
                return false;
            }
        }
    }
}
