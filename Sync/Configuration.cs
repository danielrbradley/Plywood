using System;

namespace Plywood.Sync
{
    public class Configuration
    {
        public TimeSpan CheckFrequency { get; set; }
        public Guid RoleKey { get; set; }
    }
}
