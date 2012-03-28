using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Plywood.Tests.Framework.Unit
{
    [TestClass]
    public class ContextHierarchyTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetNameNull()
        {
            ContextHierarchy.GetName(null);
        }

        [TestMethod]
        public void GetNameEmpty()
        {
            var hierarchy = string.Empty;
            var expected = string.Empty;
            var actual = ContextHierarchy.GetName(hierarchy);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetNameSimple()
        {
            var hierarchy = "Simple";
            var expected = "Simple";
            var actual = ContextHierarchy.GetName(hierarchy);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetNameComplex()
        {
            var hierarchy = "Complex.Context";
            var expected = "Context";
            var actual = ContextHierarchy.GetName(hierarchy);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetNameComplexEmpty()
        {
            var hierarchy = "Complex.";
            var expected = string.Empty;
            var actual = ContextHierarchy.GetName(hierarchy);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void GetContextNull()
        {
            ContextHierarchy.GetContext(null);
        }

        [TestMethod]
        public void GetContextEmpty()
        {
            var hierarchy = string.Empty;
            string expected = null;
            var actual = ContextHierarchy.GetContext(hierarchy);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetContextSimple()
        {
            var hierarchy = "Simple";
            string expected = null;
            var actual = ContextHierarchy.GetContext(hierarchy);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetContextComplex()
        {
            var hierarchy = "Complex.Context";
            string expected = "Complex";
            var actual = ContextHierarchy.GetContext(hierarchy);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetContextComplexEmpty()
        {
            var hierarchy = ".Context";
            string expected = string.Empty;
            var actual = ContextHierarchy.GetContext(hierarchy);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void GetContextComplexEmpty2()
        {
            var hierarchy = "Complex.";
            string expected = "Complex";
            var actual = ContextHierarchy.GetContext(hierarchy);
            Assert.AreEqual(expected, actual);
        }
    }
}
