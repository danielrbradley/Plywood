using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood;
using System.Diagnostics;

namespace Plywood.Tests.Functional
{
    static class VersionTests
    {
        public static Plywood.Version Run(ControllerConfiguration context, Guid appKey)
        {
            var versionsController = new Versions(context);

            var searchEmpty = versionsController.SearchAppVersions(appKey);
            Debug.Assert(searchEmpty.TotalCount == 0);
            Debug.Assert(searchEmpty.Versions.Count() == 0);

            var testVersion = new Plywood.Version() { VersionNumber = "0.0.1", Comment = "Test Version", AppKey = appKey };
            versionsController.CreateVersion(testVersion);
            var createdVersion = versionsController.GetVersion(testVersion.Key);
            Debug.Assert(createdVersion.VersionNumber == testVersion.VersionNumber);
            Debug.Assert(createdVersion.Comment == testVersion.Comment);
            Debug.Assert(DateTimeEquals(createdVersion.Timestamp, testVersion.Timestamp));
            Debug.Assert(createdVersion.GroupKey == testVersion.GroupKey);

            var searchSingle = versionsController.SearchAppVersions(appKey);
            Debug.Assert(searchSingle.TotalCount == 1);
            Debug.Assert(searchSingle.Versions.Count() == 1);
            Debug.Assert(searchSingle.Versions.First().Key == createdVersion.Key);
            Debug.Assert(searchSingle.Versions.First().VersionNumber == createdVersion.VersionNumber);
            Debug.Assert(searchSingle.Versions.First().Comment == createdVersion.Comment);
            Debug.Assert(DateTimeEquals(searchSingle.Versions.First().Timestamp, createdVersion.Timestamp));

            createdVersion.Tags.Add("Foo", "Bar");
            versionsController.UpdateVersion(createdVersion);

            var taggedVersion = versionsController.GetVersion(testVersion.Key);
            Debug.Assert(taggedVersion.GroupKey == createdVersion.GroupKey);
            Debug.Assert(taggedVersion.Tags.ContainsKey("Foo"));
            Debug.Assert(taggedVersion.Tags["Foo"] == "Bar");
            var searchUpdated = versionsController.SearchAppVersions(appKey);
            Debug.Assert(searchUpdated.TotalCount == 1);
            Debug.Assert(searchUpdated.Versions.Count() == 1);
            Debug.Assert(searchUpdated.Versions.First().Key == createdVersion.Key);
            Debug.Assert(searchUpdated.Versions.First().VersionNumber == createdVersion.VersionNumber);
            Debug.Assert(searchUpdated.Versions.First().Comment == createdVersion.Comment);
            Debug.Assert(DateTimeEquals(searchUpdated.Versions.First().Timestamp, createdVersion.Timestamp));

            taggedVersion.VersionNumber = "0.0.2";
            taggedVersion.Comment = "Updated Test Version";
            versionsController.UpdateVersion(taggedVersion);

            var renamedVersion = versionsController.GetVersion(testVersion.Key);
            Debug.Assert(renamedVersion.VersionNumber == taggedVersion.VersionNumber);
            Debug.Assert(renamedVersion.Comment == taggedVersion.Comment);
            Debug.Assert(DateTimeEquals(renamedVersion.Timestamp, taggedVersion.Timestamp));
            Debug.Assert(renamedVersion.GroupKey == taggedVersion.GroupKey);
            var searchRenamed = versionsController.SearchAppVersions(appKey);
            Debug.Assert(searchRenamed.TotalCount == 1);
            Debug.Assert(searchRenamed.Versions.Count() == 1);
            Debug.Assert(searchRenamed.Versions.First().Key == taggedVersion.Key);
            Debug.Assert(searchRenamed.Versions.First().VersionNumber == taggedVersion.VersionNumber);
            Debug.Assert(searchRenamed.Versions.First().Comment == taggedVersion.Comment);
            Debug.Assert(DateTimeEquals(searchRenamed.Versions.First().Timestamp, taggedVersion.Timestamp));

            Searching(appKey, versionsController, createdVersion);

            versionsController.UpdateVersion(testVersion);
            var searchReset = versionsController.SearchAppVersions(appKey);
            Debug.Assert(searchReset.TotalCount == 1);
            Debug.Assert(searchReset.Versions.Count() == 1);
            Debug.Assert(searchReset.Versions.First().Key == testVersion.Key);
            Debug.Assert(searchReset.Versions.First().VersionNumber == testVersion.VersionNumber);
            Debug.Assert(searchReset.Versions.First().Comment == testVersion.Comment);
            Debug.Assert(DateTimeEquals(searchReset.Versions.First().Timestamp, testVersion.Timestamp));

            return testVersion;
        }

