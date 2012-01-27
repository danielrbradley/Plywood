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
    public class LogEntryTests
    {
        public LogEntryTests()
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
        public void LogEntrySerialiseDeserialise()
        {
            var originalLogEntry = new LogEntry()
            {
                Timestamp = DateTime.UtcNow,
                Status = LogStatus.Fatal,
                GroupKey = Guid.NewGuid(),
                RoleKey = Guid.NewGuid(),
                ServerKey = Guid.NewGuid(),
                LogContent = "Test Log Entry",
            };

            LogEntry secondLogEntry;
            using (var stream = originalLogEntry.Serialise())
            {
                secondLogEntry = new LogEntry(stream);
            }

            Assert.AreEqual(originalLogEntry.Key, secondLogEntry.Key);
            Assert.AreEqual(originalLogEntry.Timestamp, secondLogEntry.Timestamp);
            Assert.AreEqual(originalLogEntry.Status, secondLogEntry.Status);
            Assert.AreEqual(originalLogEntry.GroupKey, secondLogEntry.GroupKey);
            Assert.AreEqual(originalLogEntry.RoleKey, secondLogEntry.RoleKey);
            Assert.AreEqual(originalLogEntry.ServerKey, secondLogEntry.ServerKey);
            Assert.AreEqual(originalLogEntry.LogContent, secondLogEntry.LogContent);
        }
    }
}
