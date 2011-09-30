using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Plywood.Utils
{
    public static class Validation
    {
        public static bool IsNameValid(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.All(c => c != '\r' && c != '\n' && c != '\f' && c != '\t');
        }

        public static bool IsDirectoryNameValid(string name)
        {
            var reserved = new char[9] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
            return !string.IsNullOrEmpty(name) && name.All(c => !char.IsControl(c) && !reserved.Contains(c));
        }

        public static bool IsMajorVersionValid(string majorVersion)
        {
            if (string.IsNullOrEmpty(majorVersion))
                return false;
            else
                return Regex.IsMatch(majorVersion, @"^(?:\d+\.)*\d+$");
        }

        public static string GenerateETag(Stream stream)
        {
            var crypto = new MD5CryptoServiceProvider();
            return GenerateETag(stream, crypto);
        }

        public static string GenerateETag(Stream stream, MD5 crypto)
        {
            return String.Format("\"{0}\"", BitConverter.ToString(crypto.ComputeHash(stream)).Replace("-", string.Empty).ToLower());
        }
    }
}
