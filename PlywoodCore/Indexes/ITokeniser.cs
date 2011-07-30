using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Indexes
{
    public interface ITokeniser
    {
        /// <summary>
        /// Split any input string into a valid set of token strings.
        /// </summary>
        /// <param name="input">The string to tokenise.</param>
        /// <returns>A collection of unique tokens.</returns>
        IEnumerable<string> Tokenise(string input);
    }
}
