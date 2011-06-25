using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;

namespace Plywood.Tests.Functional
{
    class Program
    {
        public const string STR_PUSH_FOLDER = @"C:\PlywoodTesting\FunctionalTestResources\Test App";
        public const string STR_PULL_FOLDER = @"C:\PlywoodTesting\FunctionalTestResources\Pull Test";

        static void Main(string[] args)
        {
            // TODO: Point this at a test bucket (everything will be deleted from here).
            var context = new ControllerConfiguration()
            {
                AwsAccessKeyId = "ZXXXXXXXXXZXZXZXZXZX",
                AwsSecretAccessKey = "AWWWWWWWAWAWWWWWWWWAWAWAWAWWWWWWWAWAWAWA",
                BucketName = "YOUR_TEST_BUCKET_NAME",
            };

            // Wipe pull folder.
            System.IO.DirectoryInfo pullFolder = new System.IO.DirectoryInfo(Program.STR_PULL_FOLDER);
            if (pullFolder.Exists)
                pullFolder.Delete(true);

            WipeBucket(context);

            var group = GroupTests.Run(context);
            var app = AppTests.Run(context, group.Key);
            var version = VersionTests.Run(context, app.Key);
            VersionFileTests.Run(context, version.Key);
            var target = TargetTests.Run(context, group.Key);
            TargetAppTests.Run(context, target.Key, app.Key);
            TargetAppVersionTests.Run(context, target.Key, app.Key, version.Key);
            var instance = InstanceTests.Run(context, target.Key);
            LogTests.Run(context, instance.Key);

            // TODO: Remove these hard coded paths (used for testing the command line tools to do an actual push).
            var deploymentConfig = new DeploymentConfiguration()
            {
                AwsAccessKeyId = context.AwsAccessKeyId,
                AwsSecretAccessKey = context.AwsSecretAccessKey,
                BucketName = context.BucketName,
                CheckFrequency = TimeSpan.FromSeconds(10),
                DeploymentDirectory = @"C:\PlywoodTesting\ServiceDeployment",
                TargetKey = target.Key,
            };
            File.WriteAllText(@"C:\PlywoodTesting\TestConfig.txt", Utils.Serialisation.Serialise(deploymentConfig));
            File.WriteAllText(@"C:\PlywoodTesting\PlyTestApps\Test App\.appkey", app.Key.ToString("N"));
        }

        static void WipeBucket(ControllerConfiguration context)
        {
            using (var client = new Amazon.S3.AmazonS3Client(context.AwsAccessKeyId, context.AwsSecretAccessKey))
            {
                int batchSize = 100;
                int count = batchSize;
                while (count == batchSize)
                {
                    using (var listResponse = client.ListObjects(new Amazon.S3.Model.ListObjectsRequest()
                    {
                        BucketName = context.BucketName,
                        MaxKeys = batchSize,
                    }))
                    {
                        count = listResponse.S3Objects.Count;
                        Parallel.ForEach(listResponse.S3Objects, s3obj =>
                            {
                                using (var delResponse = client.DeleteObject(new Amazon.S3.Model.DeleteObjectRequest()
                                {
                                    BucketName = context.BucketName,
                                    Key = s3obj.Key,
                                })) { }
                            });
                    }
                }
            }
        }
    }
}
