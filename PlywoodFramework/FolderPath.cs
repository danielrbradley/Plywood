namespace Plywood
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Reppresents a path specifically to a folder.
    /// </summary>
    public class FolderPath : IPath
    {
        /// <summary>
        /// Initializes a new instance of the FolderPath class with a null value.
        /// </summary>
        public FolderPath()
        {
        }

        /// <summary>
        /// Initializes a new instance of the FolderPath class with an initial value.
        /// </summary>
        /// <param name="value">Initial value of the folder path.</param>
        public FolderPath(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the value of the folder path.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets a value indicating whether the current value of the folder path is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return this.Value != null && Regex.IsMatch(this.Value, @"^([0-9a-zA-Z_\-%]+/)*[0-9a-zA-Z_\-%]+$");
            }
        }

        /// <summary>
        /// Ensures the FolderPath is valid, otherwise throws an exception.
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
