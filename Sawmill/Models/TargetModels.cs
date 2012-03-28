using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Plywood.Sawmill.Models
{
    public class TargetIndex
    {
        public TargetList TargetList { get; set; }
        public Group Group { get; set; }
    }

    public class TargetDetails
    {
        public Target Target { get; set; }
        public Group Group { get; set; }
    }
}