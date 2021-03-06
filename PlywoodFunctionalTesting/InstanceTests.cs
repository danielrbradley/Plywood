﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Plywood.Tests.Functional
{
    static class InstanceTests
    {
        public static Instance Run(ControllerConfiguration context, Guid targetKey)
        {
            var instancesController = new Instances(context);

            var searchEmpty = instancesController.SearchInstances(targetKey);
            Debug.Assert(searchEmpty.TotalCount == 0);
            Debug.Assert(searchEmpty.Instances.Count() == 0);

            var testInstance = new Instance() { TargetKey = targetKey };
            instancesController.CreateInstance(testInstance);
            var createdInstance = instancesController.GetInstance(testInstance.Key);
            Debug.Assert(testInstance.Key == createdInstance.Key);
            Debug.Assert(testInstance.Name == createdInstance.Name);
            Debug.Assert(testInstance.TargetKey == createdInstance.TargetKey);

            var searchSingle = instancesController.SearchInstances(targetKey);
            Debug.Assert(searchSingle.TotalCount == 1);
            Debug.Assert(searchSingle.Instances.Count() == 1);
            Debug.Assert(searchSingle.Instances.First().Key == createdInstance.Key);
            Debug.Assert(searchSingle.Instances.First().Name == createdInstance.Name);

            createdInstance.Tags.Add("Foo", "Bar");
            instancesController.UpdateInstance(createdInstance);

            var taggedInstance = instancesController.GetInstance(createdInstance.Key);
            Debug.Assert(taggedInstance.Tags.Count == 1);
            Debug.Assert(taggedInstance.Tags.ContainsKey("Foo"));
            Debug.Assert(taggedInstance.Tags["Foo"] == "Bar");

            taggedInstance.Name = "Updated Test Instance";
            instancesController.UpdateInstance(taggedInstance);

            var renamedInstance = instancesController.GetInstance(taggedInstance.Key);
            Debug.Assert(renamedInstance.Name == taggedInstance.Name);

            var searchRenamed = instancesController.SearchInstances(targetKey);
            Debug.Assert(searchRenamed.TotalCount == 1);
            Debug.Assert(searchRenamed.Instances.First().Name == renamedInstance.Name);

            taggedInstance.Name = "Test Instance";
            instancesController.UpdateInstance(taggedInstance);

            SearchAndDelete(targetKey, instancesController, taggedInstance);

            return testInstance;
        }

        private static void SearchAndDelete(Guid targetKey, Instances instancesController, Instance instance)
        {
            var aSecondInstance = new Instance() { Name = "A Second Instance", TargetKey = targetKey };
            instancesController.CreateInstance(aSecondInstance);
            var zThirdInstance = new Instance() { Name = "Z Third Instance", TargetKey = targetKey };
            instancesController.CreateInstance(zThirdInstance);

            var search1 = instancesController.SearchInstances(targetKey);
            Debug.Assert(search1.TotalCount == 3);
            Debug.Assert(search1.Instances.Count() == 3);
            Debug.Assert(search1.Instances.ElementAt(0).Key == aSecondInstance.Key);
            Debug.Assert(search1.Instances.ElementAt(1).Key == instance.Key);
            Debug.Assert(search1.Instances.ElementAt(2).Key == zThirdInstance.Key);

            var search2 = instancesController.SearchInstances(targetKey, "Test");
            Debug.Assert(search2.TotalCount == 1);
            Debug.Assert(search2.Instances.Count() == 1);
            Debug.Assert(search2.Instances.ElementAt(0).Key == instance.Key);

            var search3 = instancesController.SearchInstances(targetKey, pageSize: 1);
            Debug.Assert(search3.TotalCount == 3);
            Debug.Assert(search3.Instances.Count() == 1);
            Debug.Assert(search3.Instances.ElementAt(0).Key == aSecondInstance.Key);

            var search4 = instancesController.SearchInstances(targetKey, pageSize: 2);
            Debug.Assert(search4.TotalCount == 3);
            Debug.Assert(search4.Instances.Count() == 2);
            Debug.Assert(search4.Instances.ElementAt(0).Key == aSecondInstance.Key);
            Debug.Assert(search4.Instances.ElementAt(1).Key == instance.Key);

            var search5 = instancesController.SearchInstances(targetKey, offset: 1);
            Debug.Assert(search5.TotalCount == 3);
            Debug.Assert(search5.Instances.Count() == 2);
            Debug.Assert(search5.Instances.ElementAt(0).Key == instance.Key);
            Debug.Assert(search5.Instances.ElementAt(1).Key == zThirdInstance.Key);

            var search6 = instancesController.SearchInstances(targetKey, offset: 1, pageSize: 1);
            Debug.Assert(search6.TotalCount == 3);
            Debug.Assert(search6.Instances.Count() == 1);
            Debug.Assert(search6.Instances.ElementAt(0).Key == instance.Key);

            instancesController.DeleteInstance(aSecondInstance.Key);
            instancesController.DeleteInstance(zThirdInstance.Key);

            var searchDeleted = instancesController.SearchInstances(targetKey);
            Debug.Assert(searchDeleted.TotalCount == 1);
            Debug.Assert(searchDeleted.Instances.Count() == 1);
            Debug.Assert(searchDeleted.Instances.ElementAt(0).Key == instance.Key);
        }
    }
}
