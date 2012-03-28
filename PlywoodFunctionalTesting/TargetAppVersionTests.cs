using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood;
using System.Diagnostics;

namespace Plywood.Tests.Functional
{
    class TargetAppVersionTests
    {
        public static void Run(ControllerConfiguration context, Guid targetKey, Guid appKey, Guid versionKey)
        {
            var targetAppVersionsController = new TargetAppVersions(context);

            var notSet1 = targetAppVersionsController.GetTargetAppVersion(targetKey, appKey);
            Debug.Assert(notSet1.HasValue == false);

            var notSet2 = targetAppVersionsController.TargetAppVersionChanged(targetKey, appKey, Guid.Empty);
            Debug.Assert(notSet2 == VersionCheckResult.NotSet);

            // Set
            targetAppVersionsController.SetTargetAppVersion(targetKey, appKey, versionKey);

            var remoteVersion = targetAppVersionsController.GetTargetAppVersion(targetKey, appKey);
            Debug.Assert(remoteVersion.HasValue);
            Debug.Assert(remoteVersion.Value == versionKey);

            // Check same
            var same = targetAppVersionsController.TargetAppVersionChanged(targetKey, appKey, versionKey);
            Debug.Assert(same == VersionCheckResult.NotChanged);

            // Check different
            var different = targetAppVersionsController.TargetAppVersionChanged(targetKey, appKey, Guid.NewGuid());
            Debug.Assert(different == VersionCheckResult.Changed);

            // Set null
            targetAppVersionsController.SetTargetAppVersion(targetKey, appKey, null);
            var notSet3 = targetAppVersionsController.TargetAppVersionChanged(targetKey, appKey, versionKey);
            Debug.Assert(notSet3 == VersionCheckResult.NotSet);

            // Reset
            targetAppVersionsController.SetTargetAppVersion(targetKey, appKey, versionKey);
        }
    }
}
