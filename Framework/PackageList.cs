using System;
using System.Collections.Generic;

namespace Plywood
{
    public class PackageList
    {
        public Guid? GroupKey { get; set; }
        public IEnumerable<PackageListItem> Items { get; set; }
        public string Query { get; set; }
        public string Marker { get; set; }
        public int PageSize { get; set; }
        public string NextMarker { get; set; }
        public bool IsTruncated { get; set; }
    }
}
