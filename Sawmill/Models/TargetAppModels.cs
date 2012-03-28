using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Plywood.Sawmill.Models
{
    public class TargetAppsIndex
    {
        public TargetAppList TargetAppList { get; set; }
        public Target Target { get; set; }
        public Group Group { get; set; }
    }

    public class TargetAppVersionDetails
    {
        public Version Version { get; set; }
        public App App { get; set; }
        public Target Target { get; set; }
        public Group Group { get; set; }
    }
}