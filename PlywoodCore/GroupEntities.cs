using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public class Group
    {
        public Group()
        {
            Key = Guid.NewGuid();
        }

        public Guid Key { get; set; }
        public string Name { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }

    public class GroupList
    {
        public IEnumerable<GroupListItem> Groups { get; set; }
        public string Query { get; set; }
        public int Offset { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class GroupListItem
    {
        public Guid Key { get; set; }
        public string Name { get; set; }
    }
}
