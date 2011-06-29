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
    public class GroupTests
    {
        public GroupTests()
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
        public void GroupSerialiseDeserialise()
        {
            var originalGroup = new Group()
            {
                Key = Guid.NewGuid(),
                Name = "Test Group",
                Tags = new Dictionary<string, string>()
                {
                    { "tagKey", "Some tag value." },
                    { "secondKey", "Multiline \r\n   test!" }
                }
            };

            Group secondGroup;
            using (var stream = originalGroup.Serialise())
            {
                secondGroup = new Group(stream);
            }

            Assert.AreEqual(originalGroup.Key, secondGroup.Key);
            Assert.AreEqual(originalGroup.Name, secondGroup.Name);

            Assert.IsNotNull(secondGroup.Tags);
            foreach (var tag in originalGroup.Tags)
            {
                Assert.IsTrue(secondGroup.Tags.ContainsKey(tag.Key));
                Assert.AreEqual(tag.Value, secondGroup.Tags[tag.Key]);
            }
        }

        [TestMethod]
        public void GroupSerialiseDeserialiseNullTags()
        {
            var originalGroup = new Group()
            {
                Key = Guid.NewGuid(),
                Name = "Test Group",
                Tags = new Dictionary<string, string>()
                {
                    { "tagKey", "Some tag value." },
                    { "secondKey", "Multiline \r\n   test!" }
                }
            };

            Group secondGroup;
            using (var stream = originalGroup.Serialise())
            {
                secondGroup = new Group(stream);
            }

            Assert.AreEqual(originalGroup.Key, secondGroup.Key);
            Assert.AreEqual(originalGroup.Name, secondGroup.Name);

            Assert.AreEqual(originalGroup.Tags.Count, secondGroup.Tags.Count);
        }

    }
}
