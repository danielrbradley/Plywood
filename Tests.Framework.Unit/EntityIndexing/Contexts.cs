using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.Framework.Unit.EntityIndexing
{
    [TestClass]
    public class Contexts
    {
        [TestMethod]
        public void GetContextIndexPathsTestBasic()
        {
            var context = new Context()
            {
                Name= "A.Test.Context",
            };

            var expected = new List<string>()
            {
                "ci/e/atestcontext-A%2ETest%2EContext",
                "ci/t/0420bbe411bdc409ce540f49c931f28c/atestcontext-A%2ETest%2EContext",
                "c/6a192c30968d1dc66fc79038ef31adea/ci/e/atestcontext-A%2ETest%2EContext",
                "c/6a192c30968d1dc66fc79038ef31adea/ci/t/0420bbe411bdc409ce540f49c931f28c/atestcontext-A%2ETest%2EContext",
                "c/7062c57fa7e7a80f1a5935b72eacbe29/ci/e/atestcontext-A%2ETest%2EContext",
                "c/7062c57fa7e7a80f1a5935b72eacbe29/ci/t/0420bbe411bdc409ce540f49c931f28c/atestcontext-A%2ETest%2EContext",
            };
            var actual = context.GetIndexEntries().ToList();
            // TODO: Only evaluate if result contains at least the specified items to cover changes to the tokenisation.
            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }
    }
}
