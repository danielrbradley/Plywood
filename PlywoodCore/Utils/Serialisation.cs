using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Plywood.Utils
{
    public static class Serialisation
    {
        #region Entity Parsing

        public static Group ParseGroup(TextReader source)
        {
            var group = new Group();
            var properties = ReadProperties(source);

            if (!properties.ContainsKey("Key"))
            {
                throw new DeserialisationException("Failed deserialising group: missing property \"Key\"");
            }
            if (!properties.ContainsKey("Name"))
            {
                throw new DeserialisationException("Failed deserialising group: missing property \"Name\"");
            }

            Guid key;
            if (!Guid.TryParseExact(properties["Key"], "N", out key))
            {
                throw new DeserialisationException("Failed deserialising group: invalid property value for \"Key\"");
            }

            group.Key = key;
            group.Name = properties["Name"];

            properties.Remove("Key");
            properties.Remove("Name");

            group.Tags = properties;

            return group;
        }

        public static App ParseApp(TextReader source)
        {
            var app = new App();
            var properties = ReadProperties(source);

            if (!properties.ContainsKey("Key"))
            {
                throw new DeserialisationException("Failed deserialising app: missing property \"Key\"");
            }
            if (!properties.ContainsKey("Name"))
            {
                throw new DeserialisationException("Failed deserialising app: missing property \"Name\"");
            }
            if (!properties.ContainsKey("GroupKey"))
            {
                throw new DeserialisationException("Failed deserialising app: missing property \"GroupKey\"");
            }
            if (!properties.ContainsKey("DeploymentDirectory"))
            {
                throw new DeserialisationException("Failed deserialising app: missing property \"DeploymentDirectory\"");
            }

            Guid key;
            if (!Guid.TryParseExact(properties["Key"], "N", out key))
            {
                throw new DeserialisationException("Failed deserialising app: invalid property value for \"Key\"");
            }
            Guid groupKey;
            if (!Guid.TryParseExact(properties["GroupKey"], "N", out groupKey))
            {
                throw new DeserialisationException("Failed deserialising app: invalid property value for \"GroupKey\"");
            }

            app.Key = key;
            app.Name = properties["Name"];
            app.GroupKey = groupKey;
            app.DeploymentDirectory = properties["DeploymentDirectory"];

            properties.Remove("Key");
            properties.Remove("Name");
            properties.Remove("GroupKey");
            properties.Remove("DeploymentDirectory");

            if (properties.ContainsKey("MajorVersion"))
            {
                app.MajorVersion = properties["MajorVersion"];
                properties.Remove("MajorVersion");
            }

            if (properties.ContainsKey("Revision"))
            {
                int revision;
                if (int.TryParse(properties["Revision"], out revision))
                    app.Revision = revision;
                properties.Remove("Revision");
            }

            app.Tags = properties;

            return app;
        }

        public static Target ParseTarget(TextReader source)
        {
            var target = new Target();
            var properties = ReadProperties(source);

            if (!properties.ContainsKey("Key"))
            {
                throw new DeserialisationException("Failed deserialising target: missing property \"Key\"");
            }
            if (!properties.ContainsKey("Name"))
            {
                throw new DeserialisationException("Failed deserialising target: missing property \"Name\"");
            }
            if (!properties.ContainsKey("GroupKey"))
            {
                throw new DeserialisationException("Failed deserialising target: missing property \"GroupKey\"");
            }

            Guid key;
            if (!Guid.TryParseExact(properties["Key"], "N", out key))
            {
                throw new DeserialisationException("Failed deserialising target: invalid property value for \"Key\"");
            }
            Guid groupKey;
            if (!Guid.TryParseExact(properties["GroupKey"], "N", out groupKey))
            {
                throw new DeserialisationException("Failed deserialising target: invalid property value for \"GroupKey\"");
            }

            target.Key = key;
            target.Name = properties["Name"];
            target.GroupKey = groupKey;

            properties.Remove("Key");
            properties.Remove("Name");
            properties.Remove("GroupKey");

            target.Tags = properties;

            return target;
        }

        public static Version ParseVersion(TextReader source)
        {
            var version = new Version();
            var properties = ReadProperties(source);

            if (!properties.ContainsKey("Key"))
                throw new DeserialisationException("Failed deserialising version: missing property \"Key\"");
            if (!properties.ContainsKey("Name"))
                throw new DeserialisationException("Failed deserialising version: missing property \"Name\"");
            if (!properties.ContainsKey("Timestamp"))
                throw new DeserialisationException("Failed deserialising version: missing property \"Timestamp\"");
            if (!properties.ContainsKey("AppKey"))
                throw new DeserialisationException("Failed deserialising version: missing property \"AppKey\"");
            if (!properties.ContainsKey("GroupKey"))
                throw new DeserialisationException("Failed deserialising version: missing property \"GroupKey\"");

            Guid key;
            DateTime timestamp;
            Guid appKey;
            Guid groupKey;

            if (!Guid.TryParseExact(properties["Key"], "N", out key))
                throw new DeserialisationException("Failed deserialising version: invalid property value for \"Key\"");
            if (!DateTime.TryParse(properties["Timestamp"], out timestamp))
                throw new DeserialisationException("Failed deserialising version: invalid property value for \"Timestamp\"");
            if (!Guid.TryParseExact(properties["AppKey"], "N", out appKey))
                throw new DeserialisationException("Failed deserialising version: invalid property value for \"AppKey\"");
            if (!Guid.TryParseExact(properties["GroupKey"], "N", out groupKey))
                throw new DeserialisationException("Failed deserialising version: invalid property value for \"GroupKey\"");

            version.Key = key;
            version.Name = properties["Name"];
            version.Timestamp = timestamp;
            version.AppKey = appKey;
            version.GroupKey = groupKey;

            properties.Remove("Key");
            properties.Remove("Name");
            properties.Remove("Timestamp");
            properties.Remove("AppKey");
            properties.Remove("GroupKey");

            version.Tags = properties;

            return version;
        }

        public static Instance ParseInstance(TextReader source)
        {
            var instance = new Instance();
            var properties = ReadProperties(source);

            if (!properties.ContainsKey("Key"))
                throw new DeserialisationException("Failed deserialising instance: missing property \"Key\"");
            if (!properties.ContainsKey("Name"))
                throw new DeserialisationException("Failed deserialising instance: missing property \"Name\"");
            if (!properties.ContainsKey("TargetKey"))
                throw new DeserialisationException("Failed deserialising instance: missing property \"TargetKey\"");

            Guid key;
            Guid targetKey;

            if (!Guid.TryParseExact(properties["Key"], "N", out key))
                throw new DeserialisationException("Failed deserialising version: invalid property value for \"Key\"");
            if (!Guid.TryParseExact(properties["TargetKey"], "N", out targetKey))
                throw new DeserialisationException("Failed deserialising version: invalid property value for \"TargetKey\"");

            instance.Key = key;
            instance.Name = properties["Name"];
            instance.TargetKey = targetKey;

            properties.Remove("Key");
            properties.Remove("Name");
            properties.Remove("TargetKey");

            instance.Tags = properties;

            return instance;
        }

        public static LogEntry ParseLogEntry(TextReader source)
        {
            var logEntry = new LogEntry();
            var properties = ReadProperties(source);

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

        public static Guid ParseKey(TextReader source)
        {
            var keyText = source.ReadLine();
            Guid remoteKey;
            if (keyText == null || !Guid.TryParseExact(keyText, "N", out remoteKey))
            {
                throw new DeserialisationException("Failed reading key from source.");
            }
            return remoteKey;
        }

        public static ControllerConfiguration ParseControllerConfiguration(TextReader source)
        {
            var properties = Utils.Serialisation.ReadProperties(source);

            if (!properties.ContainsKey("AwsAccessKeyId"))
                throw new DeserialisationException("Failed deserialising controller configuration: missing property \"AwsAccessKeyId\"");
            if (!properties.ContainsKey("AwsSecretAccessKey"))
                throw new DeserialisationException("Failed deserialising controller configuration: missing property \"AwsSecretAccessKey\"");
            if (!properties.ContainsKey("BucketName"))
                throw new DeserialisationException("Failed deserialising controller configuration: missing property \"BucketName\"");

            return new ControllerConfiguration()
            {
                AwsAccessKeyId = properties["AwsAccessKeyId"],
                AwsSecretAccessKey = properties["AwsSecretAccessKey"],
                BucketName = properties["BucketName"],
            };
        }

        public static DeploymentConfiguration ParsePullConfiguration(TextReader source)
        {
            var properties = Utils.Serialisation.ReadProperties(source);

            if (!properties.ContainsKey("AwsAccessKeyId"))
                throw new DeserialisationException("Failed deserialising pull configuration: missing property \"AwsAccessKeyId\"");
            if (!properties.ContainsKey("AwsSecretAccessKey"))
                throw new DeserialisationException("Failed deserialising pull configuration: missing property \"AwsSecretAccessKey\"");
            if (!properties.ContainsKey("BucketName"))
                throw new DeserialisationException("Failed deserialising pull configuration: missing property \"BucketName\"");
            if (!properties.ContainsKey("CheckFrequency"))
                throw new DeserialisationException("Failed deserialising pull configuration: missing property \"CheckFrequency\"");
            if (!properties.ContainsKey("DeploymentDirectory"))
                throw new DeserialisationException("Failed deserialising pull configuration: missing property \"DeploymentDirectory\"");
            if (!properties.ContainsKey("TargetKey"))
                throw new DeserialisationException("Failed deserialising pull configuration: missing property \"TargetKey\"");

            TimeSpan checkFrequency;
            Guid targetKey;

            if (!TimeSpan.TryParse(properties["CheckFrequency"], out checkFrequency))
                throw new DeserialisationException("Failed deserialising pull configuration: invalid property value for \"CheckFrequency\"");
            if (!Guid.TryParseExact(properties["TargetKey"], "N", out targetKey))
                throw new DeserialisationException("Failed deserialising pull configuration: invalid property value for \"TargetKey\"");

            var config = new DeploymentConfiguration()
            {
                CheckFrequency = checkFrequency,
                AwsAccessKeyId = properties["AwsAccessKeyId"],
                AwsSecretAccessKey = properties["AwsSecretAccessKey"],
                BucketName = properties["BucketName"],
                DeploymentDirectory = properties["DeploymentDirectory"],
                TargetKey = targetKey,
            };

            Guid instanceKey;
            if (properties.ContainsKey("InstanceKey") && Guid.TryParseExact(properties["InstanceKey"], "N", out instanceKey))
                config.InstanceKey = instanceKey;

            return config;
        }

        #endregion

        #region Entity Parsing Overloads

        public static Group ParseGroup(string source)
        {
            return ParseGroup(new StringReader(source));
        }

        public static Group ParseGroup(Stream source)
        {
            return ParseGroup(new StreamReader(source));
        }

        public static App ParseApp(string source)
        {
            return ParseApp(new StringReader(source));
        }

        public static App ParseApp(Stream source)
        {
            return ParseApp(new StreamReader(source));
        }

        public static Target ParseTarget(string source)
        {
            return ParseTarget(new StringReader(source));
        }

        public static Target ParseTarget(Stream source)
        {
            return ParseTarget(new StreamReader(source));
        }

        public static Version ParseVersion(string source)
        {
            return ParseVersion(new StringReader(source));
        }

        public static Version ParseVersion(Stream source)
        {
            return ParseVersion(new StreamReader(source));
        }

        public static Instance ParseInstance(string source)
        {
            return ParseInstance(new StringReader(source));
        }

        public static Instance ParseInstance(Stream source)
        {
            return ParseInstance(new StreamReader(source));
        }

        public static LogEntry ParseLogEntry(string source)
        {
            return ParseLogEntry(new StringReader(source));
        }

        public static LogEntry ParseLogEntry(Stream source)
        {
            return ParseLogEntry(new StreamReader(source));
        }

        public static Guid ParseKey(string source)
        {
            return ParseKey(new StringReader(source));
        }

        public static Guid ParseKey(Stream source)
        {
            return ParseKey(new StreamReader(source));
        }

        public static ControllerConfiguration ParseControllerConfiguration(string source)
        {
            return ParseControllerConfiguration(new StringReader(source));
        }

        public static ControllerConfiguration ParseControllerConfiguration(Stream source)
        {
            return ParseControllerConfiguration(new StreamReader(source));
        }

        public static DeploymentConfiguration ParsePullConfiguration(string source)
        {
            return ParsePullConfiguration(new StringReader(source));
        }

        public static DeploymentConfiguration ParsePullConfiguration(Stream source)
        {
            return ParsePullConfiguration(new StreamReader(source));
        }

        #endregion

        #region Entity Serialisation

        public static Stream Serialise(Group group)
        {
            if (group == null)
                throw new ArgumentNullException("group", "Group cannot be null.");
            if (!Validation.IsNameValid(group.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");
            if (group.Tags != null)
            {
                if (group.Tags.ContainsKey("Key"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Key\"");
                if (group.Tags.ContainsKey("Name"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Name\"");
            }

            var values = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("Key", group.Key.ToString("N")),
                new KeyValuePair<string,string>("Name", group.Name),
            };

            if (group.Tags != null)
                values.AddRange(group.Tags.ToList());

            return Serialise(values);
        }

        public static Stream Serialise(App app)
        {
            if (app == null)
                throw new ArgumentNullException("app", "App cannot be null.");
            if (!Validation.IsNameValid(app.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");
            if (!Validation.IsDirectoryNameValid(app.DeploymentDirectory))
                throw new ArgumentException("Deployment directory must be a valid directory name.");
            if (!Validation.IsMajorVersionValid(app.MajorVersion))
                throw new ArgumentException("Major version must be numbers separated by '.'");
            if (app.Revision < 0)
                throw new ArgumentOutOfRangeException("app.Revision", app.Revision, "App revision must be a positive integer.");

            if (app.Tags != null)
            {
                if (app.Tags.ContainsKey("Key"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Key\"");
                if (app.Tags.ContainsKey("Name"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Name\"");
                if (app.Tags.ContainsKey("GroupKey"))
                    throw new ArgumentException("Tags cannot use the reserved name \"GroupKey\"");
                if (app.Tags.ContainsKey("DeploymentDirectory"))
                    throw new ArgumentException("Tags cannot use the reserved name \"DeploymentDirectory\"");
                if (app.Tags.ContainsKey("MajorVersion"))
                    throw new ArgumentException("Tags cannot use the reserved name \"MajorVersion\"");
                if (app.Tags.ContainsKey("Revision"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Revision\"");
            }

            var values = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("Key", app.Key.ToString("N")),
                new KeyValuePair<string,string>("Name", app.Name),
                new KeyValuePair<string,string>("GroupKey", app.GroupKey.ToString("N")),
                new KeyValuePair<string,string>("DeploymentDirectory", app.DeploymentDirectory),
                new KeyValuePair<string,string>("MajorVersion", app.MajorVersion),
                new KeyValuePair<string,string>("Revision", app.Revision.ToString()),
            };

            if (app.Tags != null)
                values.AddRange(app.Tags.ToList());

            return Serialise(values);
        }

        public static Stream Serialise(Target target)
        {
            if (target == null)
                throw new ArgumentNullException("target", "Target cannot be null.");
            if (!Validation.IsNameValid(target.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");
            if (target.Tags != null)
            {
                if (target.Tags.ContainsKey("Key"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Key\"");
                if (target.Tags.ContainsKey("Name"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Name\"");
                if (target.Tags.ContainsKey("GroupKey"))
                    throw new ArgumentException("Tags cannot use the reserved name \"GroupKey\"");
            }

            var values = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("Key", target.Key.ToString("N")),
                new KeyValuePair<string,string>("Name", target.Name),
                new KeyValuePair<string,string>("GroupKey", target.GroupKey.ToString("N")),
            };

            if (target.Tags != null)
                values.AddRange(target.Tags.ToList());

            return Serialise(values);
        }

        public static Stream Serialise(Version version)
        {
            if (version == null)
                throw new ArgumentNullException("version", "Version cannot be null.");
            if (!Validation.IsNameValid(version.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");
            if (version.Tags != null)
            {
                if (version.Tags.ContainsKey("Key"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Key\"");
                if (version.Tags.ContainsKey("Name"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Name\"");
                if (version.Tags.ContainsKey("Timestamp"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Timestamp\"");
                if (version.Tags.ContainsKey("AppKey"))
                    throw new ArgumentException("Tags cannot use the reserved name \"AppKey\"");
                if (version.Tags.ContainsKey("GroupKey"))
                    throw new ArgumentException("Tags cannot use the reserved name \"GroupKey\"");
            }

            var values = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("Key", version.Key.ToString("N")),
                new KeyValuePair<string,string>("Name", version.Name),
                new KeyValuePair<string,string>("Timestamp", version.Timestamp.ToString("s")),
                new KeyValuePair<string,string>("AppKey", version.AppKey.ToString("N")),
                new KeyValuePair<string,string>("GroupKey", version.GroupKey.ToString("N")),
            };

            if (version.Tags != null)
                values.AddRange(version.Tags.ToList());

            return Serialise(values);
        }

        public static Stream Serialise(Instance instance)
        {
            if (instance == null)
                throw new ArgumentNullException("version", "Version cannot be null.");
            if (!Validation.IsNameValid(instance.Name))
                throw new ArgumentException("Name must be valid (not blank & only a single line).");
            if (instance.Tags != null)
            {
                if (instance.Tags.ContainsKey("Key"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Key\"");
                if (instance.Tags.ContainsKey("Name"))
                    throw new ArgumentException("Tags cannot use the reserved name \"Name\"");
                if (instance.Tags.ContainsKey("TargetKey"))
                    throw new ArgumentException("Tags cannot use the reserved name \"TargetKey\"");
            }

            var values = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("Key", instance.Key.ToString("N")),
                new KeyValuePair<string,string>("Name", instance.Name),
                new KeyValuePair<string,string>("TargetKey", instance.TargetKey.ToString("N")),
            };

            if (instance.Tags != null)
                values.AddRange(instance.Tags.ToList());

            return Serialise(values);
        }

        public static Stream Serialise(LogEntry logEntry)
        {
            if (logEntry == null)
                throw new ArgumentNullException("logEntry", "Version cannot be null.");

            var values = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("Timestamp", SerialiseDateReversed(logEntry.Timestamp)),
                new KeyValuePair<string,string>("Status", logEntry.Status.ToString()),
                new KeyValuePair<string,string>("InstanceKey", logEntry.InstanceKey.ToString("N")),
                new KeyValuePair<string,string>("LogContent", logEntry.LogContent),
            };

            return Serialise(values);
        }

        public static Stream Serialise(Guid key)
        {
            var stream = new MemoryStream(32);
            var writer = new StreamWriter(stream);
            writer.WriteLine(key.ToString("N"));
            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        [Obsolete("Config is no longer serialised into a single string - each key is stored individually.", true)]
        public static string Serialise(ControllerConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException("config", "Configuration cannot be null.");

            var values = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("AwsAccessKeyId", config.AwsAccessKeyId),
                new KeyValuePair<string,string>("AwsSecretAccessKey", config.AwsSecretAccessKey),
                new KeyValuePair<string,string>("BucketName", config.BucketName),
            };

            using (var reader = new StreamReader(Utils.Serialisation.Serialise(values)))
            {
                return reader.ReadToEnd();
            }
        }

        [Obsolete("Config is no longer serialised into a single string - each key is stored individually.", false)]
        public static string Serialise(DeploymentConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException("config", "Configuration cannot be null.");

            var values = new List<KeyValuePair<string, string>>()
            {
                new KeyValuePair<string,string>("CheckFrequency", config.CheckFrequency.ToString()),
                new KeyValuePair<string,string>("AwsAccessKeyId", config.AwsAccessKeyId),
                new KeyValuePair<string,string>("AwsSecretAccessKey", config.AwsSecretAccessKey),
                new KeyValuePair<string,string>("BucketName", config.BucketName),
                new KeyValuePair<string,string>("DeploymentDirectory", config.DeploymentDirectory),
                new KeyValuePair<string,string>("TargetKey", config.TargetKey.ToString("N")),
            };
            if (config.InstanceKey.HasValue)
                values.Add(new KeyValuePair<string,string>("InstanceKey", config.InstanceKey.Value.ToString("N")));

            using (var reader = new StreamReader(Utils.Serialisation.Serialise(values)))
            {
                return reader.ReadToEnd();
            }
        }

        #endregion

        #region Serialisation Helpers

        public static Stream Serialise(List<KeyValuePair<string, string>> values)
        {
            var stream = new MemoryStream();
            if (values != null && values.Count > 0)
            {
                var writer = new StreamWriter(stream);
                for (int i = 0; i < values.Count; i++)
                {
                    var pair = values[i];
                    writer.Write(pair.Key);
                    writer.Write("\r\n");
                    var valueReader = new StringReader(pair.Value);
                    var line = valueReader.ReadLine();
                    while (line != null)
                    {
                        writer.Write("\t");
                        writer.Write(line);
                        line = valueReader.ReadLine();
                        if (line != null)
                            writer.Write("\r\n");
                    }
                    if (i < values.Count - 1)
                    {
                        writer.Write("\r\n");
                    }
                }
                writer.Flush();
                stream.Seek(0, SeekOrigin.Begin);
            }
            return stream;
        }

        public static Dictionary<string, string> ReadProperties(TextReader source)
        {
            var properties = new Dictionary<string, string>();
            string property = null;
            bool isFirst = true;
            bool awaitingValue = false;
            StringBuilder value = null;
            string currentLine = source.ReadLine();
            while (currentLine != null)
            {
                if (currentLine.Length == 0)
                {
                    // Skip line.
                }
                else if (currentLine[0] != '\t')
                {
                    // Property start.
                    if (!isFirst)
                    {
                        AddValue(properties, property, value);
                        value = null;
                    }
                    isFirst = false;
                    property = currentLine;
                    awaitingValue = true;
                }
                else
                {
                    // In a value.
                    if (property == null)
                    {
                        throw new DeserialisationException("Failed deserialising properties, missing property.");
                    }
                    if (value == null)
                    {
                        value = new StringBuilder();
                    }
                    value.AppendLine(currentLine.Substring(1, currentLine.Length - 1));
                    awaitingValue = false;
                }
                currentLine = source.ReadLine();
            }
            if (awaitingValue || value != null)
            {
                AddValue(properties, property, value);
            }
            return properties;
        }

        private static void AddValue(Dictionary<string, string> properties, string property, StringBuilder value)
        {
            if (value == null)
            {
                properties.Add(property, null);
            }
            else
            {
                properties.Add(property, value.Remove(value.Length - 2, 2).ToString());
            }
        }

        #endregion

        #region Reversed Date Serialisation

        public static string SerialiseDateReversed(DateTime date)
        {
            return (ulong.MaxValue - (ulong)date.Ticks).ToString("X");
        }

        public static DateTime DeserialiseReversedDate(string hexEncodedReversedUlong)
        {
            return new DateTime((long)(ulong.MaxValue - ulong.Parse(hexEncodedReversedUlong, System.Globalization.NumberStyles.AllowHexSpecifier)), DateTimeKind.Utc);
        }

        #endregion
    }
}
