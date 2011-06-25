using Plywood.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Plywood.Tests.UnitTesting
{
    
    
    /// <summary>
    ///This is a test class for ValidationTest and is intended
    ///to contain all ValidationTest Unit Tests
    ///</summary>
    [TestClass()]
    public class DirectoryValidationTest
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
        public void EmptyDirectoryName()
        {
            string name = string.Empty;
            bool expected = false;
            bool actual;
            actual = Validation.IsDirectoryNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void NullDirectoryName()
        {
            string name = null;
            bool expected = false;
            bool actual;
            actual = Validation.IsDirectoryNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CarageReturnDirectoryName()
        {
            string name = "\r";
            bool expected = false;
            bool actual;
            actual = Validation.IsDirectoryNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void NewLineDirectoryName()
        {
            string name = "\n";
            bool expected = false;
            bool actual;
            actual = Validation.IsDirectoryNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void ReservedCharacterDirectoryName()
        {
            string name = ">";
            bool expected = false;
            bool actual;
            actual = Validation.IsDirectoryNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void SingleLowercaseWordDirectoryName()
        {
            string name = "test";
            bool expected = true;
            bool actual;
            actual = Validation.IsDirectoryNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void SingleUppercaseWordDirectoryName()
        {
            string name = "TEST";
            bool expected = true;
            bool actual;
            actual = Validation.IsDirectoryNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void SingleNumberWordDirectoryName()
        {
            string name = "123";
            bool expected = true;
            bool actual;
            actual = Validation.IsDirectoryNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void MultiWordDirectoryName()
        {
            string name = "123 TEST directory";
            bool expected = true;
            bool actual;
            actual = Validation.IsDirectoryNameValid(name);
            Assert.AreEqual(expected, actual);
        }
    }
}
