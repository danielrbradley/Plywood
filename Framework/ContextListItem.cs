namespace Plywood
{
    using System;

    /// <summary>
    /// Represents a single context result returned in a context list.
    /// </summary>
    public class ContextListItem
    {
        /// <summary>
        /// Initializes a new instance of the ContextListItem class.
        /// </summary>
        public ContextListItem()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ContextListItem class from a path.
        /// </summary>
        /// <param name="path">Path of the index entry of the result item.</param>
        public ContextListItem(string path)
        {
            var segments = Utils.Indexes.GetIndexFileNameSegments(path);
            if (segments.Length != 2)
            {
                throw new ArgumentException("A context path index entry does not contain exactly 2 segments.", "path");
            }

            this.Marker = Utils.Indexes.GetIndexFileName(path);
            this.Name = Utils.Indexes.DecodeText(segments[1]);
        }

        /// <summary>
        /// Gets the key identifier of the context.
        /// </summary>
        public Guid Key
        {
            get
            {
                return this.Hierarchy.Key;
            }
        }

        /// <summary>
        /// Gets or sets the name of the context.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets the heirachy model for this context name.
        /// </summary>
        public ContextHierarchy Hierarchy { get { return new ContextHierarchy(this.Name); } }

        /// <summary>
        /// Gets or sets the marker of this item.
        /// </summary>
        internal string Marker { get; set; }
    }
}
