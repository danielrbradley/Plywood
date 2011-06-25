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

            var searchEmpty = groupsController.SearchGroups();
            Debug.Assert(searchEmpty.TotalCount == 0);
            Debug.Assert(searchEmpty.Groups.Count() == 0);

            var testGroup = new Group() { Name = "Test Group" };
            groupsController.CreateGroup(testGroup);
            var createdGroup = groupsController.GetGroup(testGroup.Key);
            Debug.Assert(createdGroup.Name == testGroup.Name);

            var searchSingle = groupsController.SearchGroups();
            Debug.Assert(searchSingle.TotalCount == 1);
            Debug.Assert(searchSingle.Groups.Count() == 1);
            Debug.Assert(searchSingle.Groups.First().Key == createdGroup.Key);
            Debug.Assert(searchSingle.Groups.First().Name == createdGroup.Name);

            createdGroup.Tags.Add("Foo", "Bar");
            groupsController.UpdateGroup(createdGroup);

            var taggedGroup = groupsController.GetGroup(testGroup.Key);
            Debug.Assert(taggedGroup.Tags.ContainsKey("Foo"));
            Debug.Assert(taggedGroup.Tags["Foo"] == "Bar");
            var searchUpdated = groupsController.SearchGroups();
            Debug.Assert(searchUpdated.TotalCount == 1);
            Debug.Assert(searchUpdated.Groups.Count() == 1);
            Debug.Assert(searchUpdated.Groups.First().Key == createdGroup.Key);
            Debug.Assert(searchUpdated.Groups.First().Name == createdGroup.Name);

            taggedGroup.Name = "Updated Test Group";
            groupsController.UpdateGroup(taggedGroup);

            var renamedGroup = groupsController.GetGroup(testGroup.Key);
            Debug.Assert(renamedGroup.Name == taggedGroup.Name);
            var searchRenamed = groupsController.SearchGroups();
            Debug.Assert(searchRenamed.TotalCount == 1);
            Debug.Assert(searchRenamed.Groups.Count() == 1);
            Debug.Assert(searchRenamed.Groups.First().Key == taggedGroup.Key);
            Debug.Assert(searchRenamed.Groups.First().Name == taggedGroup.Name);

            var aSecondGroup = new Group() { Name = "A Second Group" };
            groupsController.CreateGroup(aSecondGroup);
            var zThirdGroup = new Group() { Name = "Z Third Group" };
            groupsController.CreateGroup(zThirdGroup);

            var search1 = groupsController.SearchGroups();
            Debug.Assert(search1.TotalCount == 3);
            Debug.Assert(search1.Groups.Count() == 3);
            Debug.Assert(search1.Groups.ElementAt(0).Key == aSecondGroup.Key);
            Debug.Assert(search1.Groups.ElementAt(1).Key == createdGroup.Key);
            Debug.Assert(search1.Groups.ElementAt(2).Key == zThirdGroup.Key);

            var search2 = groupsController.SearchGroups("Updated");
            Debug.Assert(search2.TotalCount == 1);
            Debug.Assert(search2.Groups.Count() == 1);
            Debug.Assert(search2.Groups.ElementAt(0).Key == createdGroup.Key);

            var search3 = groupsController.SearchGroups(pageSize: 1);
            Debug.Assert(search3.TotalCount == 3);
            Debug.Assert(search3.Groups.Count() == 1);
            Debug.Assert(search3.Groups.ElementAt(0).Key == aSecondGroup.Key);

            var search4 = groupsController.SearchGroups(pageSize: 2);
            Debug.Assert(search4.TotalCount == 3);
            Debug.Assert(search4.Groups.Count() == 2);
            Debug.Assert(search4.Groups.ElementAt(0).Key == aSecondGroup.Key);
            Debug.Assert(search4.Groups.ElementAt(1).Key == createdGroup.Key);

            var search5 = groupsController.SearchGroups(offset: 1);
            Debug.Assert(search5.TotalCount == 3);
            Debug.Assert(search5.Groups.Count() == 2);
            Debug.Assert(search5.Groups.ElementAt(0).Key == createdGroup.Key);
            Debug.Assert(search5.Groups.ElementAt(1).Key == zThirdGroup.Key);

            var search6 = groupsController.SearchGroups(offset: 1, pageSize: 1);
            Debug.Assert(search6.TotalCount == 3);
            Debug.Assert(search6.Groups.Count() == 1);
            Debug.Assert(search6.Groups.ElementAt(0).Key == createdGroup.Key);

            groupsController.DeleteGroup(aSecondGroup.Key);
            groupsController.DeleteGroup(zThirdGroup.Key);

            var searchDeleted = groupsController.SearchGroups();
            Debug.Assert(searchDeleted.TotalCount == 1);
            Debug.Assert(searchDeleted.Groups.Count() == 1);
            Debug.Assert(searchDeleted.Groups.First().Key == taggedGroup.Key);
            Debug.Assert(searchDeleted.Groups.First().Name == taggedGroup.Name);

            groupsController.UpdateGroup(testGroup);
            var searchReset = groupsController.SearchGroups();
            Debug.Assert(searchReset.TotalCount == 1);
            Debug.Assert(searchReset.Groups.Count() == 1);
            Debug.Assert(searchReset.Groups.First().Key == testGroup.Key);
            Debug.Assert(searchReset.Groups.First().Name == testGroup.Name);

            // Create default shared group.
            groupsController.CreateGroup(new Group() { Key = new Guid("5615c002-2d9a-46c4-a9a3-26b2b19cd790"), Name = "Shared" });

            return testGroup;
        }
    }
}
