using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood;
using System.Diagnostics;

namespace Plywood.Tests.Functional
{
    static class TargetTests
    {
        public static Target Run(ControllerConfiguration context, Guid groupKey)
        {
            var targetsController = new Targets(context);

            var searchEmpty = targetsController.SearchGroupTargets(groupKey);
            Debug.Assert(searchEmpty.TotalCount == 0);
            Debug.Assert(searchEmpty.Targets.Count() == 0);

            var testTarget = new Target() { Name = "Test Target", GroupKey = groupKey };
            targetsController.CreateTarget(testTarget);
            var createdTarget = targetsController.GetTarget(testTarget.Key);
            Debug.Assert(createdTarget.Name == testTarget.Name);
            Debug.Assert(createdTarget.GroupKey == testTarget.GroupKey);

            var searchSingle = targetsController.SearchGroupTargets(groupKey);
            Debug.Assert(searchSingle.TotalCount == 1);
            Debug.Assert(searchSingle.Targets.Count() == 1);
            Debug.Assert(searchSingle.Targets.First().Key == createdTarget.Key);
            Debug.Assert(searchSingle.Targets.First().Name == createdTarget.Name);

            createdTarget.Tags.Add("Foo", "Bar");
            targetsController.UpdateTarget(createdTarget);

            var taggedTarget = targetsController.GetTarget(testTarget.Key);
            Debug.Assert(taggedTarget.GroupKey == createdTarget.GroupKey);
            Debug.Assert(taggedTarget.Tags.ContainsKey("Foo"));
            Debug.Assert(taggedTarget.Tags["Foo"] == "Bar");
            var searchUpdated = targetsController.SearchGroupTargets(groupKey);
            Debug.Assert(searchUpdated.TotalCount == 1);
            Debug.Assert(searchUpdated.Targets.Count() == 1);
            Debug.Assert(searchUpdated.Targets.First().Key == createdTarget.Key);
            Debug.Assert(searchUpdated.Targets.First().Name == createdTarget.Name);

            taggedTarget.Name = "Updated Test Target";
            targetsController.UpdateTarget(taggedTarget);

            var renamedTarget = targetsController.GetTarget(testTarget.Key);
            Debug.Assert(renamedTarget.Name == taggedTarget.Name);
            Debug.Assert(renamedTarget.GroupKey == taggedTarget.GroupKey);
            var searchRenamed = targetsController.SearchGroupTargets(groupKey);
            Debug.Assert(searchRenamed.TotalCount == 1);
            Debug.Assert(searchRenamed.Targets.Count() == 1);
            Debug.Assert(searchRenamed.Targets.First().Key == taggedTarget.Key);
            Debug.Assert(searchRenamed.Targets.First().Name == taggedTarget.Name);

            Searching(groupKey, targetsController, createdTarget);

            targetsController.UpdateTarget(testTarget);
            var searchReset = targetsController.SearchGroupTargets(groupKey);
            Debug.Assert(searchReset.TotalCount == 1);
            Debug.Assert(searchReset.Targets.Count() == 1);
            Debug.Assert(searchReset.Targets.First().Key == testTarget.Key);
            Debug.Assert(searchReset.Targets.First().Name == testTarget.Name);

            // Create default test target.
            targetsController.CreateTarget(new Target() { Key = new Guid("2c50a0af-bc66-41f5-bf52-498118217d12"), GroupKey = new Guid("5615c002-2d9a-46c4-a9a3-26b2b19cd790"), Name = "Test App Server" });

            return testTarget;
        }

        private static void Searching(Guid groupKey, Targets targetsController, Target createdTarget)
        {
            var aSecondTarget = new Target() { Name = "A Second Target", GroupKey = groupKey };
            targetsController.CreateTarget(aSecondTarget);
            var zThirdTarget = new Target() { Name = "Z Third Target", GroupKey = groupKey };
            targetsController.CreateTarget(zThirdTarget);

            var search1 = targetsController.SearchGroupTargets(groupKey);
            Debug.Assert(search1.TotalCount == 3);
            Debug.Assert(search1.Targets.Count() == 3);
            Debug.Assert(search1.Targets.ElementAt(0).Key == aSecondTarget.Key);
            Debug.Assert(search1.Targets.ElementAt(1).Key == createdTarget.Key);
            Debug.Assert(search1.Targets.ElementAt(2).Key == zThirdTarget.Key);

            var search2 = targetsController.SearchGroupTargets(groupKey, "Updated");
            Debug.Assert(search2.TotalCount == 1);
            Debug.Assert(search2.Targets.Count() == 1);
            Debug.Assert(search2.Targets.ElementAt(0).Key == createdTarget.Key);

            var search3 = targetsController.SearchGroupTargets(groupKey, pageSize: 1);
            Debug.Assert(search3.TotalCount == 3);
            Debug.Assert(search3.Targets.Count() == 1);
            Debug.Assert(search3.Targets.ElementAt(0).Key == aSecondTarget.Key);

            var search4 = targetsController.SearchGroupTargets(groupKey, pageSize: 2);
            Debug.Assert(search4.TotalCount == 3);
            Debug.Assert(search4.Targets.Count() == 2);
            Debug.Assert(search4.Targets.ElementAt(0).Key == aSecondTarget.Key);
            Debug.Assert(search4.Targets.ElementAt(1).Key == createdTarget.Key);

            var search5 = targetsController.SearchGroupTargets(groupKey, offset: 1);
            Debug.Assert(search5.TotalCount == 3);
            Debug.Assert(search5.Targets.Count() == 2);
            Debug.Assert(search5.Targets.ElementAt(0).Key == createdTarget.Key);
            Debug.Assert(search5.Targets.ElementAt(1).Key == zThirdTarget.Key);

            var search6 = targetsController.SearchGroupTargets(groupKey, offset: 1, pageSize: 1);
            Debug.Assert(search6.TotalCount == 3);
            Debug.Assert(search6.Targets.Count() == 1);
            Debug.Assert(search6.Targets.ElementAt(0).Key == createdTarget.Key);

            targetsController.DeleteTarget(aSecondTarget.Key);
            targetsController.DeleteTarget(zThirdTarget.Key);

            var searchDeleted = targetsController.SearchGroupTargets(groupKey);
            Debug.Assert(searchDeleted.TotalCount == 1);
            Debug.Assert(searchDeleted.Targets.Count() == 1);
            Debug.Assert(searchDeleted.Targets.First().Key == createdTarget.Key);

        }
    }
}
