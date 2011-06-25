using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;

namespace Plywood
{
    public class TargetAppVersions : ControllerBase
    {
        public const string STR_TARGET_APP_VERSION_INFO_EXTENSION = "target-version";

        public TargetAppVersions() : base() { }
        public TargetAppVersions(ControllerConfiguration context) : base(context) { }

        public VersionCheckResult TargetAppVersionChanged(Guid targetKey, Guid appKey, Guid localVersionKey)
        {
            try
            {
                string localETag;
                using (var localKeyStream = Utils.Serialisation.Serialise(localVersionKey))
                {
                    localETag = Utils.Validation.GenerateETag(localKeyStream);
                }
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObjectMetadata(new GetObjectMetadataRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = GetTargetAppVersionInfoPath(targetKey, appKey),
                    }))
                    {
                        if (res.ETag == localETag)
                            return VersionCheckResult.NotChanged;
                        else
                            return VersionCheckResult.Changed;
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                if (awsEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return VersionCheckResult.NotSet;
                }
                else
                {
                    throw new DeploymentException(string.Format("Failed checking for version updates for app with key \"{0}\" and target with the key \"{1}\".", appKey, targetKey), awsEx);
                }
            }
        }

        public Guid? GetTargetAppVersion(Guid targetKey, Guid appKey)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    using (var res = client.GetObject(new GetObjectRequest()
                    {
                        BucketName = Context.BucketName,
                        Key = GetTargetAppVersionInfoPath(targetKey, appKey),
                    }))
                    {
                        using (var stream = res.ResponseStream)
                        {
                            return Utils.Serialisation.ParseKey(stream);
                        }
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                if (awsEx.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw new DeploymentException(string.Format("Failed getting version for app with key \"{0}\" and target with the key \"{1}\".", appKey, targetKey), awsEx);
                }
            }
        }

        public void SetTargetAppVersion(Guid targetKey, Guid appKey, Guid? versionKey)
        {
            try
            {
                using (var client = new AmazonS3Client(Context.AwsAccessKeyId, Context.AwsSecretAccessKey))
                {
                    string targetAppVersionInfoPath = GetTargetAppVersionInfoPath(targetKey, appKey);
                    if (versionKey.HasValue)
                    {
                        using (var keyStream = Utils.Serialisation.Serialise(versionKey.Value))
                        {
                            // Put
                            using (var res = client.PutObject(new PutObjectRequest()
                            {
                                BucketName = Context.BucketName,
                                Key = targetAppVersionInfoPath,
                                InputStream = keyStream,
                                GenerateMD5Digest = true,
                            })) { }
                        }
                    }
                    else
                    {
                        // Delete
                        using (var res = client.DeleteObject(new DeleteObjectRequest()
                        {
                            BucketName = Context.BucketName,
                            Key = targetAppVersionInfoPath,
                        })) { }
                    }
                }
            }
            catch (AmazonS3Exception awsEx)
            {
                throw new DeploymentException(string.Format("Failed setting version for app with key \"{0}\" and target with the key \"{1}\".", appKey, targetKey), awsEx);
            }
        }

        public static string GetTargetAppVersionInfoPath(Guid targetKey, Guid appKey)
        {
            return string.Format("{0}/{1}/{2}.{3}", Apps.STR_APPS_CONTAINER_PATH, appKey.ToString("N"), targetKey.ToString("N"), STR_TARGET_APP_VERSION_INFO_EXTENSION);
        }

    }
}
