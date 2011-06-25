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
