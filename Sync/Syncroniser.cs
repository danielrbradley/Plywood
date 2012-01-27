using System;
using System.Collections.Generic;
using System.Linq;

namespace Plywood.Sync
{
    public class Synchroniser
    {
        private Configuration config;
        private IStorageProvider sourceProvider;
        private IStorageProvider targetProvider;

        public Synchroniser(Configuration config, IStorageProvider sourceProvider, IStorageProvider targetProvider)
        {
            try
            {
                if (config == null)
                    throw new ArgumentNullException("config", "config is null.");
                if (sourceProvider == null)
                    throw new ArgumentNullException("sourceProvider", "sourceProvider is null.");
                if (targetProvider == null)
                    throw new ArgumentNullException("targetProvider", "targetProvider is null.");
            }
            catch (Exception ex)
            {
                throw new TypeInitializationException("Plywood.Sync.Syncroniser", ex);
            }

            this.config = config;
            this.sourceProvider = sourceProvider;
            this.targetProvider = targetProvider;
        }

        public void Synchronise()
        {
            var pendingActions = GetPendingActions();

            try
            {
                var putActions =
                    from action in pendingActions
                    where action.Action != SyncAction.Delete
                    select action;

                var sourceVersions = new Versions(sourceProvider);

                foreach (var action in putActions)
                {
                    sourceVersions.TransferTo(action.VersionKey, targetProvider);
                }
            }
            finally
            {
            }
        }

        private IEnumerable<SyncOperation> GetPendingActions()
        {
            var operations = new List<SyncOperation>();

            var installedPackages = new Plywood.Packages(targetProvider).List(config.RoleKey);
            var requiredPackages = new Plywood.Packages(sourceProvider).List(config.RoleKey);

            operations.AddRange(GetDeleteOperations(installedPackages, requiredPackages));
            operations.AddRange(GetCreateOperations(installedPackages, requiredPackages));
            operations.AddRange(GetUpdateOperations(installedPackages, requiredPackages));

            return operations;
        }

        private IEnumerable<SyncOperation> GetDeleteOperations(PackageList installedPackages, PackageList requiredPackages)
        {
            return
                from ip in installedPackages.Items
                where !requiredPackages.Items.Any(rp => rp.Key == ip.Key)
                select new SyncOperation()
                {
                    Action = SyncAction.Delete,
                    PackageKey = ip.Key,
                    VersionKey = Guid.Empty,
                };
        }

        private IEnumerable<SyncOperation> GetCreateOperations(PackageList installedPackages, PackageList requiredPackages)
        {
            var createPackageVersions =
                from rp in requiredPackages.Items
                where !installedPackages.Items.Any(ip => ip.Key == rp.Key)
                select new
                {
                    PackageKey = rp.Key,
                    VersionKey = GetSourcePackageVersion(rp.Key),
                };

            return
                from packageVersion in createPackageVersions
                where packageVersion.VersionKey.HasValue
                select new SyncOperation()
                {
                    Action = SyncAction.Create,
                    PackageKey = packageVersion.PackageKey,
                    VersionKey = packageVersion.VersionKey.Value,
                };
        }

        private Guid? GetSourcePackageVersion(Guid packageKey)
        {
            var sourceRolePackageVersions = new Plywood.RolePackageVersions(sourceProvider);
            var sourceVersions = new Plywood.Versions(sourceProvider);

            var rolePackageVersion = sourceRolePackageVersions.Get(config.RoleKey, packageKey);
            if (rolePackageVersion.HasValue)
                return rolePackageVersion.Value;
            else
            {
                var versions = sourceVersions.Search(packageKey, pageSize: 1);
                if (versions.Items.Any())
                    return versions.Items.First().Key;
                else
                    return null;
            }
        }

        private IEnumerable<SyncOperation> GetUpdateOperations(PackageList installedPackages, PackageList requiredPackages)
        {
            var sourceRolePackageVersions = new Plywood.RolePackageVersions(sourceProvider);
            var targetRolePackageVersions = new Plywood.RolePackageVersions(targetProvider);

            var installedPackageVersions =
                from rp in requiredPackages.Items
                where installedPackages.Items.Any(ip => ip.Key == rp.Key)
                select new
                {
                    PackageKey = rp.Key,
                    VersionKey = targetRolePackageVersions.Get(config.RoleKey, rp.Key)
                };

            var updatedVersions =
                from ipv in installedPackageVersions
                where !ipv.VersionKey.HasValue || sourceRolePackageVersions.CheckStatus(config.RoleKey, ipv.PackageKey, ipv.VersionKey.Value) != VersionStatus.NotChanged
                select new
                {
                    PackageKey = ipv.PackageKey,
                    VersionKey = GetSourcePackageVersion(ipv.PackageKey),
                };

            return
                from upv in updatedVersions
                where upv.VersionKey.HasValue
                && installedPackageVersions.First(ipv => ipv.PackageKey == upv.PackageKey).VersionKey != upv.VersionKey.Value
                select new SyncOperation()
                {
                    Action = SyncAction.Update,
                    PackageKey = upv.PackageKey,
                    VersionKey = upv.VersionKey.Value,
                };
        }
    }
}
