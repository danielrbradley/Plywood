using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Plywood.FrameworkFunctionalTests
{
    [TestClass]
    public class TargetTests
    {
        [TestInitialize]
        public void Init()
        {
            var tempDir = new DirectoryInfo(Path.GetTempPath());
            var baseDir = tempDir.CreateSubdirectory(Guid.NewGuid().ToString());
            BaseDirectory = baseDir.FullName;

            StorageProvider = new FsProvider.FileSystemStorageProvider(BaseDirectory);

            Group = new Group() { Name = "Target Test Group" };
            new Groups(StorageProvider).Create(Group);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(BaseDirectory, true);
        }

        private string BaseDirectory { get; set; }
        private IStorageProvider StorageProvider { get; set; }
        private Group Group { get; set; }

        [TestMethod]
        public void TargetControllerTests()
        {
            var targets = new Targets(StorageProvider);
            var target = new Target()
            {
                GroupKey = Group.Key,
                Name = "Test Target",
            };

            // Exists false
            Assert.IsFalse(targets.Exists(target.Key));

            // Search no result
            var res1 = targets.Search(Group.Key);
            Assert.AreEqual(0, res1.Targets.Count());
            Assert.IsFalse(res1.IsTruncated);

            // Create
            targets.Create(target);

            // Truncated, global search
            var createSearch1 = targets.Search(pageSize: 0);
            Assert.AreEqual(0, createSearch1.Targets.Count());
            Assert.IsTrue(createSearch1.IsTruncated);

            // Keyword, group search
            var keywordGroupResult = targets.Search(Group.Key, "Test");
            Assert.AreEqual(1, keywordGroupResult.Targets.Count());
            Assert.IsFalse(keywordGroupResult.IsTruncated);

            // Null group search
            var nullGroupResult = targets.Search(Guid.NewGuid(), "Test");
            Assert.AreEqual(1, nullGroupResult.Targets.Count());

            // No results search
            Assert.AreEqual(0, targets.Search(query: "Updated").Targets.Count());

            // Get
            var createdTarget = targets.Get(target.Key);
            Assert.AreEqual(target.Key, createdTarget.Key);
            Assert.AreEqual(target.GroupKey, createdTarget.GroupKey);
            Assert.AreEqual(target.Name, createdTarget.Name);

            // Update name
            createdTarget.Name = "Updated Target";
            targets.Update(createdTarget);

            Assert.AreEqual(createdTarget.Name, targets.Get(target.Key).Name);
            Assert.AreEqual(1, targets.Search(query: "Updated").Targets.Count());
            Assert.AreEqual(0, targets.Search(query: "Test").Targets.Count());

            // Delete
            targets.Delete(target.Key);
            Assert.AreEqual(0, targets.Search().Targets.Count());
            Assert.AreEqual(0, targets.Search(Group.Key).Targets.Count());
            Assert.AreEqual(0, targets.Search(query: "Updated").Targets.Count());
            Assert.AreEqual(0, targets.Search(Group.Key, "Updated").Targets.Count());
            Assert.IsFalse(targets.Exists(target.Key));
        }
    }
}
