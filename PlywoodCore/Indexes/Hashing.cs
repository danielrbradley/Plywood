using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Utils;

namespace Plywood.Indexes
{
    public static class Hashing
    {
        public const byte MAX_HASH_LENGTH = 45;
        public const string LAST_HASH_STRING = "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz";

        public static string CreateHash(DateTime date, bool reversed = true)
        {
            ulong hashNum;
            if (reversed)
                hashNum = ulong.MaxValue - (ulong)date.Ticks;
            else
                hashNum = (ulong)date.Ticks;
            return hashNum.ToString("X");
        }

        public static string CreateVersionHash(string version, bool reversed = true)
        {
            if (version == null)
                throw new ArgumentNullException("version", "The version must have a value.");
            if (!Validation.IsMajorVersionValid(version))
                throw new FormatException("Version string is not valid.");

            if (reversed)
            {
                return string.Join("_", version.Split(new char[1] { '.' }).Select(numStr => (uint.MaxValue - uint.Parse(numStr)).ToString("X").PadLeft(8, '0')));
            }
            else
            {
                return string.Join("_", version.Split(new char[1] { '.' }).Select(numStr => uint.Parse(numStr).ToString("X").PadLeft(8, '0')));
            }
        }

        public static string CreateHash(string content, bool reversed = false, bool ignoreCase = true, bool sortEmptyLast = true)
        {
            if (content == null)
                return null;
            var emptyHash = (sortEmptyLast) ? LAST_HASH_STRING : string.Empty;

            if (content == string.Empty)
                return emptyHash;

            var sanitised = content.Where(c => char.IsLetterOrDigit(c));
            if (sanitised.Count() == 0)
                return emptyHash;

            sanitised = sanitised.Select(c =>
            {
                if (ignoreCase && char.IsUpper(c))
                    c = char.ToLower(c);
                if (reversed)
                {
                    if ((int)c < 58)
                    {
                        // Number - remap to top of lower case range.
                        c = (char)(170 - (int)c);
                    }
                    else if ((char)c < 81)
                    {
                        // First 16 capital, can remap to lowercase range.
                        c = (char)(177 - (int)c);
                    }
                    else if ((char)c < 91)
                    {
                        // Last 10 capital, can remap to capital range.
                        c = (char)(171 - (int)c);
                    }
                    else if ((char)c < 113)
                    {
                        // First 16 lower, can remap to lowercase range.
                        c = (char)(177 - (int)c);
                    }
                    else
                    {
                        // Last 10 lower, remap to number range.
                        c = (char)(170 - (int)c);
                    }
                }
                return c;
            }).ToList();
            if (!sanitised.Any())
                return emptyHash;
            if (sanitised.Count() == 1)
                return sanitised.First().ToString();
            return sanitised.Take(MAX_HASH_LENGTH).Select(c => c.ToString()).Aggregate((a, b) => a + b);
        }

        public static DateTime UnHashDate(string hexEncodedReversedUlongDate, bool reversed = true)
        {
            var hashNum = ulong.Parse(hexEncodedReversedUlongDate, System.Globalization.NumberStyles.AllowHexSpecifier);
            long ticks;
            if (reversed)
                ticks = (long)(ulong.MaxValue - hashNum);
            else
                ticks = (long)hashNum;
            return new DateTime(ticks, DateTimeKind.Utc);
        }
    }
}
