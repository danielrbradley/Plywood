using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Plywood.Sawmill.Models
{
    public class VersionIndex
    {
        public VersionList VersionList { get; set; }
        public App App { get; set; }
        public Group Group { get; set; }
    }

    public class VersionDetails
    {
        public Version Version { get; set; }
        public App App { get; set; }
        public Group Group { get; set; }
    }
}