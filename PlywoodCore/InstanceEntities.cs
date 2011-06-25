using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public class Instance
    {
        public Instance()
        {
            Key = Guid.NewGuid();
            Name = "New Instance " + DateTime.UtcNow.ToString("r");
        }

        public Guid Key { get; set; }
        public Guid TargetKey { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    public class InstanceList
    {
        public Guid TargetKey { get; set; }
        public IEnumerable<InstanceListItem> Instances { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class InstanceListItem
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
