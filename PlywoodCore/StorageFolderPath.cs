using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Plywood
{
    public class StorageFolderPath
    {
        public StorageFolderPath()
        {
        }

        public StorageFolderPath(string value)
        {
            this.Value = value;
        }

        public string Value { get; set; }

        public bool IsValid
        {
            get
            {
                return Regex.IsMatch(Value, @"^/([0-9a-zA-Z_\-]+/)*$");
            }
        }
    }
}
