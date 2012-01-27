using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.Framework.Unit.Indexes
{
    [TestClass]
    public class TokenHashTests
    {
        [TestMethod]
        public void SimpleTokenHashTest()
        {
            var token = "test";
            var expected = "098f6bcd4621d373cade4e832627b4f6";
            var actual = Plywood.Indexes.IndexEntries.GetTokenHash(token);
            Assert.AreEqual(expected, actual);
        }
    }
}
