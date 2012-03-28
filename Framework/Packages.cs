using System;
using System.Collections.Generic;
using System.Linq;
using Plywood.Indexes;
using Plywood.Utils;

namespace Plywood
{
    public class Packages : ControllerBase
    {
        public Packages(IStorageProvider provider) : base(provider) { }

        public void Create(Package package)
        {
            if (package == null)
                throw new ArgumentException("Package cannot be null.", "package");
            if (package.GroupKey == Guid.Empty)
                throw new ArgumentException("Group key cannot be empty.", "package.GroupKey");

            using (var stream = package.Serialise())
            {
                try
                {
                    var indexEntries = new IndexEntries(StorageProvider);
                    // TODO: Check key, name and deployment directory are unique.

                    StorageProvider.PutFile(Paths.GetPackageDetailsKey(package.Key), stream);

                    indexEntries.PutEntity(package);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed creating package.", ex);
                }
            }
        }

        public void Delete(Guid key)
        {
            var app = Get(key);
            try
            {
                var indexEntries = new IndexEntries(StorageProvider);

                indexEntries.DeleteEntity(app);

                // TODO: Refactor the solf-delete functionality.
                StorageProvider.MoveFile(Paths.GetPackageDetailsKey(key), string.Concat("deleted/", Paths.GetPackageDetailsKey(key)));
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed deleting package.", ex);
            }
        }

        public bool Exists(Guid key)
        {
            try
            {
                return StorageProvider.FileExists(Paths.GetPackageDetailsKey(key));
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting package with key \"{0}\"", key), ex);
            }
        }

        public Package Get(Guid key)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetPackageDetailsKey(key)))
                {
                    return new Package(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting package with key \"{0}\"", key), ex);
            }
        }

        public PackageList List(Guid? groupKey = null, string query = null)
        {
            var currentList = new PackageList() { IsTruncated = true };
            var items = new List<PackageListItem>();
            string lastMarker = null;

            while (currentList.IsTruncated)
            {
                currentList = this.Search(groupKey, query, lastMarker, GlobalConstants.MaxPageSize);
                items.AddRange(currentList.Items);
                lastMarker = currentList.NextMarker;
            }

            return new PackageList()
            {
                GroupKey = groupKey,
                Query = query,
                IsTruncated = false,
                Marker = null,
                NextMarker = lastMarker,
                Items = items,
                PageSize = items.Count,
            };
        }

        public string PushRevision(Guid packageKey)
        {
            var package = Get(packageKey);
            var thisRevision = String.Format("{0}.{1}", package.MajorVersion, package.Revision);
            package.Revision += 1;
            Update(package);
            return thisRevision;
        }

        public PackageList Search(Guid? groupKey = null, string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 0.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                string[] startLocations;
                if (groupKey.HasValue)
                    startLocations = new string[2];
                else
                    startLocations = new string[1];
                startLocations[0] = "pi";
                if (groupKey.HasValue)
                    startLocations[1] = string.Format("g/{0}/pi", Utils.Indexes.EncodeGuid(groupKey.Value));

                IEnumerable<string> basePaths;

                if (!string.IsNullOrWhiteSpace(query))
                    basePaths = new SimpleTokeniser().Tokenise(query).SelectMany(token =>
                        startLocations.Select(l => string.Format("{0}/t/{1}", l, Indexes.IndexEntries.GetTokenHash(token))));
                else
                    basePaths = startLocations.Select(l => string.Format("{0}/e", l));

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var apps = rawResults.FileNames.Select(fileName => new PackageListItem(fileName));
                var list = new PackageList()
                {
                    Items = apps,
                    GroupKey = groupKey,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    NextMarker = apps.Any() ? apps.Last().Marker : marker,
                    IsTruncated = rawResults.IsTruncated,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException("Failed searcing packages.", ex);
            }
        }

        public void Update(Package package)
        {
            if (package == null)
                throw new ArgumentNullException("package", "Package cannot be null.");

            var existingPackage = Get(package.Key);
            // Don't allow moving between groups right now as would have to recursively update references from versions and targets within app.
            package.GroupKey = existingPackage.GroupKey;

            using (var stream = package.Serialise())
            {
                try
                {
                    // This will not currently get called.
                    if (existingPackage.GroupKey != package.GroupKey)
                    {
                        var groupsController = new Groups(StorageProvider);
                        if (!groupsController.Exists(package.GroupKey))
                            throw new GroupNotFoundException(string.Format("Group with key \"{0}\" to move package into cannot be found.", package.GroupKey));
                    }

                    // Update role package indexes
                    var rolePackages = new RolePackages(StorageProvider);
                    rolePackages.Update(existingPackage, package);

                    StorageProvider.PutFile(Paths.GetPackageDetailsKey(package.Key), stream);

                    var indexEntries = new IndexEntries(StorageProvider);
                    indexEntries.UpdateEntity(existingPackage, package);
                }
                catch (Exception ex)
                {
                    throw new DeploymentException("Failed updating package.", ex);
                }
            }
        }
    }
}
