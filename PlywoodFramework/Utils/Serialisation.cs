using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml;

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

        public static Stream Serialise(XDocument doc)
        {
            var stream = new MemoryStream();
            var writer = new XmlTextWriter(stream, Encoding.UTF8);

            doc.WriteTo(writer);

            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
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
