using System;

namespace Plywood.Sync
{
    internal class SyncOperation
    {
        public SyncAction Action { get; set; }
        public Guid PackageKey { get; set; }
        public Guid VersionKey { get; set; }
    }
}
