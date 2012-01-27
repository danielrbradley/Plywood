using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Utils
{
    public static class Indexes
    {
        /// <summary>
        /// Get the segments of the filename for an index placeholder file where segments are separated by hyphens '-'.
        /// </summary>
        /// <param name="entryPath">Path to extract segments from</param>
        /// <returns>Array of each string segments</returns>
        public static string[] GetIndexFileNameSegments(string entryPath)
        {
            if (entryPath == null)
                return new string[0];

            return GetIndexFileName(entryPath).Split('-');
        }

        public static string GetIndexFileName(string entryPath)
        {
            if (entryPath == null)
            {
                return string.Empty;
            }

            int lastSlash = entryPath.LastIndexOf('/');
            string fileName;
            if (lastSlash == -1)
                fileName = entryPath;
            else
                fileName = entryPath.Substring(lastSlash + 1);

            return fileName;
        }

        public static string EncodeText(string text)
        {
            return System.Web.HttpUtility.UrlPathEncode(text).Replace(".", "%2E").Replace("-", "%2D").Replace(":", "%3A");
        }

        public static string DecodeText(string encodedText)
        {
            return System.Web.HttpUtility.UrlDecode(encodedText);
        }

        public static string EncodeGuid(Guid guid)
        {
            return guid.ToString("N");
        }

        public static Guid DecodeGuid(string guid)
        {
            return Guid.ParseExact(guid, "N");
        }
    }
}
