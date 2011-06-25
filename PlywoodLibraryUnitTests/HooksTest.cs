using Plywood.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Plywood.Tests.UnitTesting
{


    /// <summary>
    ///This is a test class for HooksTest and is intended
    ///to contain all HooksTest Unit Tests
    ///</summary>
    [TestClass()]
    public class HooksTest
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

        private bool CompareHookLists(List<Hook> expected, List<Hook> actual)
        {
            return
                expected == actual ||
                (expected.Count == actual.Count &&
                expected.TrueForAll(e => CompareHooks(e, actual[expected.IndexOf(e)])));
        }

        private bool CompareHooks(Hook expected, Hook actual)
        {
            return
                expected == actual ||
                (expected.Command == actual.Command &&
                expected.Arguments == actual.Arguments);
        }

        [TestMethod()]
        public void SimpleCommand()
        {
            string source = "Test";
            List<Hook> expected = new List<Hook>() { new Hook() { Command = "Test" } };
            List<Hook> actual;
            actual = Hooks.ParseHooks(source);
            Assert.IsTrue(CompareHookLists(expected, actual));
        }

        [TestMethod()]
        public void SimpleCommandArgument()
        {
            string source = "Test --arg";
            List<Hook> expected = new List<Hook>() { new Hook() { Command = "Test", Arguments = "--arg" } };
            List<Hook> actual;
            actual = Hooks.ParseHooks(source);
            Assert.IsTrue(CompareHookLists(expected, actual));
        }

        [TestMethod()]
        public void SimpleCommandArguments()
        {
            string source = "Test --foo bar";
            List<Hook> expected = new List<Hook>() { new Hook() { Command = "Test", Arguments = "--foo bar" } };
            List<Hook> actual;
            actual = Hooks.ParseHooks(source);
            Assert.IsTrue(CompareHookLists(expected, actual));
        }

        [TestMethod()]
        public void SimpleQuotedCommand()
        {
            string source = "\"Test Command\"";
            List<Hook> expected = new List<Hook>() { new Hook() { Command = "Test Command" } };
            List<Hook> actual;
            actual = Hooks.ParseHooks(source);
            Assert.IsTrue(CompareHookLists(expected, actual));
        }

        [TestMethod()]
        public void SimpleQuotedCommandArgument()
        {
            string source = "\"Test Command\" --arg";
            List<Hook> expected = new List<Hook>() { new Hook() { Command = "Test Command", Arguments = "--arg" } };
            List<Hook> actual;
            actual = Hooks.ParseHooks(source);
            Assert.IsTrue(CompareHookLists(expected, actual));
        }

        [TestMethod()]
        public void SimpleQuotedCommandArguments()
        {
            string source = "\"Test Command\" --foo bar";
            List<Hook> expected = new List<Hook>() { new Hook() { Command = "Test Command", Arguments = "--foo bar" } };
            List<Hook> actual;
            actual = Hooks.ParseHooks(source);
            Assert.IsTrue(CompareHookLists(expected, actual));
        }

        [TestMethod()]
        [ExpectedException(typeof(HooksParserException))]
        public void MissingQuote()
        {
            string source = "\"Test --arg";
            var res = Hooks.ParseHooks(source);
        }

        [TestMethod()]
        [ExpectedException(typeof(HooksParserException))]
        public void NoSpace()
        {
            string source = "\"Test\"--arg";
            var res = Hooks.ParseHooks(source);
        }

    }
}
