using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Plywood.FrameworkFunctionalTests
{
    [TestClass]
    public class AppTests
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
        public void AppControllerTests()
        {
            var apps = new Apps(StorageProvider);
            var app = new App()
            {
                GroupKey = Group.Key,
                Name = "Test App",
                DeploymentDirectory = "TestApp",
                MajorVersion = "1.0",
                Revision = 1,
            };

            // Exists false
            Assert.IsFalse(apps.AppExists(app.Key));

            // Search no result
            var noApps = apps.Search();
            Assert.AreEqual(0, noApps.Apps.Count());
            Assert.IsFalse(noApps.IsTruncated);

            // Create
            apps.Create(app);

            // Get
            var createdApp = apps.Get(app.Key);
            Assert.AreEqual(app.Key, createdApp.Key);
            Assert.AreEqual(app.GroupKey, createdApp.GroupKey);
            Assert.AreEqual(app.Name, createdApp.Name);
            Assert.AreEqual(app.DeploymentDirectory, createdApp.DeploymentDirectory);
            Assert.AreEqual(app.MajorVersion, createdApp.MajorVersion);
            Assert.AreEqual(app.Revision, createdApp.Revision);

            // Search 0 rows
            var truncated = apps.Search(pageSize: 0);
            Assert.AreEqual(0, truncated.Apps.Count());
            Assert.IsTrue(truncated.IsTruncated);

            // Group search
            Assert.AreEqual(1, apps.Search(Group.Key).Apps.Count());

            // Search details
            var detailsSearch = apps.Search(Group.Key, "Test", pageSize: 1);
            Assert.AreEqual(Group.Key, detailsSearch.GroupKey);
            Assert.IsFalse(detailsSearch.IsTruncated);
            Assert.AreEqual(null, detailsSearch.Marker);
            Assert.AreEqual(1, detailsSearch.PageSize);
            Assert.AreEqual("Test", detailsSearch.Query);
            Assert.AreEqual(1, detailsSearch.Apps.Count());
            Assert.AreEqual(app.Key, detailsSearch.Apps.First().Key);
            Assert.AreEqual(app.GroupKey, detailsSearch.Apps.First().GroupKey);
            Assert.AreEqual(app.Name, detailsSearch.Apps.First().Name);
            Assert.AreEqual(app.MajorVersion, detailsSearch.Apps.First().MajorVersion);

            // Update
            app.Name = "Updated App";
            app.Revision = 2;
            app.MajorVersion = "1.2.1";
            apps.Update(app);

            // Check updated details
            var updatedApp = apps.Get(app.Key);
            Assert.AreEqual(app.Key, updatedApp.Key);
            Assert.AreEqual(app.GroupKey, updatedApp.GroupKey);
            Assert.AreEqual(app.Name, updatedApp.Name);
            Assert.AreEqual(app.DeploymentDirectory, updatedApp.DeploymentDirectory);
            Assert.AreEqual(app.MajorVersion, updatedApp.MajorVersion);
            Assert.AreEqual(app.Revision, updatedApp.Revision);

            // Global keyword search
            Assert.AreEqual(1, apps.Search(query: "Updated").Apps.Count());

            // Local keyword search
            Assert.AreEqual(1, apps.Search(Group.Key, "Updated").Apps.Count());

            // Keyword no match
            Assert.AreEqual(0, apps.Search(Group.Key, "Test").Apps.Count());

            // Delete
            apps.Delete(app.Key);

            // Not exists
            Assert.IsFalse(apps.AppExists(app.Key));

            // Search no results
            Assert.AreEqual(0, apps.Search().Apps.Count());
        }
    }
}
