using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.Framework.Unit.Entities
{
    /// <summary>
    /// Summary description for AppTests
    /// </summary>
    [TestClass]
    public class ContextTests
    {
        [TestMethod]
        public void ContextSerialiseDeserialise()
        {
            var originalContext = new Context()
            {
                Name = "Test Context",
                Tags = new Dictionary<string, string>()
                {
                    { "tagKey", "Some tag value." },
                    { "secondKey", "Multiline \r\n   test!" }
                }
            };

            Context secondContext;
            using (var stream = originalContext.Serialise())
            {
                secondContext = new Context(stream);
            }

            Assert.AreEqual(originalContext.Key, secondContext.Key);
            Assert.AreEqual(originalContext.Name, secondContext.Name);

            Assert.IsNotNull(secondContext.Tags);
            foreach (var tag in originalContext.Tags)
            {
                Assert.IsTrue(secondContext.Tags.ContainsKey(tag.Key));
                Assert.AreEqual(tag.Value, secondContext.Tags[tag.Key]);
            }
        }
    }
}
