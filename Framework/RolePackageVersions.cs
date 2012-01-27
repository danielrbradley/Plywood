using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;

namespace Plywood
{
    public class RolePackageVersions : ControllerBase
    {
        public RolePackageVersions(IStorageProvider provider) : base(provider) { }

        public VersionStatus CheckStatus(Guid roleKey, Guid packageKey, Guid currentVersionKey)
        {
            try
            {
                string localETag;
                using (var localKeyStream = Utils.Serialisation.Serialise(currentVersionKey))
                {
                    localETag = Utils.Validation.GenerateHash(localKeyStream);
                }
                var fileHash = StorageProvider.GetFileHash(Utils.Paths.GetRolePackageVersionKey(roleKey, packageKey));
                if (fileHash == null)
                {
                    return VersionStatus.NotSet;
                }
                else if (fileHash == localETag)
                {
                    return VersionStatus.NotChanged;
                }
                else
                {
                    return VersionStatus.Changed;
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed checking for version updates for package with key \"{0}\" for server role with the key \"{1}\".", packageKey, roleKey), ex);
            }
        }

        public Guid? Get(Guid roleKey, Guid packageKey)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetRolePackageVersionKey(roleKey, packageKey)))
                {
                    return Utils.Serialisation.ParseKey(stream);
                }
            }
            catch (FileNotFoundException)
            {
                // Version has not been set.
                return null;
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting version for package with key \"{0}\" for server role with the key \"{1}\".", packageKey, roleKey), ex);
            }
        }

        public void Set(Guid roleKey, Guid packageKey, Guid? versionKey)
        {
            try
            {
                    string path = Paths.GetRolePackageVersionKey(roleKey, packageKey);
                    if (versionKey.HasValue)
                    {
                        // Set
                        using (var keyStream = Utils.Serialisation.Serialise(versionKey.Value))
                        {
                            StorageProvider.PutFile(path, keyStream);
                        }
                    }
                    else
                    {
                        // Delete
                        StorageProvider.DeleteFile(path);
                    }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed setting version for package with key \"{0}\" for server role with the key \"{1}\".", packageKey, roleKey), ex);
            }
        }
    }
}
