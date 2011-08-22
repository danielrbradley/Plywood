using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Utils
{
    public static class Indexes
    {
        public static string[] GetPathSegments(string entryPath)
        {
            if (entryPath == null)
                return new string[0];

            int lastSlash = entryPath.LastIndexOf('/');
            string fileName;
            if (lastSlash == -1)
                fileName = entryPath;
            else
                fileName = entryPath.Substring(lastSlash + 1);

            return fileName.Split('-');
        }

        public static string EncodeText(string text)
        {
            return System.Web.HttpUtility.UrlPathEncode(text).Replace(".", "%2E");
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
