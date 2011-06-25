using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Plywood.Utils
{
    public static class Hooks
    {
        public static List<Hook> ParseHooks(string source)
        {
            return ParseHooks(new StringReader(source));
        }

        public static List<Hook> ParseHooks(TextReader reader)
        {
            var hooks = new List<Hook>();
            var currentLine = reader.ReadLine();
            while (currentLine != null)
            {
                if (currentLine[0] == '"')
                {
                    var cmdEndIndex = currentLine.IndexOf('"', 1);
                    if (cmdEndIndex < 0)
                        throw new HooksParserException("Unterminated quotes.");
                    if (currentLine.Length > cmdEndIndex + 1 && currentLine[cmdEndIndex + 1] != ' ')
                        throw new HooksParserException("Command and argument not separated by a space.");

                    if (currentLine.Length <= cmdEndIndex + 2)
                        hooks.Add(new Hook() { Command = currentLine.Substring(1, cmdEndIndex - 1) });
                    else
                        hooks.Add(new Hook() { Command = currentLine.Substring(1, cmdEndIndex - 1), Arguments = currentLine.Substring(cmdEndIndex + 2, currentLine.Length - (cmdEndIndex + 2)) });
                }
                else
                {
                    var spaceIndex = currentLine.IndexOf(' ');
                    if (spaceIndex < 0 || spaceIndex >= currentLine.Length - 1)
                        hooks.Add(new Hook() { Command = currentLine });
                    else
                        hooks.Add(new Hook() { Command = currentLine.Substring(0, spaceIndex), Arguments = currentLine.Substring(spaceIndex + 1, currentLine.Length - (spaceIndex + 1)) });
                }
                currentLine = reader.ReadLine();
            }
            return hooks;
        }
    }

    public class Hook
    {
        public string Command { get; set; }
        public string Arguments { get; set; }
    }

}
