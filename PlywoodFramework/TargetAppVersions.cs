using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Plywood.Utils;

namespace Plywood
{
    public class TargetAppVersions : ControllerBase
    {
        public const string STR_TARGET_APP_VERSION_INFO_EXTENSION = "target-version";

        public TargetAppVersions(IStorageProvider provider) : base(provider) { }

        public VersionCheckResult TargetAppVersionChanged(Guid targetKey, Guid appKey, Guid localVersionKey)
        {
            try
            {
                string localETag;
                using (var localKeyStream = Utils.Serialisation.Serialise(localVersionKey))
                {
                    localETag = Utils.Validation.GenerateETag(localKeyStream);
                }
                var fileHash = StorageProvider.GetFileHash(Utils.Paths.GetTargetAppVersionKey(targetKey, appKey));
                if (fileHash == null)
                {
                    return VersionCheckResult.NotSet;
                }
                else if (fileHash == localETag)
                {
                    return VersionCheckResult.NotChanged;
                }
                else
                {
                    return VersionCheckResult.Changed;
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed checking for version updates for app with key \"{0}\" and target with the key \"{1}\".", appKey, targetKey), ex);
            }
        }

        public Guid? GetTargetAppVersion(Guid targetKey, Guid appKey)
        {
            try
            {
                using (var stream = StorageProvider.GetFile(Paths.GetTargetAppVersionKey(targetKey, appKey)))
                {
                    return Utils.Serialisation.ParseKey(stream);
                }
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting version for app with key \"{0}\" and target with the key \"{1}\".", appKey, targetKey), ex);
            }
        }

        public void SetTargetAppVersion(Guid targetKey, Guid appKey, Guid? versionKey)
        {
            try
            {
                    string path = Paths.GetTargetAppVersionKey(targetKey, appKey);
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
                throw new DeploymentException(string.Format("Failed setting version for app with key \"{0}\" and target with the key \"{1}\".", appKey, targetKey), ex);
            }
        }
    }
}
