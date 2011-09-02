using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml;
using System.Reflection;
using Plywood.Indexes;

namespace Plywood
{
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
