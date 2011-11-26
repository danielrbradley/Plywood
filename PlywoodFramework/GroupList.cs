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

    public class GroupList
    {
        public IEnumerable<GroupListItem> Groups { get; set; }
        public string Query { get; set; }
        public string Marker { get; set; }
        public string NextMarker { get; set; }
        public int PageSize { get; set; }
        public bool IsTruncated { get; set; }
    }
}
