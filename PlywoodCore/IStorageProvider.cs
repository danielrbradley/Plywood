using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Plywood
{
    public interface IStorageProvider
    {
        void DeleteFile(string path);
        bool FileExists(string path);
        Stream GetFile(string path);
        void PutFile(string path, Stream content);
        void MoveFolder(string oldFolderPath, string newFolderPath);
        FileListing ListFiles(string folderPath, string marker, int pageSize);
    }
}
