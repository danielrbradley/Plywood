using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.UnitTesting.EntityIndexing
{
    [TestClass]
    public class Packages
    {
        [TestMethod]
        public void GetPackageIndexPathsTestBasic()
        {
            var package = new Package()
            {
                Key = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                GroupKey = new Guid("8d4abbb4-af9f-432f-bce5-e6da5a402469"),
                Name= "Test App",
                MajorVersion = "1.2.3",
                Revision = 4,
            };

            var expected = new List<string>()
            {
                "pi/e/testapp-9a28d7bed5b211e095ba0c204924019b-Test%20App-1%2E2%2E3",
                "pi/t/098f6bcd4621d373cade4e832627b4f6/testapp-9a28d7bed5b211e095ba0c204924019b-Test%20App-1%2E2%2E3",
                "pi/t/d2a57dc1d883fd21fb9951699df71cc7/testapp-9a28d7bed5b211e095ba0c204924019b-Test%20App-1%2E2%2E3",
                "g/8d4abbb4af9f432fbce5e6da5a402469/pi/e/testapp-9a28d7bed5b211e095ba0c204924019b-Test%20App-1%2E2%2E3",
                "g/8d4abbb4af9f432fbce5e6da5a402469/pi/t/098f6bcd4621d373cade4e832627b4f6/testapp-9a28d7bed5b211e095ba0c204924019b-Test%20App-1%2E2%2E3",
                "g/8d4abbb4af9f432fbce5e6da5a402469/pi/t/d2a57dc1d883fd21fb9951699df71cc7/testapp-9a28d7bed5b211e095ba0c204924019b-Test%20App-1%2E2%2E3",
            };
            var actual = package.GetIndexEntries();

            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }

        [TestMethod]
        public void PackageIndexSerialiseListItemDeserialiseTest()
        {
            var package = new Package()
            {
                Key = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                GroupKey = new Guid("8d4abbb4-af9f-432f-bce5-e6da5a402469"),
                Name = "Test App",
                MajorVersion = "1.2.3",
                Revision = 4,
            };

            var expected = new PackageListItem()
            {
                Key = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                Name = "Test App",
                MajorVersion = "1.2.3",
            };

            var actual = new PackageListItem(package.GetIndexEntries().First());

            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.MajorVersion, actual.MajorVersion);
        }
    }
}
