﻿using System;
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

        public void Create(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException("logEntry", "You cannot add a null log entry.");
            if (logEntry.ServerKey == Guid.Empty)
                throw new ArgumentException("Instance key must be set to a non-empty guid.");

            using (var stream = logEntry.Serialise())
            {
                var instancesController = new Servers(StorageProvider);
                if (!instancesController.Exists(logEntry.ServerKey))
                    throw new InstanceNotFoundException(String.Format("Instance with the key \"{0}\" could not be found.", logEntry.ServerKey));

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

        public LogEntryPage Search(Guid serverKey, string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 0.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                var startLocation = string.Format("s/{0}/li", serverKey.ToString("N"));

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
                    ServerKey = serverKey,
                    Marker = marker,
                    Items = entries,
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

        public LogEntry Get(Guid key)
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
