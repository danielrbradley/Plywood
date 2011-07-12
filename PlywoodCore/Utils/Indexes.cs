using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Utils
{
    public static class Indexes
    {
        public const byte MAX_HASH_LENGTH = 45;
        public const string LAST_HASH_STRING = "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz";

        public static string GetIndexEntryFilename(IndexEntry indexEntry)
        {
            return string.Format("{0}-{1}-{2}", indexEntry.SortHash, indexEntry.EntryKey.ToString("N"), indexEntry.EntryText);
        }

        public static string CreateHash(DateTime date, bool reversed = true)
        {
            ulong hashNum;
            if (reversed)
                hashNum = ulong.MaxValue - (ulong)date.Ticks;
            else
                hashNum = (ulong)date.Ticks;
            return hashNum.ToString("X");
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

        // TODO: Unit test index entry validation funciton.
        public static bool IsIndexEntryValid(IndexEntry indexEntry)
        {
            if (indexEntry == null || (indexEntry.SortHash == null | indexEntry.EntryText == null | indexEntry.EntryKey == Guid.Empty))
                return false;

            if (indexEntry.SortHash.Length > 41)
                return false;
            if (indexEntry.EntryText.Length > 60)
                return false;

            return true;
        }
    }

    public class IndexEntry
    {
        public string SortHash { get; set; }
        public Guid EntryKey { get; set; }
        public string EntryText { get; set; }

        public override string ToString()
        {
            return Indexes.GetIndexEntryFilename(this);
        }

        public bool IsValid()
        {
            return Indexes.IsIndexEntryValid(this);
        }
    }
}
