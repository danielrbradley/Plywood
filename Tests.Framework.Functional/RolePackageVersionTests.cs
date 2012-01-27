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
    public class RolePackageVersionTests
    {
        [TestInitialize]
        public void Init()
        {
            var tempDir = new DirectoryInfo(@"C:\plywdtmp");
            if (!tempDir.Exists) tempDir.Create();

            var baseDir = tempDir.CreateSubdirectory(Guid.NewGuid().ToString());
            BaseDirectory = baseDir.FullName;

            StorageProvider = new FileSystemStorageProvider(BaseDirectory);

            Group = new Group() { Name = "Role Package Test Group" };
            new Groups(StorageProvider).Create(Group);

            Role = new Role() { Name = "Role Package Test Role", GroupKey = Group.Key };
            new Roles(StorageProvider).Create(Role);

            Package = new Package() { Name = "Role Package Test Package", DeploymentDirectory = "Test", GroupKey = Group.Key, MajorVersion = "1", Revision = 1 };
            new Packages(StorageProvider).Create(Package);

            Version = new Version() { GroupKey = Group.Key, PackageKey = Package.Key, VersionNumber = "1.0", Comment = "Role Package Version Test Version" };
            new Versions(StorageProvider).Create(Version);

            new RolePackages(StorageProvider).Add(Role.Key, Package.Key);
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
        private Package Package { get; set; }
        private Version Version { get; set; }

        [TestMethod]
        public void RolePackageVersionControllerTests()
        {
            var deployedVersions = new RolePackageVersions(StorageProvider);

            // Check not set
            Assert.IsNull(deployedVersions.Get(Role.Key, Package.Key));

            // Check status not set
            Assert.AreEqual(VersionStatus.NotSet, deployedVersions.CheckStatus(Role.Key, Package.Key, Version.Key));

            // Set
            deployedVersions.Set(Role.Key, Package.Key, Version.Key);

            // Check set
            Assert.AreEqual(Version.Key, deployedVersions.Get(Role.Key, Package.Key));

            // Check status up-to-date
            Assert.AreEqual(VersionStatus.NotChanged, deployedVersions.CheckStatus(Role.Key, Package.Key, Version.Key));

            // Check status different
            Assert.AreEqual(VersionStatus.Changed, deployedVersions.CheckStatus(Role.Key, Package.Key, Guid.NewGuid()));

            // Re-set
            deployedVersions.Set(Role.Key, Package.Key, Version.Key);

            // Delete
            deployedVersions.Set(Role.Key, Package.Key, null);
            deployedVersions.Set(Role.Key, Package.Key, null);

            // Check not set
            Assert.IsNull(deployedVersions.Get(Role.Key, Package.Key));

            // Check status not set
            Assert.AreEqual(VersionStatus.NotSet, deployedVersions.CheckStatus(Role.Key, Package.Key, Version.Key));
        }
    }
}
