using Plywood.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace Plywood.Tests.Framework.Unit
{
    
    
    /// <summary>
    ///This is a test class for FilesTest and is intended
    ///to contain all FilesTest Unit Tests
    ///</summary>
    [TestClass()]
    public class FilesTest
    {

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        [TestMethod()]
        public void GetRelativePathTest()
        {
            FileInfo file = new FileInfo(@"C:\WINDOWS\system32\notepad.exe");
            DirectoryInfo baseDirectory = new DirectoryInfo(@"C:\WINDOWS\system32");
            string expected = "notepad.exe";
            string actual;
            actual = Files.GetRelativePath(file.FullName, baseDirectory.FullName);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void GetRelativePathSubDirTest()
        {
            FileInfo file = new FileInfo(@"C:\WINDOWS\system32\notepad.exe");
            DirectoryInfo baseDirectory = new DirectoryInfo(@"C:\WINDOWS");
            string expected = @"system32/notepad.exe";
            string actual;
            actual = Files.GetRelativePath(file.FullName, baseDirectory.FullName);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void GetLocalAbsolutePathTest()
        {
            string fileKey = "versions/e848a3417b304163a6cfa7dc198aa349/notepad.exe";
            string keyPrefix = "versions/e848a3417b304163a6cfa7dc198aa349/";
            DirectoryInfo baseDirectory = new DirectoryInfo(@"C:\WINDOWS\system32");
            string expected = @"C:\WINDOWS\system32\notepad.exe";
            string actual;
            actual = Files.GetLocalAbsolutePath(fileKey, keyPrefix, baseDirectory.FullName);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod()]
        public void GetLocalAbsolutePathSubDirTest()
        {
            string fileKey = "versions/e848a3417b304163a6cfa7dc198aa349/system32/notepad.exe";
            string keyPrefix = "versions/e848a3417b304163a6cfa7dc198aa349/";
            DirectoryInfo baseDirectory = new DirectoryInfo(@"C:\WINDOWS");
            string expected = @"C:\WINDOWS\system32\notepad.exe";
            string actual;
            actual = Files.GetLocalAbsolutePath(fileKey, keyPrefix, baseDirectory.FullName);
            Assert.AreEqual(expected, actual);
        }
    }
}
