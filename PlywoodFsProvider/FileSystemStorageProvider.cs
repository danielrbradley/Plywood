using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Plywood.FsProvider
{
    public class FileSystemStorageProvider : IStorageProvider
    {
        public FileSystemStorageProvider(string rootPath)
        {
            this.rootPath = rootPath;
        }

        private string rootPath;

        public void DeleteFile(string path)
        {
            DeleteFile(new FilePath(path));
        }

        public void DeleteFile(FilePath path)
        {
            File.Delete(GetFsPath(path));
        }

        public bool FileExists(string path)
        {
            return this.FileExists(new FilePath(path));
        }

        public bool FileExists(FilePath path)
        {
            return File.Exists(GetFsPath(path));
        }

        public System.IO.Stream GetFile(string path)
        {
            return this.GetFile(new FilePath(path));
        }

        public System.IO.Stream GetFile(FilePath path)
        {
            return File.OpenRead(GetFsPath(path));
        }

        public void PutFile(string path, System.IO.Stream content = null)
        {
            this.PutFile(new FilePath(path), content);
        }

        public void PutFile(FilePath path, System.IO.Stream content = null)
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
            MoveFile(new FilePath(oldPath), new FilePath(newPath));
        }

        public void MoveFile(FilePath oldPath, FilePath newPath)
        {
            File.Move(GetFsPath(oldPath), GetFsPath(newPath));
        }

        public void MoveFolder(string oldFolderPath, string newFolderPath)
        {
            MoveFolder(new FolderPath(oldFolderPath), new FolderPath(newFolderPath));
        }

        public void MoveFolder(FolderPath oldFolderPath, FolderPath newFolderPath)
        {
            Directory.Move(GetFsPath(oldFolderPath), GetFsPath(newFolderPath));
        }

        public FileListing ListFiles(string folderPath, string marker, int pageSize)
        {
            return ListFiles(new FolderPath(folderPath), marker, pageSize);
        }

        public FileListing ListFiles(FolderPath folderPath, string marker, int pageSize)
        {
            var files = Directory.EnumerateFiles(GetFsPath(folderPath)).SkipWhile(f => string.Compare(Path.GetFileName(f), marker) <= 0).Take(pageSize + 1).Select(f => Path.GetFileName(f)).ToList();
            var page = files.Take(pageSize).ToList();
            var isTruncated = files.Skip(pageSize).Any();
            var nextMarker = page.Any() ? page.Last() : marker;
            return new FileListing()
            {
                FolderPath = folderPath.Value,
                Items = page.ToList(),
                Marker = marker,
                PageSize = pageSize,
                IsTruncated = isTruncated,
                NextMarker = nextMarker
            };
        }

        private string GetFsPath(IPath path)
        {
            if (!path.IsValid)
            {
                throw new FormatException("The specified path is not valid.");
            }

            return Path.Combine(rootPath, path.Value.Replace("/", @"\").TrimStart(new char[1] { '\\' }));
        }
    }
}
