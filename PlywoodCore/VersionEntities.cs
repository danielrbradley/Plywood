using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public class Version
    {
        public Version()
        {
            Key = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
        }

        public Guid Key { get; set; }
        public Guid AppKey { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, string> Tags { get; set; }
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
