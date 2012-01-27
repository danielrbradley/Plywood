using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Plywood.Providers;

namespace Plywood.Tests.Framework.Functional
{
    [TestClass]
    public class GroupTests
    {
        [TestInitialize]
        public void Init()
        {
            var tempDir = new DirectoryInfo(Path.GetTempPath());
            var baseDir = tempDir.CreateSubdirectory(Guid.NewGuid().ToString());
            BaseDirectory = baseDir.FullName;

            StorageProvider = new FileSystemStorageProvider(BaseDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(BaseDirectory, true);
        }

        private string BaseDirectory { get; set; }
        private IStorageProvider StorageProvider { get; set; }

        [TestMethod]
        public void GroupControllerTests()
        {
            var groups = new Groups(StorageProvider);
            var newGroup = new Group()
            {
                Name = "Test Group A",
            };

            // Test Exists false.
            Assert.IsFalse(groups.Exists(newGroup.Key));

            // Test search.
            var searchRes1 = groups.Search();
            Assert.AreEqual(0, searchRes1.Groups.Count());
            Assert.AreEqual(false, searchRes1.IsTruncated);

            var searchRes2 = groups.Search("query", "marker", 0);
            Assert.AreEqual(0, searchRes2.Groups.Count());
            Assert.AreEqual(false, searchRes2.IsTruncated);
            Assert.AreEqual("marker", searchRes2.Marker);
            Assert.AreEqual(0, searchRes2.PageSize);

            // Test create.
            groups.Create(newGroup);

            // Test search.
            var searchRes3 = groups.Search(pageSize: 0);
            Assert.AreEqual(0, searchRes3.Groups.Count());
            Assert.AreEqual(true, searchRes3.IsTruncated);

            var searchRes4 = groups.Search("test", null, 1);
            Assert.AreEqual(1, searchRes4.Groups.Count());
            Assert.AreEqual(false, searchRes4.IsTruncated);
            Assert.AreEqual(1, searchRes4.PageSize);
            Assert.AreEqual(newGroup.Key, searchRes4.Groups.First().Key);
            Assert.AreEqual(newGroup.Name, searchRes4.Groups.First().Name);

            // Test Exists true.
            Assert.IsTrue(groups.Exists(newGroup.Key));

            // Test get.
            var createdGroup = groups.Get(newGroup.Key);
            Assert.AreEqual(createdGroup.Key, newGroup.Key);
            Assert.AreEqual(createdGroup.Name, newGroup.Name);
            
            // Update name.
            createdGroup.Name = "Test Group B";
            groups.Update(createdGroup);
            var updatedGroup = groups.Get(newGroup.Key);
            Assert.AreEqual(updatedGroup.Name, createdGroup.Name);

            // Test search.
            var searchRes5 = groups.Search("A");
            Assert.AreEqual(0, searchRes5.Groups.Count());
            Assert.AreEqual(false, searchRes5.IsTruncated);

            // Test search.
            var searchRes6 = groups.Search("B");
            Assert.AreEqual(1, searchRes6.Groups.Count());
            Assert.AreEqual(updatedGroup.Name, searchRes6.Groups.First().Name);

            // Delete
            groups.Delete(newGroup.Key);
            Assert.AreEqual(0, groups.Search().Groups.Count());
            Assert.AreEqual(0, groups.Search("Test").Groups.Count());
            Assert.AreEqual(0, groups.Search("Group").Groups.Count());
            Assert.AreEqual(0, groups.Search("A").Groups.Count());
            Assert.AreEqual(0, groups.Search("B").Groups.Count());
            Assert.IsFalse(groups.Exists(newGroup.Key));
        }
    }
}
