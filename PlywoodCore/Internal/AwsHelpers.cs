using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using System.Threading.Tasks;

namespace Plywood.Internal
{
    public static class AwsHelpers
    {
        public static void SoftDeleteFolders(ControllerConfiguration context, string folder)
        {
            SoftDeleteFolders(context, new List<string>() { folder });
        }

        public static void SoftDeleteFolders(ControllerConfiguration context, IEnumerable<string> folders)
        {
            if (context == null)
                throw new ArgumentNullException("context", "Context cannot be null.");
            if (folders == null)
                throw new ArgumentNullException("folders", "Folders cannot be null.");

            using (var client = new AmazonS3Client(context.AwsAccessKeyId, context.AwsSecretAccessKey))
            {
                foreach (var folder in folders)
                {
                    int maxResults = 100;
                    int lastCount = maxResults;
                    while (maxResults == lastCount)
                    {
                        using (var listResponse = client.ListObjects(new ListObjectsRequest()
                        {
                            BucketName = context.BucketName,
                            Prefix = folder,
                        }))
                        {
                            lastCount = listResponse.S3Objects.Count;

                            Parallel.ForEach(listResponse.S3Objects, folderObject =>
                                {
                                    using (var copyResponse = client.CopyObject(new CopyObjectRequest()
                                    {
                                        SourceBucket = context.BucketName,
                                        DestinationBucket = context.BucketName,
                                        SourceKey = folderObject.Key,
                                        DestinationKey = ".recycled/" + folderObject.Key,
                                    })) { }
                                });

                            Parallel.ForEach(listResponse.S3Objects, folderObject =>
                                {
                                    using (var deleteReponse = client.DeleteObject(new DeleteObjectRequest()
                                    {
                                        BucketName = context.BucketName,
                                        Key = folderObject.Key,
                                    })) { }
                                });
                        }
                    }
                }
            }
        }

    }
}
