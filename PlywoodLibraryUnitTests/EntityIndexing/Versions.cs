using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.UnitTesting.EntityIndexing
{
    [TestClass]
    public class Versions
    {
        [TestMethod]
        public void GetVersionIndexPathsTestBasic()
        {
            var version = new Version()
            {
                Key = new Guid("7dc11e0c-d5c5-11e0-ae84-6ab04724019b"),
                PackageKey = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                Comment = "Test Version",
                VersionNumber = "1.2.3",
                Timestamp = new DateTime(2011, 09, 03, 1, 40, 17),
            };

            var expected = new List<string>()
            {
                "p/9a28d7bed5b211e095ba0c204924019b/vi/e/FFFFFFFE_FFFFFFFD_FFFFFFFC-7dc11e0cd5c511e0ae846ab04724019b-Test%20Version-1%2E2%2E3-2011%2D09%2D03T01%3A40%3A17",
                "p/9a28d7bed5b211e095ba0c204924019b/vi/t/098f6bcd4621d373cade4e832627b4f6/FFFFFFFE_FFFFFFFD_FFFFFFFC-7dc11e0cd5c511e0ae846ab04724019b-Test%20Version-1%2E2%2E3-2011%2D09%2D03T01%3A40%3A17",
                "p/9a28d7bed5b211e095ba0c204924019b/vi/t/2af72f100c356273d46284f6fd1dfc08/FFFFFFFE_FFFFFFFD_FFFFFFFC-7dc11e0cd5c511e0ae846ab04724019b-Test%20Version-1%2E2%2E3-2011%2D09%2D03T01%3A40%3A17",
                "p/9a28d7bed5b211e095ba0c204924019b/vi/t/c4ca4238a0b923820dcc509a6f75849b/FFFFFFFE_FFFFFFFD_FFFFFFFC-7dc11e0cd5c511e0ae846ab04724019b-Test%20Version-1%2E2%2E3-2011%2D09%2D03T01%3A40%3A17",
                "p/9a28d7bed5b211e095ba0c204924019b/vi/t/56765472680401499c79732468ba4340/FFFFFFFE_FFFFFFFD_FFFFFFFC-7dc11e0cd5c511e0ae846ab04724019b-Test%20Version-1%2E2%2E3-2011%2D09%2D03T01%3A40%3A17",
                "p/9a28d7bed5b211e095ba0c204924019b/vi/t/b0e8daa258acbb6fc4c86f89e0c9183e/FFFFFFFE_FFFFFFFD_FFFFFFFC-7dc11e0cd5c511e0ae846ab04724019b-Test%20Version-1%2E2%2E3-2011%2D09%2D03T01%3A40%3A17",
            };
            var actual = version.GetIndexEntries();

            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }

        [TestMethod]
        public void VersionIndexSerialiseListItemDeserialiseTest()
        {
            var version = new Version()
            {
                Key = new Guid("7dc11e0c-d5c5-11e0-ae84-6ab04724019b"),
                PackageKey = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                Comment = "Test Version",
                VersionNumber = "1.2.3",
                Timestamp = new DateTime(2011, 09, 03, 1, 40, 17),
            };

            var expected = new VersionListItem()
            {
                Key = new Guid("7dc11e0c-d5c5-11e0-ae84-6ab04724019b"),
                Comment = "Test Version",
                VersionNumber = "1.2.3",
                Timestamp = new DateTime(2011, 09, 03, 1, 40, 17),
            };

            var actual = new VersionListItem(version.GetIndexEntries().First());

            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Comment, actual.Comment);
            Assert.AreEqual(expected.VersionNumber, actual.VersionNumber);
            Assert.AreEqual(expected.Timestamp, actual.Timestamp);
        }
    }
}
