using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;

namespace Plywood
{
    public class Target
    {
        #region Constructors

        public Target()
        {
            Key = Guid.NewGuid();
        }

        public Target(string source)
            : base()
        {
            Extend(Target.Parse(source));
        }

        public Target(Stream source)
            : base()
        {
            Extend(Target.Parse(source));
        }

        public Target(TextReader source)
            : base()
        {
            Extend(Target.Parse(source));
        }

        #endregion

        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        private void Extend(Target prototype)
        {
            this.Key = prototype.Key;
            this.Name = prototype.Name;
            this.Tags = prototype.Tags;
            this.GroupKey = prototype.GroupKey;
        }

        public Stream Serialise()
        {
            return Target.Serialise(this);
        }

        #region Static Serialisation Methods

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

            return Serialisation.Serialise(values);
        }

        public static Target Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Target Parse(Stream source)
        {
            return Parse(new StreamReader(source));
        }

        public static Target Parse(TextReader source)
        {
            var target = new Target();
            var properties = Serialisation.ReadProperties(source);

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

        #endregion

    }

    public class TargetList
    {
        public Guid GroupKey { get; set; }
        public IEnumerable<TargetListItem> Targets { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class TargetListItem
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }

    public class TargetAppList
    {
        public Guid TargetKey { get; set; }
        public IEnumerable<AppListItem> Apps { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class TargetAppVersion
    {
        public string ETag { get; set; }
        public Guid Key { get; set; }
    }

    public enum VersionCheckResult
    {
        NotChanged,
        Changed,
        NotSet
    }
}
