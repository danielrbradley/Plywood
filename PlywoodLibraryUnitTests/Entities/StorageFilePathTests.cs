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
            var path = new FilePath("path");
            Assert.IsTrue(path.IsValid);
        }

        [TestMethod]
        public void ValidPathValidateTest()
        {
            var path = new FilePath("a/valid/path.extension");
            Assert.IsTrue(path.IsValid);
        }

        [TestMethod]
        public void ValidPathJustFilenameTest()
        {
            var path = new FilePath("a-valid_path.extension");
            Assert.IsTrue(path.IsValid);
        }

        [TestMethod]
        public void InvalidPathSpacesTest()
        {
            var path = new FilePath("invaid path.extension");
            Assert.IsFalse(path.IsValid);
        }
    }
}
