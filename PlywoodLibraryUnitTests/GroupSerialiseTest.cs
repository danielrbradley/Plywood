using Plywood;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using Plywood.Utils;
using System.IO;

namespace Plywood.Tests.UnitTesting
{


    /// <summary>
    ///This is a test class for EntitySerialisationTest and is intended
    ///to contain all EntitySerialisationTest Unit Tests
    ///</summary>
    [TestClass()]
    public class GroupSerialiseTest
    {


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
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        [TestMethod()]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullGroup()
        {
            Group group = null;
            string actual;
            actual = ReadStream(group.Serialise());
            Assert.Fail("Should not reach this point.");
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void KeyTag()
        {
            Group group = new Group()
            {
                Key = new Guid("828d4f6d1a0d4606aad1517a45758d01"),
                Name = "Test",
                Tags = new Dictionary<string, string>()
                {
                    { "Key", "828d4f6d1a0d4606aad1517a45758d01" }
                }
            };
            string actual;
            actual = ReadStream(group.Serialise());
            Assert.Fail("Should not reach this point.");
        }

        [TestMethod()]
        [ExpectedException(typeof(ArgumentException))]
        public void NameTag()
        {
            Group group = new Group()
            {
                Key = new Guid("828d4f6d1a0d4606aad1517a45758d01"),
                Name = "Test",
                Tags = new Dictionary<string, string>()
                {
                    { "Name", "Test" }
                }
            };
            string actual;
            actual = ReadStream(group.Serialise());
            Assert.Fail("Should not reach this point.");
        }

        [TestMethod()]
        public void SimpleGroupNullTags()
        {
            Group group = new Group()
            {
                Key = new Guid("828d4f6d1a0d4606aad1517a45758d01"),
                Name = "Test",
            };
            string expected = "Key\r\n\t828d4f6d1a0d4606aad1517a45758d01\r\nName\r\n\tTest";
            string actual;
            actual = ReadStream(group.Serialise());
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void SimpleGroupNoTags()
        {
            Group group = new Group()
            {
                Key = new Guid("828d4f6d1a0d4606aad1517a45758d01"),
                Name = "Test",
                Tags = new Dictionary<string,string>()
            };
            string expected = "Key\r\n\t828d4f6d1a0d4606aad1517a45758d01\r\nName\r\n\tTest";
            string actual;
            actual = ReadStream(group.Serialise());
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void SingleTag()
        {
            Group group = new Group()
            {
                Key = new Guid("828d4f6d1a0d4606aad1517a45758d01"),
                Name = "Test",
                Tags = new Dictionary<string, string>()
                {
                    { "Tag", "Label" }
                }
            };
            string expected = "Key\r\n\t828d4f6d1a0d4606aad1517a45758d01\r\nName\r\n\tTest\r\nTag\r\n\tLabel";
            string actual;
            actual = ReadStream(group.Serialise());
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void MultiTags()
        {
            Group group = new Group()
            {
                Key = new Guid("828d4f6d1a0d4606aad1517a45758d01"),
                Name = "Test",
                Tags = new Dictionary<string, string>()
                {
                    { "Tag1", "A" },
                    { "Tag2", "B" },
                }
            };
            string expected = "Key\r\n\t828d4f6d1a0d4606aad1517a45758d01\r\nName\r\n\tTest\r\nTag1\r\n\tA\r\nTag2\r\n\tB";
            string actual;
            actual = ReadStream(group.Serialise());
            Assert.AreEqual(expected, actual);
        }

        private static string ReadStream(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
