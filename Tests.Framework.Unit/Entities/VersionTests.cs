using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.Framework.Unit.Entities
{
    /// <summary>
    /// Summary description for AppTests
    /// </summary>
    [TestClass]
    public class VersionTests
    {
        public VersionTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void VersionSerialiseDeserialise()
        {
            var originalVersion = new Version()
            {
                Key = Guid.NewGuid(),
                GroupKey = Guid.NewGuid(),
                PackageKey = Guid.NewGuid(),
                VersionNumber = "0.1",
                Comment = "Test Version",
                Tags = new Dictionary<string, string>()
                {
                    { "tagKey", "Some tag value." },
                    { "secondKey", "Multiline \r\n   test!" }
                }
            };

            Version secondVersion;
            using (var stream = originalVersion.Serialise())
            {
                secondVersion = new Version(stream);
            }

            Assert.AreEqual(originalVersion.Key, secondVersion.Key);
            Assert.AreEqual(originalVersion.GroupKey, secondVersion.GroupKey);
            Assert.AreEqual(originalVersion.PackageKey, secondVersion.PackageKey);
            Assert.AreEqual(originalVersion.Timestamp, secondVersion.Timestamp);
            Assert.AreEqual(originalVersion.Comment, secondVersion.Comment);
            Assert.AreEqual(originalVersion.VersionNumber, secondVersion.VersionNumber);

            Assert.IsNotNull(secondVersion.Tags);
            foreach (var tag in originalVersion.Tags)
            {
                Assert.IsTrue(secondVersion.Tags.ContainsKey(tag.Key));
                Assert.AreEqual(tag.Value, secondVersion.Tags[tag.Key]);
            }
        }

        [TestMethod]
        public void VersionSerialiseDeserialiseNullTags()
        {
            var originalVersion = new Version()
            {
                Key = Guid.NewGuid(),
                GroupKey = Guid.NewGuid(),
                PackageKey = Guid.NewGuid(),
                VersionNumber = "0.2",
                Comment = "Test Version",
            };

            Version secondVersion;
            using (var stream = originalVersion.Serialise())
            {
                secondVersion = new Version(stream);
            }

            Assert.AreEqual(originalVersion.Key, secondVersion.Key);
            Assert.AreEqual(originalVersion.GroupKey, secondVersion.GroupKey);
            Assert.AreEqual(originalVersion.PackageKey, secondVersion.PackageKey);
            Assert.AreEqual(originalVersion.Timestamp, secondVersion.Timestamp);
            Assert.AreEqual(originalVersion.Comment, secondVersion.Comment);
            Assert.AreEqual(originalVersion.VersionNumber, secondVersion.VersionNumber);

            Assert.AreEqual(originalVersion.Tags.Count, secondVersion.Tags.Count);
        }

    }
}
