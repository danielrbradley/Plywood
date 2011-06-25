using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;
using Amazon.S3;
using Amazon.S3.Model;
using System.Text.RegularExpressions;

namespace Plywood
{
    public class Logs : ControllerBase
    {
        public const string STR_LOGS_CONTAINER_PATH = "logs";

        public Logs() : base() { }
        public Logs(ControllerConfiguration context) : base(context) { }

        public void AddLogEntry(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException("logEntry", "You cannot add a null log entry.");
            if (logEntry.InstanceKey == Guid.Empty)
                throw new ArgumentException("Instance key must be set to a non-empty guid.");

            using (var stream = logEntry.Serialise())
            {
                var instancesController = new Instances(Context);
                if (!instancesController.InstanceExists(logEntry.InstanceKey))
                    throw new InstanceNotFoundException(String.Format("Instance with the key \"{0}\" could not be found.", logEntry.InstanceKey));

                try
                {
                    using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                    {
                        var entryPath = GetInstanceLogEntryPath(logEntry.InstanceKey, logEntry.Timestamp, logEntry.Status);

                        using (var putResponse = client.PutObject(new PutObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = entryPath,
                            InputStream = stream,
                        })) { }
                    }
                }
                catch (AmazonS3Exception awsEx)
                {
                    throw new DeploymentException("Failed creating log entry.", awsEx);
                }
            }
        }

        public LogEntryPage GetLogEntryPage(Guid instanceKey, string marker = null, int pageSize = 50)
        {
            if (pageSize < 1)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 1.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.ListObjects(new ListObjectsRequest()
                    {
                        BucketName = Context.BucketName,
                        Prefix = string.Format("{0}/{1}/", STR_LOGS_CONTAINER_PATH, instanceKey.ToString("N")),
                        MaxKeys = pageSize,
                        Marker = marker,
                    }))
                    {
                        return new LogEntryPage()
                        {
                            InstanceKey = instanceKey,
                            StartMarker = marker,
                            PageSize = pageSize,
                            NextMarker = res.NextMarker,
                            LogEntries = res.S3Objects.Select(o =>
                            {
                                DateTime timestamp;
                                LogStatus status;
                                bool timestampSucceeded = TryGetTimestampFromKey(o.Key, out timestamp);
                                bool statusSucceeded = TryGetStateFromKey(o.Key, out status);
                                return new { Timestmp = timestamp, Status = status, ParseSucceeded = timestampSucceeded && statusSucceeded };
                            })
                            .Where(o => o.ParseSucceeded)
                            .Select(o => new LogEntryListItem() { Timestamp = o.Timestmp, Status = o.Status })
                            .ToList()
                        };
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException("Failed listing logs.", awsEx);
            }
        }

        public LogEntry GetLogEntry(Guid instanceKey, DateTime timestamp, LogStatus status)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObject(new GetObjectRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = GetInstanceLogEntryPath(instanceKey, timestamp, status),
                    }))
                    {
                        using (var stream = res.ResponseStream)
                        {
                            return new LogEntry(stream);
                        }
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                if (awsEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new LogEntryNotFoundException(string.Format("Could not find the log entry at timestamp {0} for instance with key: {1}", timestamp, instanceKey), awsEx);
                }
                else
                {
                    throw new DeploymentException(string.Format("Failed getting the log entry at timestamp {0} for instance with key: {1}", timestamp, instanceKey), awsEx);
                }
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

        public static string GetInstanceLogEntryPath(Guid instanceKey, DateTime entryTimestamp, LogStatus status)
        {
            return string.Format("{0}/{1}/{2}-{3}", STR_LOGS_CONTAINER_PATH, instanceKey.ToString("N"), Serialisation.SerialiseDateReversed(entryTimestamp), (char)status);
        }
    }
}
