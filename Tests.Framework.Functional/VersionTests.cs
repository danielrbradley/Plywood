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
    public class VersionTests
    {
        [TestInitialize]
        public void Init()
        {
            var tempDir = new DirectoryInfo(@"c:\plywdtmp");
            if (!tempDir.Exists) tempDir.Create();
            var baseDir = tempDir.CreateSubdirectory(Guid.NewGuid().ToString());
            BaseDirectory = baseDir.FullName;

            StorageProvider = new FileSystemStorageProvider(BaseDirectory);

            Group = new Group() { Name = "Version Test Group" };
            new Groups(StorageProvider).Create(Group);

            Package = new Package() { Name = "Version Test Package", GroupKey = Group.Key, DeploymentDirectory = "PackageDir" };
            new Packages(StorageProvider).Create(Package);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(BaseDirectory, true);
        }

        private string BaseDirectory { get; set; }
        private IStorageProvider StorageProvider { get; set; }
        private Group Group { get; set; }
        private Package Package { get; set; }

        [TestMethod]
        public void VersionControllerTests()
        {
            var versions = new Versions(StorageProvider);
            var version = new Version()
            {
                GroupKey = Group.Key,
                PackageKey = Package.Key,
                VersionNumber = "1.2.3",
                Comment = "Test Version",
            };

            // Exists false
            Assert.IsFalse(versions.Exists(version.Key));

            // Search no result
            Assert.AreEqual(0, versions.Search(Package.Key).Items.Count());

            // Create
            versions.Create(version);

            // Get
            var createdVersion = versions.Get(version.Key);
            Assert.AreEqual(version.Key, createdVersion.Key);
            Assert.AreEqual(version.GroupKey, createdVersion.GroupKey);
            Assert.AreEqual(version.PackageKey, createdVersion.PackageKey);
            Assert.AreEqual(version.VersionNumber, createdVersion.VersionNumber);
            Assert.AreEqual(version.Comment, createdVersion.Comment);
            Assert.IsTrue(Math.Abs((version.Timestamp - createdVersion.Timestamp).TotalSeconds) < 1);

            // Truncated search
            var truncatedSearch = versions.Search(Package.Key, pageSize: 0);
            Assert.AreEqual(0, truncatedSearch.Items.Count());
            Assert.IsTrue(truncatedSearch.IsTruncated);

            // Keyword exact count search
            var keywordSearch = versions.Search(Package.Key, "Test", pageSize: 1);
            Assert.AreEqual(1, keywordSearch.Items.Count());
            Assert.IsFalse(keywordSearch.IsTruncated);
            Assert.AreEqual(Package.Key, keywordSearch.PackageKey);
            Assert.AreEqual("Test", keywordSearch.Query);
            Assert.AreEqual(1, keywordSearch.PageSize);
            Assert.AreEqual(null, keywordSearch.Marker);
            Assert.AreEqual(version.Key, keywordSearch.Items.First().Key);
            Assert.AreEqual(version.VersionNumber, keywordSearch.Items.First().VersionNumber);
            Assert.AreEqual(version.Comment, keywordSearch.Items.First().Comment);
            Assert.IsTrue(Math.Abs((version.Timestamp - keywordSearch.Items.First().Timestamp).TotalSeconds) < 1);

            // Version number search
            Assert.AreEqual(1, versions.Search(Package.Key, "1.2.3").Items.Count());
            Assert.AreEqual(1, versions.Search(Package.Key, "1.2").Items.Count());

            // Update
            version.Comment = "Updated Version";
            version.VersionNumber = "1.2.4";
            versions.Update(version);

            // Check update
            var updatedVersion = versions.Get(version.Key);
            Assert.AreEqual(version.Comment, updatedVersion.Comment);
            Assert.AreEqual(version.VersionNumber, updatedVersion.VersionNumber);

            Assert.AreEqual(1, versions.Search(Package.Key, "Updated").Items.Count());
            Assert.AreEqual(0, versions.Search(Package.Key, "Test").Items.Count());
            Assert.AreEqual(0, versions.Search(Package.Key, "1.2.3").Items.Count());
            Assert.AreEqual(1, versions.Search(Package.Key, "1.2").Items.Count());
            Assert.AreEqual(1, versions.Search(Package.Key, "1.2.4").Items.Count());

            // Delete
            versions.Delete(version.Key);
            Assert.IsFalse(versions.Exists(version.Key));
            Assert.AreEqual(0, versions.Search(Package.Key, "Updated").Items.Count());
            Assert.AreEqual(0, versions.Search(Package.Key, "1.2").Items.Count());
            Assert.AreEqual(0, versions.Search(Package.Key, "1.2.4").Items.Count());
        }
    }
}
