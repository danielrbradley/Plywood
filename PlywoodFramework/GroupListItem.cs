namespace Plywood
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using Plywood.Indexes;
    using Plywood.Utils;

    public class GroupListItem
    {
        public GroupListItem()
        {
        }

        public GroupListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 3)
                throw new ArgumentException("A group path index entry does not contain exactly 3 segments.", "path");

            Marker = Utils.Indexes.GetIndexFileName(path);
            Key = Utils.Indexes.DecodeGuid(segments[1]);
            Name = Utils.Indexes.DecodeText(segments[2]);
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
