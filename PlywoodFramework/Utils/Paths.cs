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
            return string.Format("g/{0}/d", key.ToString("N"));
        }

        public static string GetPackageDetailsKey(Guid key)
        {
            return string.Format("p/{0}/d", key.ToString("N"));
        }

        public static string GetAppIndexBaseKey(Guid groupKey)
        {
            return string.Format("g/{0}/ai", groupKey.ToString("N"));
        }

        public static string GetVersionDetailsKey(Guid key)
        {
            return string.Format("v/{0}/d", key.ToString("N"));
        }

        public static string GetVersionIndexBaseKey(Guid appKey)
        {
            return string.Format("a/{0}/vi", appKey.ToString("N"));
        }

        public static string GetRoleDetailsKey(Guid key)
        {
            return string.Format("r/{0}/d", key.ToString("N"));
        }

        public static string GetTargetIndexBaseKey(Guid groupKey)
        {
            return string.Format("g/{0}/ti", groupKey.ToString("N"));
        }

        public static string GetServerDetailsKey(Guid key)
        {
            return string.Format("s/{0}/d", key.ToString("N"));
        }

        public static string GetInstanceIndexBaseKey(Guid targetKey)
        {
            return string.Format("t/{0}/ii", targetKey.ToString("N"));
        }

        public static string GetTargetAppVersionKey(Guid targetKey, Guid appKey)
        {
            return string.Format("t/{0}/av/{1}", targetKey.ToString("N"), appKey.ToString("N"));
        }

        public static string GetLogDetailsPath(Guid logKey)
        {
            return string.Format("l/{0}/d", logKey.ToString("N"));
        }
    }
}
