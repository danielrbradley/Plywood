using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public class FileListing
    {
        public string FolderPath { get; set; }
        public string Marker { get; set; }
        public int PageSize { get; set; }
        public IList<string> Items { get; set; }
        public string NextMarker { get; set; }
        public bool IsTruncated { get; set; }
    }
}
