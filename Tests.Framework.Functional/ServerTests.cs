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
    public class ServerTests
    {
        [TestInitialize]
        public void Init()
        {
            var tempDir = new DirectoryInfo(Path.GetTempPath());
            var baseDir = tempDir.CreateSubdirectory(Guid.NewGuid().ToString());
            BaseDirectory = baseDir.FullName;

            StorageProvider = new FileSystemStorageProvider(BaseDirectory);

            Group = new Group() { Name = "Server Test Group" };
            new Groups(StorageProvider).Create(Group);

            Role = new Role() { Name = "Server Test Role", GroupKey = Group.Key };
            new Roles(StorageProvider).Create(Role);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(BaseDirectory, true);
        }

        private string BaseDirectory { get; set; }
        private IStorageProvider StorageProvider { get; set; }
        private Group Group { get; set; }
        private Role Role { get; set; }

        [TestMethod]
        public void ServerControllerTests()
        {
            var instances = new Servers(StorageProvider);
            var instance = new Server()
            {
                GroupKey = Group.Key,
                RoleKey = Role.Key,
                Name = "Test Instance",
            };

            // Exists false
            Assert.IsFalse(instances.Exists(instance.Key));

            // Search no result
            var res1 = instances.Search(Role.Key);
            Assert.AreEqual(0, res1.Items.Count());
            Assert.IsFalse(res1.IsTruncated);

            // Create
            instances.Create(instance);

            // Truncated search
            var createSearch1 = instances.Search(Role.Key, pageSize: 0);
            Assert.AreEqual(0, createSearch1.Items.Count());
            Assert.IsTrue(createSearch1.IsTruncated);

            // Keyword, group search
            var keywordGroupResult = instances.Search(Role.Key, "Test");
            Assert.AreEqual(1, keywordGroupResult.Items.Count());
            Assert.IsFalse(keywordGroupResult.IsTruncated);

            // Null group search
            var nullGroupResult = instances.Search(Guid.NewGuid(), "Test");
            Assert.AreEqual(0, nullGroupResult.Items.Count());

            // No results search
            Assert.AreEqual(0, instances.Search(Role.Key, query: "Updated").Items.Count());

            // Get
            var createdTarget = instances.Get(instance.Key);
            Assert.AreEqual(instance.Key, createdTarget.Key);
            Assert.AreEqual(instance.GroupKey, createdTarget.GroupKey);
            Assert.AreEqual(instance.Name, createdTarget.Name);

            // Update name
            createdTarget.Name = "Updated Instance";
            instances.Update(createdTarget);

            Assert.AreEqual(createdTarget.Name, instances.Get(instance.Key).Name);
            Assert.AreEqual(1, instances.Search(Role.Key, query: "Updated").Items.Count());
            Assert.AreEqual(0, instances.Search(Role.Key, query: "Test").Items.Count());

            // Delete
            instances.Delete(instance.Key);
            Assert.AreEqual(0, instances.Search(Role.Key).Items.Count());
            Assert.AreEqual(0, instances.Search(Role.Key).Items.Count());
            Assert.AreEqual(0, instances.Search(Role.Key, query: "Updated").Items.Count());
            Assert.AreEqual(0, instances.Search(Role.Key, "Updated").Items.Count());
            Assert.IsFalse(instances.Exists(instance.Key));
        }
    }
}
