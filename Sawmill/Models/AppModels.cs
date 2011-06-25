using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Plywood.Sawmill.Models
{
    public class AppIndex
    {
        public AppList AppList { get; set; }
        public Group Group { get; set; }
    }

    public class AppDetails
    {
        public App App { get; set; }
        public Group Group { get; set; }
    }
}