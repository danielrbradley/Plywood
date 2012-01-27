using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.Framework.Unit.EntityIndexing
{
    [TestClass]
    public class Groups
    {
        [TestMethod]
        public void GetGroupIndexPathsTestBasic()
        {
            var group = new Group()
            {
                Key = new Guid("8d4abbb4-af9f-432f-bce5-e6da5a402469"),
                Name= "Test Group",
            };

            var expected = new List<string>()
            {
                "gi/e/testgroup-8d4abbb4af9f432fbce5e6da5a402469-Test%20Group",
                "gi/t/098f6bcd4621d373cade4e832627b4f6/testgroup-8d4abbb4af9f432fbce5e6da5a402469-Test%20Group",
                "gi/t/db0f6f37ebeb6ea09489124345af2a45/testgroup-8d4abbb4af9f432fbce5e6da5a402469-Test%20Group",
            };
            var actual = group.GetIndexEntries();

            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }
    }
}
