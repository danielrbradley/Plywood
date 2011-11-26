namespace Plywood
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Represents a listing of files returned from a storage provider.
    /// </summary>
    public class FileListing
    {
        /// <summary>
        /// Gets or sets the relative folder path the files were listed from.
        /// </summary>
        public string FolderPath { get; set; }

        /// <summary>
        /// Gets or sets the marker after which all files were taken.
        /// </summary>
        public string Marker { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items to fetch.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the ordered list of file paths retrieved.
        /// </summary>
        public IList<string> Items { get; set; }

        /// <summary>
        /// Gets or sets the marker for listing the next page of files.
        /// </summary>
        public string NextMarker { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether there are more files available beyond this file listing page.
        /// </summary>
        public bool IsTruncated { get; set; }
    }
}
