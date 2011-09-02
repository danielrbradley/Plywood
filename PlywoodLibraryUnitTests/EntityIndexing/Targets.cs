using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.UnitTesting.EntityIndexing
{
    [TestClass]
    public class Targets
    {
        [TestMethod]
        public void GetTargetIndexPathsTestBasic()
        {
            var target = new Target()
            {
                Key = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                GroupKey = new Guid("8d4abbb4-af9f-432f-bce5-e6da5a402469"),
                Name= "Test Target",
            };

            var expected = new List<string>()
            {
                "ti/e/testtarget-9a28d7bed5b211e095ba0c204924019b-8d4abbb4af9f432fbce5e6da5a402469-Test%20Target",
                "ti/t/098f6bcd4621d373cade4e832627b4f6/testtarget-9a28d7bed5b211e095ba0c204924019b-8d4abbb4af9f432fbce5e6da5a402469-Test%20Target",
                "ti/t/42aefbae01d2dfd981f7da7d823d689e/testtarget-9a28d7bed5b211e095ba0c204924019b-8d4abbb4af9f432fbce5e6da5a402469-Test%20Target",
                "g/8d4abbb4af9f432fbce5e6da5a402469/ti/e/testtarget-9a28d7bed5b211e095ba0c204924019b-8d4abbb4af9f432fbce5e6da5a402469-Test%20Target",
                "g/8d4abbb4af9f432fbce5e6da5a402469/ti/t/098f6bcd4621d373cade4e832627b4f6/testtarget-9a28d7bed5b211e095ba0c204924019b-8d4abbb4af9f432fbce5e6da5a402469-Test%20Target",
                "g/8d4abbb4af9f432fbce5e6da5a402469/ti/t/42aefbae01d2dfd981f7da7d823d689e/testtarget-9a28d7bed5b211e095ba0c204924019b-8d4abbb4af9f432fbce5e6da5a402469-Test%20Target",
            };
            var actual = target.GetIndexEntries();

            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }

        [TestMethod]
        public void TargetIndexSerialiseListItemDeserialiseTest()
        {
            var target = new Target()
            {
                Key = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                GroupKey = new Guid("8d4abbb4-af9f-432f-bce5-e6da5a402469"),
                Name = "Test Target",
            };

            var expected = new TargetListItem()
            {
                Key = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                GroupKey = new Guid("8d4abbb4-af9f-432f-bce5-e6da5a402469"),
                Name = "Test Target",
            };

            var actual = new TargetListItem(target.GetIndexEntries().First());

            Assert.AreEqual(expected.Key, actual.Key);
            Assert.AreEqual(expected.GroupKey, actual.GroupKey);
            Assert.AreEqual(expected.Name, actual.Name);
        }
    }
}
