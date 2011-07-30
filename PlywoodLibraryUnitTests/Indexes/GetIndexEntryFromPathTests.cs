using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Plywood.Indexes;

namespace Plywood.Tests.UnitTesting.Indexes
{
    [TestClass]
    public class GetIndexEntryFromPathTests
    {
        [TestMethod]
        public void GetIndexEntryFromValidPath()
        {
            var path = "/a/cbcb3ea1251048b8b368d12db433bf9b/vi/t/3accddf64b1dd03abeb9b0b3e5a7ba44/1a0a1-56ed6820c39049f09760802aea44905f-1%2E0%2E1%20Alpha%20Release";
            var expected = new IndexEntry()
            {
                EntryKey = new Guid("56ed6820c39049f09760802aea44905f"),
                SortHash = "1a0a1",
                EntryText = "1.0.1 Alpha Release",
            };
            var actual = IndexEntries.GetIndexEntryFromPath(path);

            Assert.AreEqual(expected.SortHash, actual.SortHash);
            Assert.AreEqual(expected.EntryKey, actual.EntryKey);
            Assert.AreEqual(expected.EntryText, actual.EntryText);
        }
    }
}
