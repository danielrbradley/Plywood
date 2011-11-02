using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.UnitTesting.EntityIndexing
{
    [TestClass]
    public class TargetApps
    {
        [TestMethod]
        public void GetTargetAppIndexPathsTestBasic()
        {
            var app = new RolePackage()
            {
                RoleKey = new Guid("8d4abbb4-af9f-432f-bce5-e6da5a402469"),
                RoleName = "Test Target",
                PackageKey = new Guid("9a28d7be-d5b2-11e0-95ba-0c204924019b"),
                PackageName = "Test App",
                PackageDeploymentDirectory = "TestApp",
            };

            var expected = new List<string>()
            {
                "r/8d4abbb4af9f432fbce5e6da5a402469/pi/e/testapp-9a28d7bed5b211e095ba0c204924019b-Test%20App-TestApp",
                "r/8d4abbb4af9f432fbce5e6da5a402469/pi/t/098f6bcd4621d373cade4e832627b4f6/testapp-9a28d7bed5b211e095ba0c204924019b-Test%20App-TestApp",
                "r/8d4abbb4af9f432fbce5e6da5a402469/pi/t/d2a57dc1d883fd21fb9951699df71cc7/testapp-9a28d7bed5b211e095ba0c204924019b-Test%20App-TestApp",
                "p/9a28d7bed5b211e095ba0c204924019b/ri/e/testtarget-8d4abbb4af9f432fbce5e6da5a402469-Test%20Target",
                "p/9a28d7bed5b211e095ba0c204924019b/ri/t/098f6bcd4621d373cade4e832627b4f6/testtarget-8d4abbb4af9f432fbce5e6da5a402469-Test%20Target",
                "p/9a28d7bed5b211e095ba0c204924019b/ri/t/42aefbae01d2dfd981f7da7d823d689e/testtarget-8d4abbb4af9f432fbce5e6da5a402469-Test%20Target",
            };
            var actual = app.GetIndexEntries();

            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }
    }
}
