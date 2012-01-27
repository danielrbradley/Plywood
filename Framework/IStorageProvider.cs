using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Plywood
{
    public interface IStorageProvider
    {
        /// <summary>
        /// Delete a single file.
        /// </summary>
        /// <param name="path">Absolute unix-style path.</param>
        /// <exception cref="FileNotFoundException">Thrown if the no file exists on the specified path.</exception>
        void DeleteFile(string path);

        /// <summary>
        /// Delete a single file.
        /// </summary>
        /// <param name="path">Absolute unix-style path.</param>
        /// <exception cref="FileNotFoundException">Thrown if the no file exists on the specified path.</exception>
        void DeleteFile(FilePath path);

        /// <summary>
        /// Check if a file exists.
        /// </summary>
        /// <param name="path">Absolute unix-style path to check.</param>
        /// <returns>Boolean indicating if the file exists.</returns>
        bool FileExists(string path);

        /// <summary>
        /// Check if a file exists.
        /// </summary>
        /// <param name="path">Absolute unix-style path to check.</param>
        /// <returns>Boolean indicating if the file exists.</returns>
        bool FileExists(FilePath path);

        /// <summary>
        /// Load the content of an existing file.
        /// </summary>
        /// <param name="path">Absolute unix-style path to the file to load.</param>
        /// <returns>Stream containing the content of the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the no file exists on the specified path.</exception>
        Stream GetFile(string path);

        /// <summary>
        /// Load the content of an existing file.
        /// </summary>
        /// <param name="path">Absolute unix-style path to the file to load.</param>
        /// <returns>Stream containing the content of the file.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the no file exists on the specified path.</exception>
        Stream GetFile(FilePath path);

        /// <summary>
        /// Get the hash of a file.
        /// </summary>
        /// <param name="path">Path of the file to check.</param>
        /// <returns>The md5 hash of a file or null if not found.</returns>
        /// <remarks>String must be lowercase with no dashes or surrounding quotes.</remarks>
        string GetFileHash(string path);

        /// <summary>
        /// Get the hash of a file.
        /// </summary>
        /// <param name="path">Path of the file to check.</param>
        /// <returns>The md5 hash of a file or null if not found.</returns>
        string GetFileHash(FilePath path);

        /// <summary>
        /// Create a new file with the selected content.
        /// </summary>
        /// <param name="path">Absolute unix-style path of the file to create.</param>
        /// <param name="content">Optional content of the file.</param>
        void PutFile(string path, Stream content = null);

        /// <summary>
        /// Create a new file with the selected content.
        /// </summary>
        /// <param name="path">Absolute unix-style path of the file to create.</param>
        /// <param name="content">Optional content of the file.</param>
        void PutFile(FilePath path, Stream content = null);

        /// <summary>
        /// Move (or rename) a file from one path to another.
        /// </summary>
        /// <param name="oldPath">The current, absolute unix-style path of the file.</param>
        /// <param name="newPath">The new, absolute unix-style path of the file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the no file exists on the old path.</exception>
        void MoveFile(string oldPath, string newPath);

        /// <summary>
        /// Move (or rename) a file from one path to another.
        /// </summary>
        /// <param name="oldPath">The current, absolute unix-style path of the file.</param>
        /// <param name="newPath">The new, absolute unix-style path of the file.</param>
        /// <exception cref="FileNotFoundException">Thrown if the no file exists on the old path.</exception>
        void MoveFile(FilePath oldPath, FilePath newPath);

        /// <summary>
        /// Move a folder and its content from one path to another.
        /// </summary>
        /// <param name="oldFolderPath">The current, absolute unix-style path of the folder.</param>
        /// <param name="newFolderPath">The new, absolute unix-style path of the folder.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if no folder exists at the specified old path.</exception>
        void MoveFolder(string oldFolderPath, string newFolderPath);

        /// <summary>
        /// Move a folder and its content from one path to another.
        /// </summary>
        /// <param name="oldFolderPath">The current, absolute unix-style path of the folder.</param>
        /// <param name="newFolderPath">The new, absolute unix-style path of the folder.</param>
        /// <exception cref="DirectoryNotFoundException">Thrown if no folder exists at the specified old path.</exception>
        void MoveFolder(FolderPath oldFolderPath, FolderPath newFolderPath);

        /// <summary>
        /// Get a page of filenames from a folder.
        /// </summary>
        /// <param name="folderPath">Absolute, unix-style path of the folder to search within</param>
        /// <param name="marker">String by which all filenames must be lexographically after.</param>
        /// <param name="pageSize">Maximum number of filenames to return.</param>
        /// <returns>File listing collection containing the filenames ordered by name ascending.</returns>
        FileListing ListFiles(string folderPath, string marker, int pageSize);

        /// <summary>
        /// Get a page of filenames from a folder.
        /// </summary>
        /// <param name="folderPath">Absolute, unix-style path of the folder to search within</param>
        /// <param name="marker">String by which all filenames must be lexographically after.</param>
        /// <param name="pageSize">Maximum number of filenames to return.</param>
        /// <returns>File listing collection containing the filenames ordered by name ascending.</returns>
        FileListing ListFiles(FolderPath folderPath, string marker, int pageSize);
    }
}
