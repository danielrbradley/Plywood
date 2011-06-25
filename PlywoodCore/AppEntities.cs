using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public class App
    {
        public App()
        {
            Key = Guid.NewGuid();
            MajorVersion = "0.1";
        }

        public Guid Key { get; set; }
        public Guid GroupKey { get; set; }
        public string Name { get; set; }
        public string DeploymentDirectory { get; set; }
        public string MajorVersion { get; set; }
        public int Revision { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    public class AppList
    {
        public Guid GroupKey { get; set; }
        public IEnumerable<AppListItem> Apps { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class AppListItem
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