        private static void Searching(Guid appKey, Versions versionsController, Plywood.Version createdVersion)
        {
            var aSecondApp = new Plywood.Version() { VersionNumber = "0.1.1", Comment = "A Second Version", AppKey = appKey };
            versionsController.CreateVersion(aSecondApp);
            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
            var zThirdApp = new Plywood.Version() { VersionNumber = "0.2.1", Comment = "Z Third Version", AppKey = appKey };
            versionsController.CreateVersion(zThirdApp);

            var search1 = versionsController.SearchAppVersions(appKey);
            Debug.Assert(search1.TotalCount == 3);
            Debug.Assert(search1.Versions.Count() == 3);
            // This should be time sorted!
            Debug.Assert(search1.Versions.ElementAt(0).Key == zThirdApp.Key);
            Debug.Assert(search1.Versions.ElementAt(1).Key == aSecondApp.Key);
            Debug.Assert(search1.Versions.ElementAt(2).Key == createdVersion.Key);

            var search2 = versionsController.SearchAppVersions(appKey, query: "Updated");
            Debug.Assert(search2.TotalCount == 1);
            Debug.Assert(search2.Versions.Count() == 1);
            Debug.Assert(search2.Versions.ElementAt(0).Key == createdVersion.Key);

            var search3 = versionsController.SearchAppVersions(appKey, pageSize: 1);
            Debug.Assert(search3.TotalCount == 3);
            Debug.Assert(search3.Versions.Count() == 1);
            Debug.Assert(search3.Versions.ElementAt(0).Key == zThirdApp.Key);

            var search4 = versionsController.SearchAppVersions(appKey, pageSize: 2);
            Debug.Assert(search4.TotalCount == 3);
            Debug.Assert(search4.Versions.Count() == 2);
            Debug.Assert(search4.Versions.ElementAt(0).Key == zThirdApp.Key);
            Debug.Assert(search4.Versions.ElementAt(1).Key == aSecondApp.Key);

            var search5 = versionsController.SearchAppVersions(appKey, offset: 1);
            Debug.Assert(search5.TotalCount == 3);
            Debug.Assert(search5.Versions.Count() == 2);
            Debug.Assert(search5.Versions.ElementAt(0).Key == aSecondApp.Key);
            Debug.Assert(search5.Versions.ElementAt(1).Key == createdVersion.Key);

            var search6 = versionsController.SearchAppVersions(appKey, offset: 1, pageSize: 1);
            Debug.Assert(search6.TotalCount == 3);
            Debug.Assert(search6.Versions.Count() == 1);
            Debug.Assert(search6.Versions.ElementAt(0).Key == aSecondApp.Key);

            versionsController.DeleteVersion(aSecondApp.Key);
            versionsController.DeleteVersion(zThirdApp.Key);

            var searchDeleted = versionsController.SearchAppVersions(appKey);
            Debug.Assert(searchDeleted.TotalCount == 1);
            Debug.Assert(searchDeleted.Versions.Count() == 1);
            Debug.Assert(searchDeleted.Versions.First().Key == createdVersion.Key);
        }

        private static bool DateTimeEquals(DateTime a, DateTime b)
        {
            return Math.Abs((a - b).TotalSeconds) < 1;
        }
    }
}
