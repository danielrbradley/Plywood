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
    public class PackageTests
    {
        [TestInitialize]
        public void Init()
        {
            var tempDir = new DirectoryInfo(Path.GetTempPath());
            var baseDir = tempDir.CreateSubdirectory(Guid.NewGuid().ToString());
            BaseDirectory = baseDir.FullName;

            StorageProvider = new FileSystemStorageProvider(BaseDirectory);

            Group = new Group() { Name = "Package Test Group" };
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
        public void PackageControllerTests()
        {
            var packages = new Packages(StorageProvider);
            var package = new Package()
            {
                GroupKey = Group.Key,
                Name = "Test Package",
                DeploymentDirectory = "TestPackage",
                MajorVersion = "1.0",
                Revision = 1,
            };

            // Exists false
            Assert.IsFalse(packages.Exists(package.Key));

            // Search no result
            var noPackages = packages.Search();
            Assert.AreEqual(0, noPackages.Items.Count());
            Assert.IsFalse(noPackages.IsTruncated);

            // Create
            packages.Create(package);

            // Get
            var createdPackage = packages.Get(package.Key);
            Assert.AreEqual(package.Key, createdPackage.Key);
            Assert.AreEqual(package.GroupKey, createdPackage.GroupKey);
            Assert.AreEqual(package.Name, createdPackage.Name);
            Assert.AreEqual(package.DeploymentDirectory, createdPackage.DeploymentDirectory);
            Assert.AreEqual(package.MajorVersion, createdPackage.MajorVersion);
            Assert.AreEqual(package.Revision, createdPackage.Revision);

            // Search 0 rows
            var truncated = packages.Search(pageSize: 0);
            Assert.AreEqual(0, truncated.Items.Count());
            Assert.IsTrue(truncated.IsTruncated);

            // Group search
            Assert.AreEqual(1, packages.Search(Group.Key).Items.Count());

            // Search details
            var detailsSearch = packages.Search(Group.Key, "Test", pageSize: 1);
            Assert.AreEqual(Group.Key, detailsSearch.GroupKey);
            Assert.IsFalse(detailsSearch.IsTruncated);
            Assert.AreEqual(null, detailsSearch.Marker);
            Assert.AreEqual(1, detailsSearch.PageSize);
            Assert.AreEqual("Test", detailsSearch.Query);
            Assert.AreEqual(1, detailsSearch.Items.Count());
            Assert.AreEqual(package.Key, detailsSearch.Items.First().Key);
            Assert.AreEqual(package.Name, detailsSearch.Items.First().Name);
            Assert.AreEqual(package.MajorVersion, detailsSearch.Items.First().MajorVersion);

            // Update
            package.Name = "Updated Package";
            package.Revision = 2;
            package.MajorVersion = "1.2.1";
            packages.Update(package);

            // Check updated details
            var updatedPackage = packages.Get(package.Key);
            Assert.AreEqual(package.Key, updatedPackage.Key);
            Assert.AreEqual(package.GroupKey, updatedPackage.GroupKey);
            Assert.AreEqual(package.Name, updatedPackage.Name);
            Assert.AreEqual(package.DeploymentDirectory, updatedPackage.DeploymentDirectory);
            Assert.AreEqual(package.MajorVersion, updatedPackage.MajorVersion);
            Assert.AreEqual(package.Revision, updatedPackage.Revision);

            // Global keyword search
            Assert.AreEqual(1, packages.Search(query: "Updated").Items.Count());

            // Local keyword search
            Assert.AreEqual(1, packages.Search(Group.Key, "Updated").Items.Count());

            // Keyword no match
            Assert.AreEqual(0, packages.Search(Group.Key, "Test").Items.Count());

            // Delete
            packages.Delete(package.Key);

            // Not exists
            Assert.IsFalse(packages.Exists(package.Key));

            // Search no results
            Assert.AreEqual(0, packages.Search().Items.Count());
        }
    }
}
