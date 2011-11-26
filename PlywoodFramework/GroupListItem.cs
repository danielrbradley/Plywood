namespace Plywood
{
    using System;

    /// <summary>
    /// Represents a single group result returned in a group list.
    /// </summary>
    public class GroupListItem
    {
        /// <summary>
        /// Initializes a new instance of the GroupListItem class.
        /// </summary>
        public GroupListItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the GroupListItem class from a path.
        /// </summary>
        /// <param name="path">Path of the index entry of the result item.</param>
        public GroupListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 3)
            {
                throw new ArgumentException("A group path index entry does not contain exactly 3 segments.", "path");
            }

            this.Marker = Utils.Indexes.GetIndexFileName(path);
            this.Key = Utils.Indexes.DecodeGuid(segments[1]);
            this.Name = Utils.Indexes.DecodeText(segments[2]);
        }

        /// <summary>
        /// Gets or sets the key identifier of the group.
        /// </summary>
        public Guid Key { get; set; }

        /// <summary>
        /// Gets or sets the name of the group.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the marker of this item.
        /// </summary>
        internal string Marker { get; set; }
    }
}
