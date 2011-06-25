using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.UnitTesting
{
    [TestClass]
    public class ReverseOrderDateSerialiseationTest
    {
        [TestMethod]
        public void StandardReverseOrderDateSerialiseDeserialise()
        {
            var date = DateTime.UtcNow;
            var encoded = Plywood.Utils.Serialisation.SerialiseDateReversed(date);
            var decoded = Plywood.Utils.Serialisation.DeserialiseReversedDate(encoded);
            Assert.AreEqual(date, decoded);
        }

        [TestMethod]
        public void ReverseOrderDateSerialiseOrdering()
        {
            var date = DateTime.UtcNow;
            var olderDate = date.AddSeconds(-1);
            var encodedDate = Plywood.Utils.Serialisation.SerialiseDateReversed(date);
            var encodedOlderDate = Plywood.Utils.Serialisation.SerialiseDateReversed(olderDate);
            Assert.IsTrue(string.Compare(encodedDate, encodedOlderDate) < 0);
        }

    }
}
