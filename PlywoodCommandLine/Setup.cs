using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;

namespace Plywood.Ply
{
    public static class Setup
    {
        public static void AddToPath(string[] args)
        {
            string localPath = null;
            if (args.Length == 2)
            {
                localPath = args[1];
            }
            else if (args.Length != 1)
            {
                Console.WriteLine("Try 'ply help add-to-path' for more information.");
                return;
            }
            if (localPath == null)
                localPath = Directory.GetCurrentDirectory();

            var currentPath = Registry.CurrentUser.OpenSubKey("Environment").GetValue("Path") as string;
            if (currentPath == null)
                currentPath = string.Empty;

            if (Regex.IsMatch(currentPath, Regex.Escape(localPath) + "($|;)", RegexOptions.IgnoreCase))
            {
                Console.WriteLine("Current directory is already part of the path.");
                return;
            }
            if (currentPath != string.Empty)
                currentPath += ";";
            currentPath += localPath;
            Registry.CurrentUser.OpenSubKey("Environment", true).SetValue("Path", currentPath, RegistryValueKind.String);
        }

        internal static void AddToPathHelp(string[] args)
        {
            Console.WriteLine(
@"Usage: 'ply add-to-path ([PATH])'
Adds a directory to the path variable of the current user's environment.
[DIRECTORY]           Optional directory to add to the path.

If the directory is not specified, the current working directory is used.");
        }
    }
}
