using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Plywood
{
    public class FolderPath : IPath
    {
        public FolderPath()
        {
        }

        public FolderPath(string value)
        {
            this.Value = value;
        }

        public string Value { get; set; }

        public bool IsValid
        {
            get
            {
                return this.Value != null && Regex.IsMatch(this.Value, @"^([0-9a-zA-Z_\-]+/)*[0-9a-zA-Z_\-]+$");
            }
        }

        /// <summary>
        /// Throws an exception if the current path is not valid.
        /// </summary>
        /// <exception cref="FormatException">Thrown if the path is not in a valid format.</exception>
        public void EnsureValidity()
        {
            if (!this.IsValid)
            {
                throw new FormatException("The specified path is not valid.");
            }
        }
    }
}
