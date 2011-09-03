using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Indexes
{
    public class VersionTokeniser : ITokeniser
    {
        public IEnumerable<string> Tokenise(string input)
        {
            return Enumerable.Range(1, input.Length).Select(n => input.Take(n).Select(c => c.ToString()).Aggregate((a, b) => a + b)).Where(t => !t.EndsWith("."));
        }
    }
}
