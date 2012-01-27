using Plywood.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Plywood.Tests.Framework.Unit.Indexes
{
    
    
    /// <summary>
    ///This is a test class for IndexesTest and is intended
    ///to contain all IndexesTest Unit Tests
    ///</summary>
    [TestClass()]
    public class StringHashing
    {

        [TestMethod()]
        public void CreateEmptyStringHashLastTest()
        {
            string content = string.Empty;
            bool reversed = false;
            bool ignoreCase = false;
            bool sortEmptyLast = true;
            string expected = Plywood.Indexes.Hashing.LAST_HASH_STRING;
            string actual;
            actual = Plywood.Indexes.Hashing.CreateHash(content, reversed, ignoreCase, sortEmptyLast);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CreateEmptyStringHashFirstTest()
        {
            string content = string.Empty;
            bool reversed = false;
            bool ignoreCase = false;
            bool sortEmptyLast = false;
            string expected = string.Empty;
            string actual;
            actual = Plywood.Indexes.Hashing.CreateHash(content, reversed, ignoreCase, sortEmptyLast);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CreateStringHashForwardTest1()
        {
            string content = "0123456789ABCDEFGHIJKLMNOPQRSTU";
            bool reversed = false;
            bool ignoreCase = false;
            string expected = "0123456789ABCDEFGHIJKLMNOPQRSTU";
            string actual;
            actual = Plywood.Indexes.Hashing.CreateHash(content, reversed, ignoreCase);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CreateStringHashForwardTest2()
        {
            string content = "VWXYZabcdefghijklmnopqrstuvwxyz";
            bool reversed = false;
            bool ignoreCase = false;
            string expected = "VWXYZabcdefghijklmnopqrstuvwxyz";
            string actual;
            actual = Plywood.Indexes.Hashing.CreateHash(content, reversed, ignoreCase);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CreateStringHashBackwardTest1()
        {
            string content = "0123456789ABCDEFGHIJKLMNOPQRSTU";
            bool reversed = true;
            bool ignoreCase = false;
            string expected = "zyxwvutsrqponmlkjihgfedcbaZYXWV";
            string actual;
            actual = Plywood.Indexes.Hashing.CreateHash(content, reversed, ignoreCase);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CreateStringHashBackwardTest2()
        {
            string content = "VWXYZabcdefghijklmnopqrstuvwxyz";
            bool reversed = true;
            bool ignoreCase = false;
            string expected = "UTSRQPONMLKJIHGFEDCBA9876543210";
            string actual;
            actual = Plywood.Indexes.Hashing.CreateHash(content, reversed, ignoreCase);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CreateStringHashMessyTest()
        {
            string content = "  a    !\"£$%^&*()_+-=1{};'#:@~,./<>?|\\Z!\"£$%^&*()_+-={};'#:@~,./<>?|\\";
            bool reversed = false;
            bool ignoreCase = false;
            string expected = "a1Z";
            string actual;
            actual = Plywood.Indexes.Hashing.CreateHash(content, reversed, ignoreCase);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CreateSingleCharacterStringHashTest()
        {
            string content = "a";
            bool reversed = false;
            bool ignoreCase = false;
            string expected = "a";
            string actual;
            actual = Plywood.Indexes.Hashing.CreateHash(content, reversed, ignoreCase);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CreateLowerCaseStringHashTest1()
        {
            string content = "0123456789ABCDEFGHIJKLMNOPQRSTU";
            bool reversed = false;
            bool ignoreCase = true;
            string expected = "0123456789abcdefghijklmnopqrstu";
            string actual;
            actual = Plywood.Indexes.Hashing.CreateHash(content, reversed, ignoreCase);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void CreateLowerCaseStringHashTest2()
        {
            string content = "VWXYZabcdefghijklmnopqrstuvwxyz";
            bool reversed = false;
            bool ignoreCase = true;
            string expected = "vwxyzabcdefghijklmnopqrstuvwxyz";
            string actual;
            actual = Plywood.Indexes.Hashing.CreateHash(content, reversed, ignoreCase);
            Assert.AreEqual(expected, actual);
        }
    }
}
