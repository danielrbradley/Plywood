using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.UnitTesting.EntityIndexing
{
    [TestClass]
    public class Servers
    {
        [TestMethod]
        public void GetServerIndexPathsTestBasic()
        {
            var server = new Server()
            {
                Key = new Guid("7dc11e0c-d5c5-11e0-ae84-6ab04724019b"),
                RoleKey = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                Name = "Test Instance",
            };

            var expected = new List<string>()
            {
                "r/9a28d7bed5b211e095ba0c204924019b/si/e/testinstance-7dc11e0cd5c511e0ae846ab04724019b-Test%20Instance",
                "r/9a28d7bed5b211e095ba0c204924019b/si/t/098f6bcd4621d373cade4e832627b4f6/testinstance-7dc11e0cd5c511e0ae846ab04724019b-Test%20Instance",
                "r/9a28d7bed5b211e095ba0c204924019b/si/t/7123a699d77db6479a1d8ece2c4f1c16/testinstance-7dc11e0cd5c511e0ae846ab04724019b-Test%20Instance",
                "si/e/testinstance-7dc11e0cd5c511e0ae846ab04724019b-Test%20Instance",
                "si/t/098f6bcd4621d373cade4e832627b4f6/testinstance-7dc11e0cd5c511e0ae846ab04724019b-Test%20Instance",
                "si/t/7123a699d77db6479a1d8ece2c4f1c16/testinstance-7dc11e0cd5c511e0ae846ab04724019b-Test%20Instance",
            };
            var actual = server.GetIndexEntries();

            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }

        [TestMethod]
        public void ServerIndexSerialiseListItemDeserialiseTest()
        {
            var server = new Server()
            {
                Key = new Guid("7dc11e0c-d5c5-11e0-ae84-6ab04724019b"),
                RoleKey = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                Name = "Test Instance",
            };

            var expected = new ServerListItem()
            {
                Key = new Guid("7dc11e0c-d5c5-11e0-ae84-6ab04724019b"),
                Name = "Test Instance",
            };

            var actual = new ServerListItem(server.GetIndexEntries().First());

            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.Name, actual.Name);
        }
    }
}
