using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;

namespace Plywood
{
    public class Group
    {
        #region Constructors

        public Group()
        {
            Key = Guid.NewGuid();
        }

        public Group(string source)
            : base()
        {
            Extend(Group.Parse(source));
        }

        public Group(Stream source)
            : base()
        {
            Extend(Group.Parse(source));
        }

        public Group(TextReader source)
            : base()
        {
            Extend(Group.Parse(source));
        }

        #endregion

        public Guid Key { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }

        private void Extend(Group prototype)
        {
            this.Key = prototype.Key;
            this.Name = prototype.Name;
            this.Tags = prototype.Tags;
        }

        public Stream Serialise()
        {
            return Group.Serialise(this);
        }

        #region Static Serialisation Methods

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

            return Serialisation.Serialise(values);
        }

        public static Group Parse(string source)
        {
            return Parse(new StringReader(source));
        }

        public static Group Parse(Stream source)
        {
            return Parse(new StreamReader(source));
        }

        public static Group Parse(TextReader source)
        {
            var group = new Group();
            var properties = Serialisation.ReadProperties(source);

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

        #endregion

    }

    public class GroupList
    {
        public IEnumerable<GroupListItem> Groups { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class GroupListItem
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
