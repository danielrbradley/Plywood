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
    public class TargetTests
    {
        public TargetTests()
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
        public void TargetSerialiseDeserialise()
        {
            var originalTarget = new Target()
            {
                Key = Guid.NewGuid(),
                Name = "Test Target",
                GroupKey = Guid.NewGuid(),
                Tags = new Dictionary<string, string>()
                {
                    { "tagKey", "Some tag value." },
                    { "secondKey", "Multiline \r\n   test!" }
                }
            };

            Target secondTarget;
            using (var stream = originalTarget.Serialise())
            {
                secondTarget = new Target(stream);
            }

            Assert.AreEqual(originalTarget.Key, secondTarget.Key);
            Assert.AreEqual(originalTarget.Name, secondTarget.Name);
            Assert.AreEqual(originalTarget.GroupKey, secondTarget.GroupKey);

            Assert.IsNotNull(secondTarget.Tags);
            foreach (var tag in originalTarget.Tags)
            {
                Assert.IsTrue(secondTarget.Tags.ContainsKey(tag.Key));
                Assert.AreEqual(tag.Value, secondTarget.Tags[tag.Key]);
            }
        }

        [TestMethod]
        public void TargetSerialiseDeserialiseNullTags()
        {
            var originalTarget = new Target()
            {
                Key = Guid.NewGuid(),
                Name = "Test Target",
                GroupKey = Guid.NewGuid(),
            };

            Target secondTarget;
            using (var stream = originalTarget.Serialise())
            {
                secondTarget = new Target(stream);
            }

            Assert.AreEqual(originalTarget.Key, secondTarget.Key);
            Assert.AreEqual(originalTarget.Name, secondTarget.Name);
            Assert.AreEqual(originalTarget.GroupKey, secondTarget.GroupKey);

            Assert.AreEqual(originalTarget.Tags.Count, secondTarget.Tags.Count);
        }

    }
}
