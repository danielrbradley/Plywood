using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;

namespace Plywood
{
    public class AmazonS3StorageProvider : IPlywoodStorageProvider, IDisposable
    {
        public AmazonS3StorageProvider(string awsAccessKeyId, string awsSecretAccessKey, string bucketName, AmazonS3Config config = null)
        {
            this.awsAccessKeyId = awsAccessKeyId;
            this.awsSecretAccessKey = new SecureString();
            foreach (var c in awsSecretAccessKey)
            {
                this.awsSecretAccessKey.AppendChar(c);
            }
            this.awsSecretAccessKey.MakeReadOnly();
            this.bucketName = bucketName;
            this.config = config;
        }

        public AmazonS3StorageProvider(string awsAccessKeyId, SecureString awsSecretAccessKey, string bucketName, AmazonS3Config config = null)
        {
            this.awsAccessKeyId = awsAccessKeyId;
            this.awsSecretAccessKey = awsSecretAccessKey;
            this.bucketName = bucketName;
            this.config = config;
        }

        public void Dispose()
        {
            awsSecretAccessKey.Dispose();
        }

        private string awsAccessKeyId, bucketName;
        private SecureString awsSecretAccessKey;
        private AmazonS3Config config;

        public void DeleteFile(string path)
        {
            this.DeleteFile(new StorageFilePath(path));
        }

        public void DeleteFile(StorageFilePath path)
        {
            if (!path.IsValid)
            {
                throw new FormatException("The specified path is not a valid file path.");
            }

            using (var client = new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, config))
            {
                using (var response = client.DeleteObject(
                    new DeleteObjectRequest()
                    {
                        BucketName = bucketName,
                        Key = path.Value,
                    }))
                {
                    if (!response.IsDeleteMarker)
                    {
                        throw new FileNotFoundException("The requested file does not exist.", path.Value);
                    }
                }
            }
        }

        public bool FileExists(string path)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(StorageFilePath path)
        {
            throw new NotImplementedException();
        }

        public Stream GetFile(string path)
        {
            throw new NotImplementedException();
        }

        public Stream GetFile(StorageFilePath path)
        {
            throw new NotImplementedException();
        }

        public void PutFile(string path, Stream content = null)
        {
            throw new NotImplementedException();
        }

        public void PutFile(StorageFilePath path, Stream content = null)
        {
            throw new NotImplementedException();
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
    }
}
