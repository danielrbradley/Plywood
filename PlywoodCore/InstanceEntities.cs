using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;

namespace Plywood
{
    public class Instance
    {
        #region Constructors

        public Instance()
        {
            Key = Guid.NewGuid();
            Name = "New Instance " + DateTime.UtcNow.ToString("r");
        }

        public Instance(string source)
            : base()
        {
            Extend(Instance.Parse(source));
        }

        public Instance(Stream source)
            : base()
        {
            Extend(Instance.Parse(source));
        }

        public Instance(TextReader source)
            : base()
        {
            Extend(Instance.Parse(source));
        }

        #endregion

        public Guid Key { get; set; }
        public Guid TargetKey { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        private void Extend(Instance prototype)
        {
            this.Key = prototype.Key;
            this.TargetKey = prototype.TargetKey;
            this.Name = prototype.Name;
            this.Tags = prototype.Tags;
        }

        public Stream Serialise()
        {
            return Instance.Serialise(this);
        }

        #region Static Serialisation Methods

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

            return Serialisation.Serialise(values);
        }

        public static Instance Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Instance Parse(Stream source)
        {
            return Parse(new StreamReader(source));
        }

        public static Instance Parse(TextReader source)
        {
            var instance = new Instance();
            var properties = Serialisation.ReadProperties(source);

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

        #endregion

    }

    public class InstanceList
    {
        public Guid TargetKey { get; set; }
        public IEnumerable<InstanceListItem> Instances { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class InstanceListItem
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
