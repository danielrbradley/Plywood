using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Indexes;

namespace Plywood
{
    public class RolePackageListItem
    {
        public RolePackageListItem()
        {
        }

        public RolePackageListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 4)
                throw new ArgumentException("A package path index entry must contain exactly 4 segments.", "path");

            this.Marker = Utils.Indexes.GetIndexFileName(path);
            this.Key = Utils.Indexes.DecodeGuid(segments[1]);
            this.Name = Utils.Indexes.DecodeText(segments[2]);
            this.DeploymentDirectory = Utils.Indexes.DecodeText(segments[3]);
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; }
        public string DeploymentDirectory { get; set; }
    }
}
