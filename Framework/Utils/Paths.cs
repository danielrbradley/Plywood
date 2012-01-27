﻿using System;
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

        public static string GetVersionDetailsKey(Guid key)
        {
            return string.Format("v/{0}/d", key.ToString("N"));
        }

        public static string GetRoleDetailsKey(Guid key)
        {
            return string.Format("r/{0}/d", key.ToString("N"));
        }

        public static string GetServerDetailsKey(Guid key)
        {
            return string.Format("s/{0}/d", key.ToString("N"));
        }

        public static string GetRolePackageVersionKey(Guid roleKey, Guid packageKey)
        {
            return string.Format("r/{0}/pv/{1}", roleKey.ToString("N"), packageKey.ToString("N"));
        }

        public static string GetLogDetailsPath(Guid logKey)
        {
            return string.Format("l/{0}/d", logKey.ToString("N"));
        }
    }
}