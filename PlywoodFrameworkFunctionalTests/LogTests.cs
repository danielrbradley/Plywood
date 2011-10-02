using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Plywood.FrameworkFunctionalTests
{
    [TestClass]
    public class LogTests
    {
        [TestInitialize]
        public void Init()
        {
            var tempDir = new DirectoryInfo(Path.GetTempPath());
            var baseDir = tempDir.CreateSubdirectory(Guid.NewGuid().ToString());
            BaseDirectory = baseDir.FullName;

            StorageProvider = new FsProvider.FileSystemStorageProvider(BaseDirectory);

            Group = new Group() { Name = "Instance Test Group" };
            new Groups(StorageProvider).Create(Group);

            Role = new Role() { Name = "Instance Test Role", GroupKey = Group.Key };
            new Roles(StorageProvider).Create(Role);

            Server = new Server() { Name = "Log Test Server", GroupKey = Group.Key, RoleKey = Role.Key };
            new Servers(StorageProvider).Create(Server);
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
        private Server Server { get; set; }

        [TestMethod]
        public void RoleControllerTests()
        {
            var logs = new Logs(StorageProvider);
            var logEntry = new LogEntry()
            {
                GroupKey = Group.Key,
                RoleKey = Role.Key,
                ServerKey = Server.Key,
                Status = LogStatus.Ok,
                LogContent = "Test Log Entry",
                Timestamp = DateTime.UtcNow.AddSeconds(-2),
            };

            // Empty search
            var emptySearch = logs.Search(Server.Key);
            Assert.IsFalse(emptySearch.IsTruncated);
            Assert.AreEqual(0, emptySearch.Items.Count());

            // Create
            logs.Create(logEntry);

            // Simple search
            Assert.AreEqual(1, logs.Search(Server.Key).Items.Count());

            // Truncated Search
            Assert.IsTrue(logs.Search(Server.Key, pageSize: 0).IsTruncated);

            // Keyword search match
            Assert.AreEqual(1, logs.Search(Server.Key, "Ok").Items.Count());

            // Keyword search no match
            var keywordNoMatch = logs.Search(Server.Key, "Error");
            Assert.AreEqual(0, keywordNoMatch.Items.Count());
            Assert.IsFalse(keywordNoMatch.IsTruncated);

            // Other server search
            Assert.AreEqual(0, logs.Search(Guid.NewGuid()).Items.Count());

            // Create second
            var secondLogEntry = new LogEntry()
            {
                GroupKey = Group.Key,
                RoleKey = Role.Key,
                ServerKey = Server.Key,
                LogContent = "Something went wrong!",
                Status = LogStatus.Error,
                Timestamp = DateTime.UtcNow,
            };
            logs.Create(secondLogEntry);

            // Check second details
            var created = logs.Get(secondLogEntry.Key);
            Assert.AreEqual(secondLogEntry.Key, created.Key);
            Assert.AreEqual(secondLogEntry.GroupKey, created.GroupKey);
            Assert.AreEqual(secondLogEntry.RoleKey, created.RoleKey);
            Assert.AreEqual(secondLogEntry.ServerKey, created.ServerKey);
            Assert.AreEqual(secondLogEntry.LogContent, created.LogContent);
            Assert.AreEqual(secondLogEntry.Status, created.Status);
            Assert.AreEqual(secondLogEntry.Timestamp, created.Timestamp);

            // Second is first
            var secondSearch = logs.Search(Server.Key, pageSize: 1);
            Assert.AreEqual(1, secondSearch.Items.Count());
            Assert.AreEqual(secondLogEntry.Key, secondSearch.Items.First().Key);
            Assert.IsTrue(secondSearch.IsTruncated);

            // First is second
            var firstSecond = logs.Search(Server.Key, marker: secondSearch.NextMarker);
            Assert.AreEqual(1, firstSecond.Items.Count());
            Assert.AreEqual(logEntry.Key, firstSecond.Items.First().Key);
            Assert.AreEqual(logEntry.Status, firstSecond.Items.First().Status);
            Assert.AreEqual(logEntry.Timestamp, firstSecond.Items.First().Timestamp);

        }
    }
}
