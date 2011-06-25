using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public class Target
    {
        public Target()
        {
            Key = Guid.NewGuid();
        }

        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    public class TargetList
    {
        public Guid GroupKey { get; set; }
        public IEnumerable<TargetListItem> Targets { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class TargetListItem
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }

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
