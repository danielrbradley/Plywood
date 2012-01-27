using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.Framework.Unit.EntityIndexing
{
    [TestClass]
    public class Logs
    {
        [TestMethod]
        public void GetLogIndexPathsTestBasic()
        {
            var log = new LogEntry()
            {
                Key = new Guid("b478053b-c204-497e-9610-b6439dcf9a07"),
                Timestamp = new DateTime(2011, 09, 04, 23, 15, 23),
                ServerKey = new Guid("8d4abbb4-af9f-432f-bce5-e6da5a402469"),
                Status = LogStatus.Warning,
            };

            var expected = new List<string>()
            {
                "s/8d4abbb4af9f432fbce5e6da5a402469/li/e/F731C69057E9C87F-b-b478053bc204497e9610b6439dcf9a07",
                "s/8d4abbb4af9f432fbce5e6da5a402469/li/t/7b83d3f08fa392b79e3f553b585971cd/F731C69057E9C87F-b-b478053bc204497e9610b6439dcf9a07",
            };
            var actual = log.GetIndexEntries();

            CustomAsserts.AreCollectionsEqual<IEnumerable<string>, string>(expected, actual);
        }

        [TestMethod]
        public void LogIndexSerialiseListItemDeserialiseTest()
        {
            var log = new LogEntry()
            {
                Timestamp = new DateTime(2011, 09, 04, 23, 15, 23),
                GroupKey = new Guid("8d4abbb4-af9f-432f-bce5-e6da5a402469"),
                Status = LogStatus.Warning,
            };

            var expected = new LogEntryListItem()
            {
                Timestamp = new DateTime(2011, 09, 04, 23, 15, 23),
                Status = LogStatus.Warning,
            };

            var actual = new LogEntryListItem(log.GetIndexEntries().First());

            Assert.AreEqual(expected.Timestamp, actual.Timestamp);
            Assert.AreEqual(expected.Status, actual.Status);
        }
    }
}
