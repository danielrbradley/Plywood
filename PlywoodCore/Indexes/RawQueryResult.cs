using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Indexes
{
    public class RawQueryResult
    {
        public IList<string> FileNames { get; set; }
        public bool IsTruncated { get; set; }
    }
}
