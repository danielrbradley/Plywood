using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.Framework.Unit
{
    [TestClass]
    public class CustomAssertTests
    {
        [TestMethod]
        public void AreCollectionsEqualEmpty()
        {
            var actual = new List<int>()
            {
            };
            var expected = new List<int>()
            {
            };
            CustomAsserts.AreCollectionsEqual<IEnumerable<int>, int>(expected, actual);
        }

        [TestMethod]
        public void AreCollectionsEqualSingleSame()
        {
            var actual = new List<int>()
            {
                1
            };
            var expected = new List<int>()
            {
                1
            };
            CustomAsserts.AreCollectionsEqual<IEnumerable<int>, int>(expected, actual);
        }

        [TestMethod]
        public void AreCollectionsEqualDoubleInOrderSame()
        {
            var actual = new List<int>()
            {
                1, 2
            };
            var expected = new List<int>()
            {
                1, 2
            };
            CustomAsserts.AreCollectionsEqual<IEnumerable<int>, int>(expected, actual);
        }

        [TestMethod]
        public void AreCollectionsEqualDoubleNotOrderedSame()
        {
            var actual = new List<int>()
            {
                1, 2
            };
            var expected = new List<int>()
            {
                2, 1
            };
            CustomAsserts.AreCollectionsEqual<IEnumerable<int>, int>(expected, actual);
        }

        [TestMethod]
        public void AreCollectionsEqualSameLegnthDifferent()
        {
            var actual = new List<int>()
            {
                1, 2
            };
            var expected = new List<int>()
            {
                2, 3
            };
            try
            {
                CustomAsserts.AreCollectionsEqual<IEnumerable<int>, int>(expected, actual);
                Assert.Fail();
            }
            catch (AssertFailedException)
            {
            }
        }

        [TestMethod]
        public void AreCollectionsEqual1Missing()
        {
            var actual = new List<int>()
            {
                1, 2
            };
            var expected = new List<int>()
            {
                2
            };
            try
            {
                CustomAsserts.AreCollectionsEqual<IEnumerable<int>, int>(expected, actual);
                Assert.Fail();
            }
            catch (AssertFailedException)
            {
            }
        }

        [TestMethod]
        public void AreCollectionsEqual1Extra()
        {
            var actual = new List<int>()
            {
                1
            };
            var expected = new List<int>()
            {
                1, 2
            };
            try
            {
                CustomAsserts.AreCollectionsEqual<IEnumerable<int>, int>(expected, actual);
                Assert.Fail();
            }
            catch (AssertFailedException)
            {
            }
        }
    }
}
