using Plywood;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Plywood.Utils;

namespace Plywood.Tests.UnitTesting
{


    /// <summary>
    ///This is a test class for EntitySerializationTest and is intended
    ///to contain all EntitySerializationTest Unit Tests
    ///</summary>
    [TestClass()]
    public class KeyValuePairDeSerialiseTest
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
        public void NoProperties()
        {
            TextReader source = new StringReader("");
            Dictionary<string, string> expected = new Dictionary<string, string>();
            Dictionary<string, string> actual;
            actual = Serialisation.ReadProperties(source);
            AssertDictionariesEqual(expected, actual);
        }

        [TestMethod()]
        [ExpectedException(typeof(DeserialisationException))]
        public void JustAValue()
        {
            TextReader source = new StringReader("\tValue");
            Dictionary<string, string> expected = new Dictionary<string, string>();
            Dictionary<string, string> actual;
            actual = Serialisation.ReadProperties(source);
            Assert.Fail();
        }

        [TestMethod()]
        public void SingleProperty()
        {
            TextReader source = new StringReader("Name\r\n\tTest");
            Dictionary<string, string> expected = new Dictionary<string, string>() { { "Name", "Test" } };
            Dictionary<string, string> actual;
            actual = Serialisation.ReadProperties(source);
            AssertDictionariesEqual(expected, actual);
        }

        [TestMethod()]
        public void TwoProperties()
        {
            TextReader source = new StringReader("Key\r\n\t828d4f6d1a0d4606aad1517a45758d01\r\nName\r\n\tTest");
            Dictionary<string, string> expected = new Dictionary<string, string>() { { "Key", "828d4f6d1a0d4606aad1517a45758d01" }, { "Name", "Test" } };
            Dictionary<string, string> actual;
            actual = Serialisation.ReadProperties(source);
            AssertDictionariesEqual(expected, actual);
        }

        [TestMethod()]
        public void SingleMultilineProperty()
        {
            TextReader source = new StringReader("Description\r\n\tSome text ...\r\n\tover \r\n\tseveral\r\n\t\tlines!!! ");
            Dictionary<string, string> expected = new Dictionary<string, string>() { { "Description", "Some text ...\r\nover \r\nseveral\r\n\tlines!!! " } };
            Dictionary<string, string> actual;
            actual = Serialisation.ReadProperties(source);
            AssertDictionariesEqual(expected, actual);
        }

        [TestMethod()]
        public void LeadingBlankLine()
        {
            TextReader source = new StringReader("\r\nName\r\n\tTest");
            Dictionary<string, string> expected = new Dictionary<string, string>() { { "Name", "Test" } };
            Dictionary<string, string> actual;
            actual = Serialisation.ReadProperties(source);
            AssertDictionariesEqual(expected, actual);
        }

        [TestMethod()]
        public void NoValue()
        {
            TextReader source = new StringReader("Key");
            Dictionary<string, string> expected = new Dictionary<string, string>() { { "Key", null } };
            Dictionary<string, string> actual;
            actual = Serialisation.ReadProperties(source);
            AssertDictionariesEqual(expected, actual);
        }

        private static void AssertDictionariesEqual(Dictionary<string, string> expected, Dictionary<string, string> actual)
        {
            if (expected == null)
            {
                Assert.IsNull(actual);
            }
            foreach (var key in expected.Keys)
            {
                Assert.IsTrue(actual.ContainsKey(key), "Results does not contain the expected key \"{0}\"", key);
                Assert.AreEqual(expected[key], actual[key]);
            }
            Assert.IsFalse(actual.Keys.Any(ak => !expected.Keys.Contains(ak)), "Results contans extra keys.");
        }
    }
}
