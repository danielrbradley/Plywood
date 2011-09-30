using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Indexes
{
    public class SimpleTokeniser : ITokeniser
    {
        public IEnumerable<string> Tokenise(string input)
        {
            var tokens = new HashSet<string>();
            var lowered = input.ToLower();
            char[] trimChars = new char[] { '.' };
            int s = 0;
            for (int i = 0; i < lowered.Length; i++)
            {
                if (!(char.IsLetterOrDigit(input[i]) || lowered[i] == '_' || lowered[i] == '-' || lowered[i] == '#' || lowered[i] == '.'))
                {
                    if (s < i)
                    {
                        var token = lowered.Substring(s, i - s).TrimEnd(trimChars);
                        if (!tokens.Contains(token))
                            tokens.Add(token);
                    }
                    s = i + 1;
                }
            }
            if (s < lowered.Length)
            {
                var token = lowered.Substring(s, input.Length - s).TrimEnd(trimChars);
                if (!tokens.Contains(token))
                    tokens.Add(token);
            }
            return tokens.AsEnumerable();
        }
    }
}
