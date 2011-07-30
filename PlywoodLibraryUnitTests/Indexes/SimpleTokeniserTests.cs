using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Plywood.Indexes;

namespace Plywood.Tests.UnitTesting.Indexes
{
    [TestClass]
    public class SimpleTokeniserTests
    {
        [TestMethod]
        public void SingleWordSimpleTokenisation()
        {
            var input = "test";
            var expected = new List<string>()
            {
                "test",
            };
            var tokeniser = new SimpleTokeniser();
            var actual = tokeniser.Tokenise(input).ToList();

            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
        
        [TestMethod]
        public void MixedCaseWordSimpleTokenisation()
        {
            var input = "Test";
            var expected = new List<string>()
            {
                "test",
            };
            var tokeniser = new SimpleTokeniser();
            var actual = tokeniser.Tokenise(input).ToList();

            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void TwoWordSimpleTokenisation()
        {
            var input = "Two Words";
            var expected = new List<string>()
            {
                "two",
                "words"
            };
            var tokeniser = new SimpleTokeniser();
            var actual = tokeniser.Tokenise(input).ToList();

            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void SingleLetterSimpleTokenisation()
        {
            var input = "a";
            var expected = new List<string>()
            {
                "a",
            };
            var tokeniser = new SimpleTokeniser();
            var actual = tokeniser.Tokenise(input).ToList();

            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void TwoSingleLetterSimpleTokenisation()
        {
            var input = "a b";
            var expected = new List<string>()
            {
                "a",
                "b",
            };
            var tokeniser = new SimpleTokeniser();
            var actual = tokeniser.Tokenise(input).ToList();

            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void SentanceSimpleTokenisation()
        {
            var input = "This is a test sentance.";
            var expected = new List<string>()
            {
                "this",
                "is",
                "a",
                "test",
                "sentance",
            };
            var tokeniser = new SimpleTokeniser();
            var actual = tokeniser.Tokenise(input).ToList();

            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void TwoSentancesSimpleTokenisation()
        {
            var input = "This is a test sentance. This is also a test sentance.";
            var expected = new List<string>()
            {
                "this",
                "is",
                "a",
                "test",
                "sentance",
                "also",
            };
            var tokeniser = new SimpleTokeniser();
            var actual = tokeniser.Tokenise(input).ToList();

            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void VersionNumberSimpleTokenisation()
        {
            var input = "1.0.1 Alpha Test";
            var expected = new List<string>()
            {
                "1.0.1",
                "alpha",
                "test"
            };
            var tokeniser = new SimpleTokeniser();
            var actual = tokeniser.Tokenise(input).ToList();

            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}
