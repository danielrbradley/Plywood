using Plywood;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Plywood.Utils;

namespace Plywood.Tests.UnitTesting
{
    /// <summary>
    ///This is a test class for ValidationTest and is intended
    ///to contain all ValidationTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ValidationNameTest
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
        public void SingleWord()
        {
            string name = "Test";
            bool expected = true;
            bool actual;
            actual = Validation.IsNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void TwoWords()
        {
            string name = "Test Name";
            bool expected = true;
            bool actual;
            actual = Validation.IsNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void BlankName()
        {
            string name = string.Empty;
            bool expected = false;
            bool actual;
            actual = Validation.IsNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void NullName()
        {
            string name = null;
            bool expected = false;
            bool actual;
            actual = Validation.IsNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void WhitespaceSpaces()
        {
            string name = "    ";
            bool expected = false;
            bool actual;
            actual = Validation.IsNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void WhitespaceTabs()
        {
            string name = "\t\t";
            bool expected = false;
            bool actual;
            actual = Validation.IsNameValid(name);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void TwoLines()
        {
            string name = "Multiline\r\nTest";
            bool expected = false;
            bool actual;
            actual = Validation.IsNameValid(name);
            Assert.AreEqual(expected, actual);
        }
    }
}
