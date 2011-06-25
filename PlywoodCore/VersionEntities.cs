using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;

namespace Plywood
{
    public class Version
    {
        #region Constructors

        public Version()
        {
            Key = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }

        public Version(string source)
            : base()
        {
            Extend(Version.Parse(source));
        }

        public Version(Stream source)
            : base()
        {
            Extend(Version.Parse(source));
        }

        public Version(TextReader source)
            : base()
        {
            Extend(Version.Parse(source));
        }

        #endregion

        public Guid Key { get; set; }
        public Guid AppKey { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        private void Extend(Version prototype)
        {
            this.Key = prototype.Key;
            this.AppKey = prototype.AppKey;
            this.GroupKey = prototype.GroupKey;
            this.Name = prototype.Name;
            this.Timestamp = prototype.Timestamp;
            this.Tags = prototype.Tags;
        }

        public Stream Serialise()
        {
            return Version.Serialise(this);
        }

        #region Static Serialisation Methods

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

            return Serialisation.Serialise(values);
        }

        public static Version Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Version Parse(Stream source)
        {
            return Parse(new StreamReader(source));
        }

        public static Version Parse(TextReader source)
        {
            var version = new Version();
            var properties = Serialisation.ReadProperties(source);

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

        #endregion
    }

    public class VersionList
    {
        public Guid AppKey { get; set; }
        public IEnumerable<VersionListItem> Versions { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class VersionListItem
    {
        public Guid Key { get; set; }
        public DateTime Timestamp { get; set; }
        public string Name { get; set; }
    }
}
