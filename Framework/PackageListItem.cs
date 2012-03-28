using System;

namespace Plywood
{
    public class PackageListItem
    {
        public PackageListItem()
        {
        }

        public PackageListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 4)
                throw new ArgumentException("A package path index entry must contain exactly 4 segments.", "path");

            Marker = Utils.Indexes.GetIndexFileName(path);
            Key = Utils.Indexes.DecodeGuid(segments[1]);
            Name = Utils.Indexes.DecodeText(segments[2]);
            MajorVersion = Utils.Indexes.DecodeText(segments[3]);
        }

        internal string Marker { get; set; }
        public Guid Key { get; set; }
        public string Name { get; set; }
        public string MajorVersion { get; set; }
    }
}
