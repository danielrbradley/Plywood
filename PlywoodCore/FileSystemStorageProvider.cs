using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Plywood
{
    public class FileSystemStorageProvider : IPlywoodStorageProvider
    {
        public FileSystemStorageProvider(string rootPath)
        {
            this.rootPath = rootPath;
        }

        private string rootPath;

        public void DeleteFile(string path)
        {
            DeleteFile(new StorageFilePath(path));
        }

        public void DeleteFile(StorageFilePath path)
        {
            File.Delete(GetFsPath(path));
        }

        public bool FileExists(string path)
        {
            return this.FileExists(new StorageFilePath(path));
        }

        public bool FileExists(StorageFilePath path)
        {
            return File.Exists(GetFsPath(path));
        }

        public System.IO.Stream GetFile(string path)
        {
            return this.GetFile(new StorageFilePath(path));
        }

        public System.IO.Stream GetFile(StorageFilePath path)
        {
            return File.OpenRead(GetFsPath(path));
        }

        public void PutFile(string path, System.IO.Stream content = null)
        {
            this.PutFile(new StorageFilePath(path), content);
        }

        public void PutFile(StorageFilePath path, System.IO.Stream content = null)
        {
            using (var stream = File.Create(GetFsPath(path)))
            {
                if (content != null)
                {
                    content.CopyTo(stream);
                }
            }
        }

        public void MoveFile(string oldPath, string newPath)
        {
            throw new NotImplementedException();
        }

        public void MoveFile(StorageFilePath oldPath, StorageFilePath newPath)
        {
            throw new NotImplementedException();
        }

        public void MoveFolder(string oldFolderPath, string newFolderPath)
        {
            throw new NotImplementedException();
        }

        public void MoveFolder(StorageFilePath oldFolderPath, StorageFilePath newFolderPath)
        {
            throw new NotImplementedException();
        }

        public FileListing ListFiles(string folderPath, string marker, int pageSize)
        {
            throw new NotImplementedException();
        }

        public FileListing ListFiles(StorageFilePath folderPath, string marker, int pageSize)
        {
            throw new NotImplementedException();
        }

        private string GetFsPath(StorageFilePath path)
        {
            if (!path.IsValid)
            {
                throw new FormatException("The specified path is not valid.");
            }

            return Path.Combine(rootPath, path.Value.Replace("/", @"\"));
        }
    }
}
