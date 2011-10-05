using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public class PackageRoleListItem
    {
        public PackageRoleListItem()
        {
        }

        public PackageRoleListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 3)
                throw new ArgumentException("A package role index entry path must contain exactly 3 segments.", "path");

            this.Marker = Utils.Indexes.GetIndexFileName(path);
            this.Key = Utils.Indexes.DecodeGuid(segments[1]);
            this.Name = Utils.Indexes.DecodeText(segments[2]);
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
