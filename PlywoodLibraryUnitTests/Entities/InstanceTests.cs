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
    public class InstanceTests
    {
        public InstanceTests()
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
        public void InstanceSerialiseDeserialise()
        {
            var originalInstance = new Server()
            {
                Key = Guid.NewGuid(),
                GroupKey = Guid.NewGuid(),
                RoleKey = Guid.NewGuid(),
                Name = "Test Instance",
                Tags = new Dictionary<string, string>()
                {
                    { "tagKey", "Some tag value." },
                    { "secondKey", "Multiline \r\n   test!" }
                }
            };

            Server secondInstance;
            using (var stream = originalInstance.Serialise())
            {
                secondInstance = new Server(stream);
            }

            Assert.AreEqual(originalInstance.Key, secondInstance.Key);
            Assert.AreEqual(originalInstance.GroupKey, secondInstance.GroupKey);
            Assert.AreEqual(originalInstance.RoleKey, secondInstance.RoleKey);
            Assert.AreEqual(originalInstance.Name, secondInstance.Name);

            Assert.IsNotNull(secondInstance.Tags);
            foreach (var tag in originalInstance.Tags)
            {
                Assert.IsTrue(secondInstance.Tags.ContainsKey(tag.Key));
                Assert.AreEqual(tag.Value, secondInstance.Tags[tag.Key]);
            }
        }

        [TestMethod]
        public void InstanceSerialiseDeserialiseNullTags()
        {
            var originalInstance = new Server()
            {
                Key = Guid.NewGuid(),
                GroupKey = Guid.NewGuid(),
                RoleKey = Guid.NewGuid(),
                Name = "Test Instance",
            };

            Server secondInstance;
            using (var stream = originalInstance.Serialise())
            {
                secondInstance = new Server(stream);
            }

            Assert.AreEqual(originalInstance.Key, secondInstance.Key);
            Assert.AreEqual(originalInstance.GroupKey, secondInstance.GroupKey);
            Assert.AreEqual(originalInstance.RoleKey, secondInstance.RoleKey);
            Assert.AreEqual(originalInstance.Name, secondInstance.Name);

            Assert.AreEqual(originalInstance.Tags.Count, secondInstance.Tags.Count);
        }

    }
}
