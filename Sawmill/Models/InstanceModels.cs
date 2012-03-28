using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Plywood.Sawmill.Models
{
    public class InstanceIndex
    {
        public InstanceList InstanceList { get; set; }
        public Target Target { get; set; }
        public Group Group { get; set; }
    }

    public class InstanceDetails
    {
        public Instance Instance { get; set; }
        public Target Target { get; set; }
        public Group Group { get; set; }
    }
}