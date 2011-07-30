using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Indexes
{
    public class IndexQueryResult
    {
        public IList<IndexEntry> Results { get; set; }
        public bool IsTruncated { get; set; }
        public string NextMarker { get; set; }
    }
}
