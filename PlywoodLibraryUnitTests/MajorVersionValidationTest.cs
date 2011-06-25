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
    public class MajorVersionValidationTest
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
        public void NullString()
        {
            string majorVersion = null;
            bool expected = false;
            bool actual;
            actual = Validation.IsMajorVersionValid(majorVersion);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void BlankString()
        {
            string majorVersion = "";
            bool expected = false;
            bool actual;
            actual = Validation.IsMajorVersionValid(majorVersion);
            Assert.AreEqual(expected, actual);
        }
        
        [TestMethod()]
        public void SingleDigit()
        {
            string majorVersion = "1";
            bool expected = true;
            bool actual;
            actual = Validation.IsMajorVersionValid(majorVersion);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void TwoDigits()
        {
            string majorVersion = "1";
            bool expected = true;
            bool actual;
            actual = Validation.IsMajorVersionValid(majorVersion);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void TwoParts()
        {
            string majorVersion = "1.0";
            bool expected = true;
            bool actual;
            actual = Validation.IsMajorVersionValid(majorVersion);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void ThreeParts()
        {
            string majorVersion = "1.0.0";
            bool expected = true;
            bool actual;
            actual = Validation.IsMajorVersionValid(majorVersion);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void TwoDoubleDigits()
        {
            string majorVersion = "10.20";
            bool expected = true;
            bool actual;
            actual = Validation.IsMajorVersionValid(majorVersion);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void LeadingDot()
        {
            string majorVersion = ".10.20";
            bool expected = false;
            bool actual;
            actual = Validation.IsMajorVersionValid(majorVersion);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void TrailingDot()
        {
            string majorVersion = "10.20.";
            bool expected = false;
            bool actual;
            actual = Validation.IsMajorVersionValid(majorVersion);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void JustADot()
        {
            string majorVersion = ".";
            bool expected = false;
            bool actual;
            actual = Validation.IsMajorVersionValid(majorVersion);
            Assert.AreEqual(expected, actual);
        }

    }
}
