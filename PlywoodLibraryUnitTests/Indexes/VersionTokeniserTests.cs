using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Plywood.Indexes;

namespace Plywood.Tests.UnitTesting.Indexes
{
    [TestClass]
    public class VersionTokeniserTests
    {
        [TestMethod]
        public void TokeniseVersion1()
        {
            var version = "1";
            var expected = new List<string>()
            {
                "1",
            };
            var actual = (new VersionTokeniser()).Tokenise(version);
            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }

        [TestMethod]
        public void TokeniseVersion2()
        {
            var version = "1.0";
            var expected = new List<string>()
            {
                "1",
                "1.0",
            };
            var actual = (new VersionTokeniser()).Tokenise(version);
            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }

        [TestMethod]
        public void TokeniseVersion3()
        {
            var version = "1.23.4";
            var expected = new List<string>()
            {
                "1",
                "1.2",
                "1.23",
                "1.23.4",
            };
            var actual = (new VersionTokeniser()).Tokenise(version);
            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }
    }
}
