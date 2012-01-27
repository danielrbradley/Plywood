using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Indexes;

namespace Plywood
{
    public class RolePackages : ControllerBase
    {
        public RolePackages(IStorageProvider provider) : base(provider) { }

        public void Add(Guid roleKey, Guid packageKey)
        {
            var roles = new Roles(StorageProvider);
            var packages = new Packages(StorageProvider);

            var role = roles.Get(roleKey);
            var package = packages.Get(packageKey);

            Add(new RolePackage(role, package));
        }

        internal void Add(Role role, Guid packageKey)
        {
            var packages = new Packages(StorageProvider);
            var package = packages.Get(packageKey);

            Add(new RolePackage(role, package));
        }

        internal void Add(Guid roleKey, Package package)
        {
            var roles = new Roles(StorageProvider);
            var role = roles.Get(roleKey);

            Add(new RolePackage(role, package));
        }

        internal void Add(Role role, Package package)
        {
            Add(new RolePackage(role, package));
        }

        private void Add(RolePackage targetApp)
        {
            try
            {
                var indexes = new Indexes.IndexEntries(StorageProvider);
                indexes.PutEntity(targetApp);
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed adding app with key \"{0}\" to target \"{1}\".", targetApp.PackageKey, targetApp.RoleKey), ex);
            }
        }

        public RolePackageList SearchPackages(Guid roleKey, string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 0.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                var startLocation = string.Format("r/{0}/pi", roleKey.ToString("N"));

                var basePaths = new List<string>();
                if (string.IsNullOrWhiteSpace(query))
                {
                    basePaths.Add(string.Format("{0}/e", startLocation));
                }
                else
                {
                    basePaths.AddRange(
                        new SimpleTokeniser().Tokenise(query).Select(
                            token =>
                                string.Format("{0}/t/{1}", startLocation, Indexes.IndexEntries.GetTokenHash(token))));
                }

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var packages = rawResults.FileNames.Select(fileName => new RolePackageListItem(fileName));

                var list = new RolePackageList()
                {
                    Items = packages,
                    RoleKey = roleKey,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    IsTruncated = rawResults.IsTruncated,
                    NextMarker = packages.Any() ? packages.Last().Marker : marker,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting packages for server role \"{0}\".", roleKey), ex);
            }
        }

        public PackageRoleList SearchRoles(Guid packageKey, string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 0.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                var startLocation = string.Format("p/{0}/ri", packageKey.ToString("N"));

                var basePaths = new List<string>();
                if (string.IsNullOrWhiteSpace(query))
                {
                    basePaths.Add(string.Format("{0}/e", startLocation));
                }
                else
                {
                    basePaths.AddRange(
                        new SimpleTokeniser().Tokenise(query).Select(
                            token =>
                                string.Format("{0}/t/{1}", startLocation, Indexes.IndexEntries.GetTokenHash(token))));
                }

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var roles = rawResults.FileNames.Select(fileName => new PackageRoleListItem(fileName));

                var list = new PackageRoleList()
                {
                    Items = roles,
                    PackageKey = packageKey,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    IsTruncated = rawResults.IsTruncated,
                    NextMarker = roles.Any() ? roles.Last().Marker : marker,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting packages for server role \"{0}\".", packageKey), ex);
            }
        }

        public void Remove(Guid roleKey, Guid packageKey)
        {
            var roles = new Roles(StorageProvider);
            var packages = new Packages(StorageProvider);

            var role = roles.Get(roleKey);
            var package = packages.Get(packageKey);

            Remove(new RolePackage(role, package));
        }

        internal void Remove(Guid roleKey, Package package)
        {
            var roles = new Roles(StorageProvider);
            var role = roles.Get(roleKey);

            Remove(new RolePackage(role, package));
        }

        internal void Remove(Role role, Guid packageKey)
        {
            var packages = new Packages(StorageProvider);
            var package = packages.Get(packageKey);

            Remove(new RolePackage(role, package));
        }

        internal void Remove(Role role, Package package)
        {
            Remove(new RolePackage(role, package));
        }

        private void Remove(RolePackage targetApp)
        {
            try
            {
                var indexes = new IndexEntries(StorageProvider);
                indexes.DeleteEntity(targetApp);
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed removing app with key \"{0}\" from target \"{1}\".", targetApp.PackageKey, targetApp.RoleKey), ex);
            }
        }

        internal void Update(Package oldPackage, Package newPackage)
        {
            // TODO: Deal with when there's more than a single page of roles.
            var assignedRoles = this.SearchRoles(oldPackage.Key);

            // TODO: We could make this significantly faster by only updating the entries that have changed!
            assignedRoles.Items.Select(r => r.Key).AsParallel().ForAll(roleKey =>
            {
                this.Remove(roleKey, oldPackage);
                this.Add(roleKey, newPackage);
            });
        }

        internal void Update(Role oldRole, Role newRole)
        {
            // TODO: Deal with when there's more than a single page of packages.
            var assignedPackages = this.SearchPackages(oldRole.Key);

            // TODO: We could make this significantly faster by only updating the entries that have changed!
            assignedPackages.Items.Select(p => p.Key).AsParallel().ForAll(packageKey =>
            {
                this.Remove(oldRole, packageKey);
                this.Add(newRole, packageKey);
            });
        }
    }
}
