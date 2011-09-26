using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.UnitTesting.Entities
{
    [TestClass]
    public class StorageFilePathTests
    {
        [TestMethod]
        public void ValidPathSimpleTest()
        {
            var path = new StorageFilePath("/path");
            Assert.IsTrue(path.IsValid);
        }

        [TestMethod]
        public void ValidPathValidateTest()
        {
            var path = new StorageFilePath("/a/valid/path.extension");
            Assert.IsTrue(path.IsValid);
        }

        [TestMethod]
        public void ValidPathJustFilenameTest()
        {
            var path = new StorageFilePath("/a-valid_path.extension");
            Assert.IsTrue(path.IsValid);
        }

        [TestMethod]
        public void InvalidPathSpacesTest()
        {
            var path = new StorageFilePath("/invaid path.extension");
            Assert.IsFalse(path.IsValid);
        }
    }
}
