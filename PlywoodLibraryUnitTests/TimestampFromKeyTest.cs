using Plywood;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Plywood.Tests.UnitTesting
{
    
    
    /// <summary>
    ///This is a test class for LogsTest and is intended
    ///to contain all LogsTest Unit Tests
    ///</summary>
    [TestClass()]
    public class TimestampFromKeyTest
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
        public void TryGetTimestampFromKeyTest()
        {
            DateTime timestamp;
            DateTime timestampExpected = DateTime.UtcNow;
            bool expected = true;
            bool actual;
            string key = string.Format("logs/{0}/{1}", Guid.NewGuid().ToString("N"), Plywood.Utils.Serialisation.SerialiseDateReversed(timestampExpected));

            actual = Logs.TryGetTimestampFromKey(key, out timestamp);
            Assert.AreEqual(timestampExpected, timestamp);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void TryGetTimestampFromKeyTest_MissingChar()
        {
            DateTime timestamp;
            DateTime timestampExpected = DateTime.MinValue;
            bool expected = false;
            bool actual;
            string key = string.Format("logs/{0}/000000000000000", Guid.NewGuid().ToString("N"), Plywood.Utils.Serialisation.SerialiseDateReversed(DateTime.UtcNow));

            actual = Logs.TryGetTimestampFromKey(key, out timestamp);
            Assert.AreEqual(timestampExpected, timestamp);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void TryGetTimestampFromKeyTest_WrongChar()
        {
            DateTime timestamp;
            DateTime timestampExpected = DateTime.MinValue;
            bool expected = false;
            bool actual;
            string key = string.Format("logs/{0}/0123456789ABCDEG", Guid.NewGuid().ToString("N"), Plywood.Utils.Serialisation.SerialiseDateReversed(DateTime.UtcNow));

            actual = Logs.TryGetTimestampFromKey(key, out timestamp);
            Assert.AreEqual(timestampExpected, timestamp);
            Assert.AreEqual(expected, actual);
        }

    }
}
