namespace Plywood
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Reppresents a path specifically to a file.
    /// </summary>
    public class FilePath : IPath
    {
        /// <summary>
        /// Initializes a new instance of the FilePath class with a null value.
        /// </summary>
        public FilePath()
        {
        }

        /// <summary>
        /// Initializes a new instance of the FilePath class with an initial value.
        /// </summary>
        /// <param name="value">Initial value of the file path.</param>
        public FilePath(string value)
        {
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the value of the file path.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets the folder path section of the file path.
        /// </summary>
        public FolderPath FolderPath
        {
            get
            {
                return new FolderPath(this.Value.Substring(0, this.Value.LastIndexOf('/')));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current value of the file path is valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return this.Value != null && Regex.IsMatch(this.Value, @"^([0-9a-zA-Z_\-%]+/)*[0-9a-zA-Z_\-\.%]+$");
            }
        }

        /// <summary>
        /// Ensures the FilePath is valid, otherwise throws an exception.
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
