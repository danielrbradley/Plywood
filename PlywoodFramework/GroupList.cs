namespace Plywood
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents a listing result for groups.
    /// </summary>
    public class GroupList
    {
        /// <summary>
        /// Gets or sets the colleciton of group items returned.
        /// </summary>
        public IEnumerable<GroupListItem> Groups { get; set; }

        /// <summary>
        /// Gets or sets the query used to retrieve the listing.
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Gets or sets the marker used for the paging start position.
        /// </summary>
        public string Marker { get; set; }

        /// <summary>
        /// Gets or sets the marker to get the next page of results.
        /// </summary>
        public string NextMarker { get; set; }

        /// <summary>
        /// Gets or sets the page size specified for the listing.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the listing is truncated.
        /// </summary>
        public bool IsTruncated { get; set; }
    }
}
