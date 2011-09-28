using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    /// <summary>
    /// Defines methods and properties of any path entity.
    /// </summary>
    public interface IPath
    {
        /// <summary>
        /// Gets or sets the value of the path.
        /// </summary>
        string Value { get; set; }

        /// <summary>
        /// Indicates if the current value of the file path is valid.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Throws an exception if the current path is not valid.
        /// </summary>
        /// <exception cref="FormatException">Thrown if the path is not in a valid format.</exception>
        void EnsureValidity();
    }
}
