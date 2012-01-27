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
