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
            new Groups(StorageProvider).CreateGroup(Group);
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
            Assert.IsFalse(targets.TargetExists(target.Key));

            // Search no result
            var res1 = targets.SearchTargets(Group.Key);
            Assert.AreEqual(0, res1.Targets.Count());
            Assert.IsFalse(res1.IsTruncated);

            // Create
            targets.CreateTarget(target);

            // Truncated, global search
            var createSearch1 = targets.SearchTargets(pageSize: 0);
            Assert.AreEqual(0, createSearch1.Targets.Count());
            Assert.IsTrue(createSearch1.IsTruncated);

            // Keyword, group search
            var keywordGroupResult = targets.SearchTargets(Group.Key, "Test");
            Assert.AreEqual(1, keywordGroupResult.Targets.Count());
            Assert.IsFalse(keywordGroupResult.IsTruncated);

            // Null group search
            var nullGroupResult = targets.SearchTargets(Guid.NewGuid(), "Test");
            Assert.AreEqual(1, nullGroupResult.Targets.Count());

            // No results search
            Assert.AreEqual(0, targets.SearchTargets(query: "Updated").Targets.Count());

            // Get
            var createdTarget = targets.GetTarget(target.Key);
            Assert.AreEqual(target.Key, createdTarget.Key);
            Assert.AreEqual(target.GroupKey, createdTarget.GroupKey);
            Assert.AreEqual(target.Name, createdTarget.Name);

            // Update name
            createdTarget.Name = "Updated Target";
            targets.UpdateTarget(createdTarget);

            Assert.AreEqual(createdTarget.Name, targets.GetTarget(target.Key).Name);
            Assert.AreEqual(1, targets.SearchTargets(query: "Updated").Targets.Count());
            Assert.AreEqual(0, targets.SearchTargets(query: "Test").Targets.Count());

            // Delete
            targets.DeleteTarget(target.Key);
            Assert.AreEqual(0, targets.SearchTargets().Targets.Count());
            Assert.AreEqual(0, targets.SearchTargets(Group.Key).Targets.Count());
            Assert.AreEqual(0, targets.SearchTargets(query: "Updated").Targets.Count());
            Assert.AreEqual(0, targets.SearchTargets(Group.Key, "Updated").Targets.Count());
            Assert.IsFalse(targets.TargetExists(target.Key));
        }
    }
}
