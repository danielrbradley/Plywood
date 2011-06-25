using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Plywood.Utils
{
    public static class Files
    {
        public static string GetRelativePath(string filePath, string basePath)
        {
            if (filePath.StartsWith(basePath))
                return filePath.Remove(0, basePath.Length).TrimStart('\\').Replace('\\', '/');
            throw new FileNotFoundException("File is not contained in path.");
        }

        public static string GetLocalAbsolutePath(string fileKey, string keyPrefix, string basePath)
        {
            if (String.IsNullOrEmpty(fileKey))
                throw new ArgumentException("fileKey is null or empty.", "fileKey");
            if (String.IsNullOrEmpty(keyPrefix))
                throw new ArgumentException("keyPrefix is null or empty.", "keyPrefix");
            if (String.IsNullOrEmpty(basePath))
                throw new ArgumentException("basePath is null or empty.", "basePath");
            if (!fileKey.StartsWith(keyPrefix))
                throw new ArgumentException("Key prefix must be a prefix of the file key.", "keyPrefix");

            return String.Format("{0}\\{1}", basePath, fileKey.Remove(0, keyPrefix.Length).Replace('/', '\\'));
        }
    }
}
