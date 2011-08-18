using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Utils
{
    public static class Paths
    {
        public static string GetGroupDetailsKey(Guid key)
        {
            return string.Format("g/{1}/d", key.ToString("N"));
        }

        public static string GetAppDetailsKey(Guid key)
        {
            return string.Format("a/{1}/d", key.ToString("N"));
        }

        public static string GetAppIndexBaseKey(Guid groupKey)
        {
            return string.Format("g/{1}/ai", groupKey.ToString("N"));
        }

        public static string GetVersionDetailsKey(Guid key)
        {
            return string.Format("v/{1}/d", key.ToString("N"));
        }

        public static string GetVersionIndexBaseKey(Guid appKey)
        {
            return string.Format("a/{1}/vi", appKey.ToString("N"));
        }

        public static string GetTargetDetailsKey(Guid key)
        {
            return string.Format("t/{1}/d", key.ToString("N"));
        }

        public static string GetTargetIndexBaseKey(Guid groupKey)
        {
            return string.Format("g/{1}/ti", groupKey.ToString("N"));
        }

        public static string GetInstanceDetailsKey(Guid key)
        {
            return string.Format("i/{1}/d", key.ToString("N"));
        }

        public static string GetInstanceIndexBaseKey(Guid targetKey)
        {
            return string.Format("t/{1}/ii", targetKey.ToString("N"));
        }

    }
}
