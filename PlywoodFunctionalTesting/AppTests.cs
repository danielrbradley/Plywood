using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood;
using System.Diagnostics;

namespace Plywood.Tests.Functional
{
    static class AppTests
    {
        public static App Run(ControllerConfiguration context, Guid groupKey)
        {
            var appsController = new Apps(context);

            var searchEmpty = appsController.SearchGroupApps(groupKey);
            Debug.Assert(searchEmpty.TotalCount == 0);
            Debug.Assert(searchEmpty.Apps.Count() == 0);

            var testApp = new App() { Name = "Test App", GroupKey = groupKey, DeploymentDirectory = "TestApp" };
            appsController.CreateApp(testApp);
            var createdApp = appsController.GetApp(testApp.Key);
            Debug.Assert(createdApp.Name == testApp.Name);
            Debug.Assert(createdApp.GroupKey == testApp.GroupKey);

            var searchSingle = appsController.SearchGroupApps(groupKey);
            Debug.Assert(searchSingle.TotalCount == 1);
            Debug.Assert(searchSingle.Apps.Count() == 1);
            Debug.Assert(searchSingle.Apps.First().Key == createdApp.Key);
            Debug.Assert(searchSingle.Apps.First().Name == createdApp.Name);

            createdApp.Tags.Add("Foo", "Bar");
            appsController.UpdateApp(createdApp);

            var taggedApp = appsController.GetApp(testApp.Key);
            Debug.Assert(taggedApp.GroupKey == createdApp.GroupKey);
            Debug.Assert(taggedApp.Tags.ContainsKey("Foo"));
            Debug.Assert(taggedApp.Tags["Foo"] == "Bar");
            var searchUpdated = appsController.SearchGroupApps(groupKey);
            Debug.Assert(searchUpdated.TotalCount == 1);
            Debug.Assert(searchUpdated.Apps.Count() == 1);
            Debug.Assert(searchUpdated.Apps.First().Key == createdApp.Key);
            Debug.Assert(searchUpdated.Apps.First().Name == createdApp.Name);

            taggedApp.Name = "Updated Test App";
            appsController.UpdateApp(taggedApp);

            var renamedApp = appsController.GetApp(testApp.Key);
            Debug.Assert(renamedApp.Name == taggedApp.Name);
            Debug.Assert(renamedApp.GroupKey == taggedApp.GroupKey);
            var searchRenamed = appsController.SearchGroupApps(groupKey);
            Debug.Assert(searchRenamed.TotalCount == 1);
            Debug.Assert(searchRenamed.Apps.Count() == 1);
            Debug.Assert(searchRenamed.Apps.First().Key == taggedApp.Key);
            Debug.Assert(searchRenamed.Apps.First().Name == taggedApp.Name);

            Searching(groupKey, appsController, createdApp);

            var searchDeleted = appsController.SearchGroupApps(groupKey);
            Debug.Assert(searchDeleted.TotalCount == 1);
            Debug.Assert(searchDeleted.Apps.Count() == 1);
            Debug.Assert(searchDeleted.Apps.First().Key == taggedApp.Key);
            Debug.Assert(searchDeleted.Apps.First().Name == taggedApp.Name);

            appsController.UpdateApp(testApp);
            var searchReset = appsController.SearchGroupApps(groupKey);
            Debug.Assert(searchReset.TotalCount == 1);
            Debug.Assert(searchReset.Apps.Count() == 1);
            Debug.Assert(searchReset.Apps.First().Key == testApp.Key);
            Debug.Assert(searchReset.Apps.First().Name == testApp.Name);

            // Create default test apps.
            appsController.CreateApp(new App() { Key = new Guid("8b245840-96be-4c9b-889e-06985fc63498"), GroupKey = new Guid("5615c002-2d9a-46c4-a9a3-26b2b19cd790"), Name = "Plywood Toolkit", DeploymentDirectory = "PlywoodToolkit", MajorVersion = "0.1", Revision = 11 });
            appsController.CreateApp(new App() { Key = new Guid("6eaf852b-5b91-4ce6-9e74-ce57ee2aef9d"), GroupKey = new Guid("5615c002-2d9a-46c4-a9a3-26b2b19cd790"), Name = "Sawmill Test", DeploymentDirectory = "SawmillTest", MajorVersion = "0.1", Revision = 1 });

            return testApp;
        }

        private static void Searching(Guid groupKey, Apps appsController, App createdApp)
        {
            var aSecondApp = new App() { Name = "A Second Group", GroupKey = groupKey, DeploymentDirectory = "ASecondGroup" };
            appsController.CreateApp(aSecondApp);
            var zThirdApp = new App() { Name = "Z Third Group", GroupKey = groupKey, DeploymentDirectory = "ZThirdGroup" };
            appsController.CreateApp(zThirdApp);

            var search1 = appsController.SearchGroupApps(groupKey);
            Debug.Assert(search1.TotalCount == 3);
            Debug.Assert(search1.Apps.Count() == 3);
            Debug.Assert(search1.Apps.ElementAt(0).Key == aSecondApp.Key);
            Debug.Assert(search1.Apps.ElementAt(1).Key == createdApp.Key);
            Debug.Assert(search1.Apps.ElementAt(2).Key == zThirdApp.Key);

            var search2 = appsController.SearchGroupApps(groupKey, "Updated");
            Debug.Assert(search2.TotalCount == 1);
            Debug.Assert(search2.Apps.Count() == 1);
            Debug.Assert(search2.Apps.ElementAt(0).Key == createdApp.Key);

            var search3 = appsController.SearchGroupApps(groupKey, pageSize: 1);
            Debug.Assert(search3.TotalCount == 3);
            Debug.Assert(search3.Apps.Count() == 1);
            Debug.Assert(search3.Apps.ElementAt(0).Key == aSecondApp.Key);

            var search4 = appsController.SearchGroupApps(groupKey, pageSize: 2);
            Debug.Assert(search4.TotalCount == 3);
            Debug.Assert(search4.Apps.Count() == 2);
            Debug.Assert(search4.Apps.ElementAt(0).Key == aSecondApp.Key);
            Debug.Assert(search4.Apps.ElementAt(1).Key == createdApp.Key);

            var search5 = appsController.SearchGroupApps(groupKey, offset: 1);
            Debug.Assert(search5.TotalCount == 3);
            Debug.Assert(search5.Apps.Count() == 2);
            Debug.Assert(search5.Apps.ElementAt(0).Key == createdApp.Key);
            Debug.Assert(search5.Apps.ElementAt(1).Key == zThirdApp.Key);

            var search6 = appsController.SearchGroupApps(groupKey, offset: 1, pageSize: 1);
            Debug.Assert(search6.TotalCount == 3);
            Debug.Assert(search6.Apps.Count() == 1);
            Debug.Assert(search6.Apps.ElementAt(0).Key == createdApp.Key);

            appsController.DeleteApp(aSecondApp.Key);
            appsController.DeleteApp(zThirdApp.Key);
        }
    }
}
