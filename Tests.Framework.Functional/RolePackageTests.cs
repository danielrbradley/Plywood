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
    public class RolePackageTests
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

        [TestMethod]
        public void RolePackageControllerTests()
        {
            var rolePackages = new RolePackages(StorageProvider);

            // Search no results
            Assert.AreEqual(0, rolePackages.SearchPackages(Role.Key).Items.Count());
            Assert.AreEqual(0, rolePackages.SearchRoles(Package.Key).Items.Count());

            // Add
            rolePackages.Add(Role.Key, Package.Key);

            // Search packages
            var packages = rolePackages.SearchPackages(Role.Key);
            Assert.AreEqual(1, packages.Items.Count());
            Assert.AreEqual(Package.Key, packages.Items.First().Key);
            Assert.AreEqual(Package.Name, packages.Items.First().Name);
            Assert.AreEqual(Package.DeploymentDirectory, packages.Items.First().DeploymentDirectory);

            // Search roles
            PackageRoleList roles = rolePackages.SearchRoles(Package.Key);
            Assert.AreEqual(1, roles.Items.Count());
            Assert.AreEqual(Role.Key, roles.Items.First().Key);
            Assert.AreEqual(Role.Name, roles.Items.First().Name);

            // Rename package
            Package.Name = "Updated Role Package Test Package";
            new Packages(StorageProvider).Update(Package);
            Assert.AreEqual(Package.Name, rolePackages.SearchPackages(Role.Key).Items.First().Name);

            // Rename role
            Role.Name = "Updated Role Package Test Role";
            new Roles(StorageProvider).Update(Role);
            Assert.AreEqual(Role.Name, rolePackages.SearchRoles(Package.Key).Items.First().Name);
        }
    }
}
