using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Plywood.Indexes;

namespace Plywood.Tests.UnitTesting.Indexes
{
    [TestClass]
    public class GetIndexEntryPathsTests
    {
        [TestMethod]
        public void GetTestIndexEntryPaths()
        {
            var indexEntry = new IndexEntry()
            {
                BasePath = "/a/cbcb3ea1251048b8b368d12db433bf9b/vi",
                EntryKey = new Guid("56ed6820c39049f09760802aea44905f"),
                SortHash = "1a0a1",
                EntryText = "1.0.1 Alpha Release",
                Tokens = new SimpleTokeniser().Tokenise("1.0.1 Alpha Release")
            };
            var tokeniser = new SimpleTokeniser();
            var expected = new List<string>()
                {
                    "/a/cbcb3ea1251048b8b368d12db433bf9b/vi/e/1a0a1-56ed6820c39049f09760802aea44905f-1%2E0%2E1%20Alpha%20Release",
                    "/a/cbcb3ea1251048b8b368d12db433bf9b/vi/t/3accddf64b1dd03abeb9b0b3e5a7ba44/1a0a1-56ed6820c39049f09760802aea44905f-1%2E0%2E1%20Alpha%20Release",
                    "/a/cbcb3ea1251048b8b368d12db433bf9b/vi/t/2c1743a391305fbf367df8e4f069f9f9/1a0a1-56ed6820c39049f09760802aea44905f-1%2E0%2E1%20Alpha%20Release",
                    "/a/cbcb3ea1251048b8b368d12db433bf9b/vi/t/123fead50246387983ee340507115ef4/1a0a1-56ed6820c39049f09760802aea44905f-1%2E0%2E1%20Alpha%20Release",
                };
            var actual = IndexEntries.GetIndexEntryPaths(indexEntry).ToList();

            Assert.AreEqual(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                Assert.IsTrue(actual.Contains(expected[i]), "Result does not contain the result \"{0}\"", expected[i]);
            }
        }
    }
}
