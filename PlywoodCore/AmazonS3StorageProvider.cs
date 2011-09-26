using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood
{
    public class AmazonS3StorageProvider : IPlywoodStorageProvider
    {
        public void DeleteFile(string path)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string path)
        {
            throw new NotImplementedException();
        }

        public System.IO.Stream GetFile(string path)
        {
            throw new NotImplementedException();
        }

        public void PutFile(string path, System.IO.Stream content = null)
        {
            throw new NotImplementedException();
        }

        public void MoveFile(string oldPath, string newPath)
        {
            throw new NotImplementedException();
        }

        public void MoveFolder(string oldFolderPath, string newFolderPath)
        {
            throw new NotImplementedException();
        }

        public FileListing ListFiles(string folderPath, string marker, int pageSize)
        {
            throw new NotImplementedException();
        }
    }
}
