using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Plywood.FsProvider.Tests
{
    [TestClass]
    public class FsProviderTests
    {
        [TestInitialize]
        public void Initilize()
        {
            var tempDir = new DirectoryInfo(Path.GetTempPath());
            var baseDir = tempDir.CreateSubdirectory(Guid.NewGuid().ToString());
            BaseDirectory = baseDir.FullName;

            using (var stream = File.Create(Path.Combine(baseDir.FullName, "rootfile.txt")))
            {
            }

            var infoDir = baseDir.CreateSubdirectory("info-directory");
            using (var stream = File.CreateText(Path.Combine(infoDir.FullName, "contentfile1.txt")))
            {
                stream.Write("Test content 1");
            }

            using (var stream = File.CreateText(Path.Combine(infoDir.FullName, "filetomove.txt")))
            {
                stream.Write("Test content 1");
            }

            using (var stream = File.CreateText(Path.Combine(infoDir.FullName, "deleteme1.txt")))
            {
                stream.Write("Content to delete 1.");
            }

            var listingDir = baseDir.CreateSubdirectory("listing-directory");
            using (var stream = File.CreateText(Path.Combine(listingDir.FullName, "1firstfile.txt")))
            {
            }

            using (var stream = File.CreateText(Path.Combine(listingDir.FullName, "2secondfile.txt")))
            {
            }

            using (var stream = File.CreateText(Path.Combine(listingDir.FullName, "3thirdfile.txt")))
            {
            }

            StorageProvider = new FsProvider.FileSystemStorageProvider(BaseDirectory);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Directory.Delete(BaseDirectory, true);
        }

        private string BaseDirectory { get; set; }
        private IStorageProvider StorageProvider { get; set; }

        [TestMethod]
        public void RootFileExistsTrueTest()
        {
            Assert.IsTrue(StorageProvider.FileExists("rootfile.txt"));
        }

        [TestMethod]
        public void FileExistsTrueTest()
        {
            Assert.IsTrue(StorageProvider.FileExists("info-directory/contentfile1.txt"));
        }

        [TestMethod]
        public void FileExistsFalseTest()
        {
            Assert.IsFalse(StorageProvider.FileExists("info-directory/notacontentfile1.txt"));
        }

        [TestMethod]
        public void DeleteFileTest()
        {
            Assert.IsTrue(StorageProvider.FileExists("info-directory/deleteme1.txt"));
            StorageProvider.DeleteFile("info-directory/deleteme1.txt");
            Assert.IsFalse(StorageProvider.FileExists("info-directory/deleteme1.txt"));
        }

        [TestMethod]
        public void ReadFileTest()
        {
            var expected = "Test content 1";
            string actual;
            using (var stream = StorageProvider.GetFile("info-directory/contentfile1.txt"))
            {
                var textReader = new StreamReader(stream);
                actual = textReader.ReadToEnd();
            }
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void MoveFileTest()
        {
            Assert.IsTrue(StorageProvider.FileExists("info-directory/filetomove.txt"));
            Assert.IsFalse(StorageProvider.FileExists("info-directory/movedfile.txt"));
            StorageProvider.MoveFile("info-directory/filetomove.txt", "info-directory/movedfile.txt");
            Assert.IsTrue(StorageProvider.FileExists("info-directory/movedfile.txt"));
            Assert.IsFalse(StorageProvider.FileExists("info-directory/filetomove.txt"));
        }

        [TestMethod]
        public void ListingFirstTwoTest()
        {
            var expected = new FileListing()
            {
                FolderPath = "listing-directory",
                IsTruncated = true,
                Items = new List<string>()
                {
                    "1firstfile.txt",
                    "2secondfile.txt",
                },
                Marker = null,
                NextMarker = "2secondfile.txt",
                PageSize = 2,
            };
            var actual = StorageProvider.ListFiles("listing-directory", null, 2);
            AssertListingsEqual(expected, actual);
        }

        [TestMethod]
        public void ListingNoneTest()
        {
            var expected = new FileListing()
            {
                FolderPath = "listing-directory",
                IsTruncated = true,
                Items = new List<string>()
                {
                },
                Marker = null,
                NextMarker = null,
                PageSize = 0,
            };
            var actual = StorageProvider.ListFiles("listing-directory", null, 0);
            AssertListingsEqual(expected, actual);
        }

        [TestMethod]
        public void ListingSecondOnlyTest()
        {
            var expected = new FileListing()
            {
                FolderPath = "listing-directory",
                IsTruncated = true,
                Items = new List<string>()
                {
                    "2secondfile.txt",
                },
                Marker = "1firstfile.txt",
                NextMarker = "2secondfile.txt",
                PageSize = 1,
            };
            var actual = StorageProvider.ListFiles("listing-directory", "1firstfile.txt", 1);
            AssertListingsEqual(expected, actual);
        }

        [TestMethod]
        public void ListingAllExactTest()
        {
            var expected = new FileListing()
            {
                FolderPath = "listing-directory",
                IsTruncated = false,
                Items = new List<string>()
                {
                    "1firstfile.txt",
                    "2secondfile.txt",
                    "3thirdfile.txt",
                },
                Marker = null,
                NextMarker = "3thirdfile.txt",
                PageSize = 3,
            };
            var actual = StorageProvider.ListFiles("listing-directory", null, 3);
            AssertListingsEqual(expected, actual);
        }

        [TestMethod]
        public void ListingAllHighLimitTest()
        {
            var expected = new FileListing()
            {
                FolderPath = "listing-directory",
                IsTruncated = false,
                Items = new List<string>()
                {
                    "1firstfile.txt",
                    "2secondfile.txt",
                    "3thirdfile.txt",
                },
                Marker = null,
                NextMarker = "3thirdfile.txt",
                PageSize = 10,
            };
            var actual = StorageProvider.ListFiles("listing-directory", null, 10);
            AssertListingsEqual(expected, actual);
        }

        private void AssertListingsEqual(FileListing expected, FileListing actual)
        {
            Assert.AreEqual(expected.FolderPath, actual.FolderPath);
            Assert.AreEqual(expected.IsTruncated, actual.IsTruncated);
            Assert.AreEqual(expected.Marker, actual.Marker);
            Assert.AreEqual(expected.NextMarker, actual.NextMarker);
            Assert.AreEqual(expected.PageSize, actual.PageSize);
            Assert.AreEqual(expected.Items.Count, actual.Items.Count, "Item collection contains {0} items, was expecting {1}.", actual.Items.Count, expected.Items.Count);
            for (int i = 0; i < expected.Items.Count; i++)
            {
                Assert.AreEqual(expected.Items[i], actual.Items[i], "Was expecting item at index {0} to be \"{1}\", was actually \"{2}\".", i, expected.Items[i], actual.Items[i]);
            }
        }
    }
}
