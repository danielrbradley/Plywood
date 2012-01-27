using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Plywood.Indexes;

namespace Plywood.Tests.Framework.Unit.Indexes
{
    [TestClass]
    public class VersionHashing
    {
        [TestMethod]
        public void ForwardVersionHashTest()
        {
            var versions = new List<string>()
            {
                "1.0",
                "16.0",
                "2.0",
                "10.0",
                "1.1",
            };
            var expected = new List<string>()
            {
                "00000001_00000000",
                "00000001_00000001",
                "00000002_00000000",
                "0000000A_00000000",
                "00000010_00000000",
            };
            var actual = versions.Select(v => Hashing.CreateVersionHash(v, false)).OrderBy(h => h).ToList();

            Assert.AreEqual(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count(); i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }

        [TestMethod]
        public void BackwardVersionHashTest()
        {
            var versions = new List<string>()
            {
                "1.0",
                "16.0",
                "2.0",
                "10.0",
                "1.1",
            };
            var expected = new List<string>()
            {
                "FFFFFFEF_FFFFFFFF",
                "FFFFFFF5_FFFFFFFF",
                "FFFFFFFD_FFFFFFFF",
                "FFFFFFFE_FFFFFFFE",
                "FFFFFFFE_FFFFFFFF",
            };
            var actual = versions.Select(v => Hashing.CreateVersionHash(v)).OrderBy(h => h).ToList();

            Assert.AreEqual(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count(); i++)
            {
                Assert.AreEqual(expected[i], actual[i]);
            }
        }
    }
}
