using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood;
using System.Diagnostics;

namespace Plywood.Tests.Functional
{
    static class TargetAppTests
    {
        public static void Run(ControllerConfiguration context, Guid targetKey, Guid appKey)
        {
            var targetAppsController = new TargetApps(context);

            var noApps = targetAppsController.GetTargetAppKeys(targetKey);
            Debug.Assert(noApps.Count() == 0);

            targetAppsController.AddApp(targetKey, appKey);
            var oneApp = targetAppsController.GetTargetAppKeys(targetKey);
            Debug.Assert(oneApp.Count() == 1);
            Debug.Assert(oneApp.Single() == appKey);

            targetAppsController.RemoveApp(targetKey, appKey);
            var removedApp = targetAppsController.GetTargetAppKeys(targetKey);
            Debug.Assert(removedApp.Count() == 0);

            targetAppsController.AddApp(targetKey, appKey);

            // Add Plywood and Sawmill to default test server.
            targetAppsController.AddApp(new Guid("2c50a0af-bc66-41f5-bf52-498118217d12"), new Guid("8b245840-96be-4c9b-889e-06985fc63498"));
            targetAppsController.AddApp(new Guid("2c50a0af-bc66-41f5-bf52-498118217d12"), new Guid("6eaf852b-5b91-4ce6-9e74-ce57ee2aef9d"));
        }
    }
}
