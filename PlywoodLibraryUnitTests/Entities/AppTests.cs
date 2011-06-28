using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.UnitTesting.Entities
{
    /// <summary>
    /// Summary description for AppTests
    /// </summary>
    [TestClass]
    public class AppTests
    {
        public AppTests()
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
        public void AppSerialiseDeserialise()
        {
            var originalApp = new App()
            {
                Key = Guid.NewGuid(),
                Name = "Test App",
                GroupKey = Guid.NewGuid(),
                DeploymentDirectory = "DeploymentDirectory",
                MajorVersion = "1.51.0",
                Revision = 1,
                Tags = new Dictionary<string, string>()
                {
                    { "tagKey", "Some tag value." },
                    { "secondKey", "Multiline \r\n   test!" }
                }
            };

            App secondApp;
            using (var stream = originalApp.Serialise())
            {
                secondApp = new App(stream);
            }

            Assert.AreEqual(originalApp.Key, secondApp.Key);
            Assert.AreEqual(originalApp.Name, secondApp.Name);
            Assert.AreEqual(originalApp.GroupKey, secondApp.GroupKey);
            Assert.AreEqual(originalApp.DeploymentDirectory, secondApp.DeploymentDirectory);
            Assert.AreEqual(originalApp.MajorVersion, secondApp.MajorVersion);
            Assert.AreEqual(originalApp.Revision, secondApp.Revision);

            Assert.IsNotNull(secondApp.Tags);
            foreach (var tag in originalApp.Tags)
            {
                Assert.IsTrue(secondApp.Tags.ContainsKey(tag.Key));
                Assert.AreEqual(tag.Value, secondApp.Tags[tag.Key]);
            }

        }

        [TestMethod]
        public void AppSerialiseDeserialiseNullTags()
        {
            var originalApp = new App()
            {
                Key = Guid.NewGuid(),
                Name = "Test App",
                GroupKey = Guid.NewGuid(),
                DeploymentDirectory = "DeploymentDirectory",
                MajorVersion = "1.51.0",
                Revision = 1,
            };

            App secondApp;
            using (var stream = originalApp.Serialise())
            {
                secondApp = new App(stream);
            }

            Assert.AreEqual(originalApp.Key, secondApp.Key);
            Assert.AreEqual(originalApp.Name, secondApp.Name);
            Assert.AreEqual(originalApp.GroupKey, secondApp.GroupKey);
            Assert.AreEqual(originalApp.DeploymentDirectory, secondApp.DeploymentDirectory);
            Assert.AreEqual(originalApp.MajorVersion, secondApp.MajorVersion);
            Assert.AreEqual(originalApp.Revision, secondApp.Revision);

            Assert.AreEqual(originalApp.Tags.Count, secondApp.Tags.Count);
        }

    }
}
