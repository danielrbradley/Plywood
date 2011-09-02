using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.UnitTesting
{
    public static class CustomAsserts
    {
        /// <summary>
        /// Compage two enumerable collections and ensure both collections have the same elements
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        public static void AreCollectionsEqual<TCollection, TItem>(TCollection expected, TCollection actual) where TCollection : IEnumerable<TItem>
        {
            if (expected.Any(e => !actual.Any(a => AreEqual(e, a))))
                Assert.Fail("Collection is missing expected elements.");
            if (actual.Any(a => !expected.Any(e => AreEqual(e, a))))
                Assert.Fail("Collection is has additional unexpected elements.");
        }

        private static bool AreEqual<TItem>(TItem e, TItem a)
        {
            return (e == null && a != null) || (a == null && e != null) || (a != null && a.Equals(e));
        }
    }
}
