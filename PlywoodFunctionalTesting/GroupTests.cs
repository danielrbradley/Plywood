using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood;
using System.Diagnostics;

namespace Plywood.Tests.Functional
{
    static class GroupTests
    {
        public static Group Run(ControllerConfiguration context)
        {
            var groupsController = new Groups(context);

            var searchEmpty = groupsController.Search();
            Debug.Assert(searchEmpty.TotalCount == 0);
            Debug.Assert(searchEmpty.Groups.Count() == 0);

            var testGroup = new Group() { Name = "Test Group" };
            groupsController.Create(testGroup);
            var createdGroup = groupsController.Get(testGroup.Key);
            Debug.Assert(createdGroup.Name == testGroup.Name);

            var searchSingle = groupsController.Search();
            Debug.Assert(searchSingle.TotalCount == 1);
            Debug.Assert(searchSingle.Groups.Count() == 1);
            Debug.Assert(searchSingle.Groups.First().Key == createdGroup.Key);
            Debug.Assert(searchSingle.Groups.First().Name == createdGroup.Name);

            createdGroup.Tags.Add("Foo", "Bar");
            groupsController.Update(createdGroup);

            var taggedGroup = groupsController.Get(testGroup.Key);
            Debug.Assert(taggedGroup.Tags.ContainsKey("Foo"));
            Debug.Assert(taggedGroup.Tags["Foo"] == "Bar");
            var searchUpdated = groupsController.Search();
            Debug.Assert(searchUpdated.TotalCount == 1);
            Debug.Assert(searchUpdated.Groups.Count() == 1);
            Debug.Assert(searchUpdated.Groups.First().Key == createdGroup.Key);
            Debug.Assert(searchUpdated.Groups.First().Name == createdGroup.Name);

            taggedGroup.Name = "Updated Test Group";
            groupsController.Update(taggedGroup);

            var renamedGroup = groupsController.Get(testGroup.Key);
            Debug.Assert(renamedGroup.Name == taggedGroup.Name);
            var searchRenamed = groupsController.Search();
            Debug.Assert(searchRenamed.TotalCount == 1);
            Debug.Assert(searchRenamed.Groups.Count() == 1);
            Debug.Assert(searchRenamed.Groups.First().Key == taggedGroup.Key);
            Debug.Assert(searchRenamed.Groups.First().Name == taggedGroup.Name);

            var aSecondGroup = new Group() { Name = "A Second Group" };
            groupsController.Create(aSecondGroup);
            var zThirdGroup = new Group() { Name = "Z Third Group" };
            groupsController.Create(zThirdGroup);

            var search1 = groupsController.Search();
            Debug.Assert(search1.TotalCount == 3);
            Debug.Assert(search1.Groups.Count() == 3);
            Debug.Assert(search1.Groups.ElementAt(0).Key == aSecondGroup.Key);
            Debug.Assert(search1.Groups.ElementAt(1).Key == createdGroup.Key);
            Debug.Assert(search1.Groups.ElementAt(2).Key == zThirdGroup.Key);

            var search2 = groupsController.Search("Updated");
            Debug.Assert(search2.TotalCount == 1);
            Debug.Assert(search2.Groups.Count() == 1);
            Debug.Assert(search2.Groups.ElementAt(0).Key == createdGroup.Key);

            var search3 = groupsController.Search(pageSize: 1);
            Debug.Assert(search3.TotalCount == 3);
            Debug.Assert(search3.Groups.Count() == 1);
            Debug.Assert(search3.Groups.ElementAt(0).Key == aSecondGroup.Key);

            var search4 = groupsController.Search(pageSize: 2);
            Debug.Assert(search4.TotalCount == 3);
            Debug.Assert(search4.Groups.Count() == 2);
            Debug.Assert(search4.Groups.ElementAt(0).Key == aSecondGroup.Key);
            Debug.Assert(search4.Groups.ElementAt(1).Key == createdGroup.Key);

            var search5 = groupsController.Search(offset: 1);
            Debug.Assert(search5.TotalCount == 3);
            Debug.Assert(search5.Groups.Count() == 2);
            Debug.Assert(search5.Groups.ElementAt(0).Key == createdGroup.Key);
            Debug.Assert(search5.Groups.ElementAt(1).Key == zThirdGroup.Key);

            var search6 = groupsController.Search(offset: 1, pageSize: 1);
            Debug.Assert(search6.TotalCount == 3);
            Debug.Assert(search6.Groups.Count() == 1);
            Debug.Assert(search6.Groups.ElementAt(0).Key == createdGroup.Key);

            groupsController.Delete(aSecondGroup.Key);
            groupsController.Delete(zThirdGroup.Key);

            var searchDeleted = groupsController.Search();
            Debug.Assert(searchDeleted.TotalCount == 1);
            Debug.Assert(searchDeleted.Groups.Count() == 1);
            Debug.Assert(searchDeleted.Groups.First().Key == taggedGroup.Key);
            Debug.Assert(searchDeleted.Groups.First().Name == taggedGroup.Name);

            groupsController.Update(testGroup);
            var searchReset = groupsController.Search();
            Debug.Assert(searchReset.TotalCount == 1);
            Debug.Assert(searchReset.Groups.Count() == 1);
            Debug.Assert(searchReset.Groups.First().Key == testGroup.Key);
            Debug.Assert(searchReset.Groups.First().Name == testGroup.Name);

            // Create default shared group.
            groupsController.Create(new Group() { Key = new Guid("5615c002-2d9a-46c4-a9a3-26b2b19cd790"), Name = "Shared" });

            return testGroup;
        }
    }
}
