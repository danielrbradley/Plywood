using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Plywood.Ply
{
    class Push
    {
        public static void Run(string[] args)
        {
            string comment;
            string directoryPath;
            Guid appKey;
            ControllerConfiguration context;

            ParseArguments(args, out comment, out directoryPath, out appKey, out context);

            var versionsController = new Versions(context);
            var appsController = new Apps(context);
            var revision = appsController.PushAppRevision(appKey);
            var newVersion = new Version()
            {
                Name = string.Format("{0} {1}", revision, comment),
                AppKey = appKey,
            };
            versionsController.PushVersion(new DirectoryInfo(directoryPath), newVersion.Key);
            versionsController.CreateVersion(newVersion);
        }

        private static void ParseArguments(string[] args, out string comment, out string directoryPath, out Guid appKey, out ControllerConfiguration context)
        {
            comment = null;
            directoryPath = Directory.GetCurrentDirectory();
            Guid? tempAppKey = null;
            string awsAccessKeyId = null;
            string awsSecretAccessKey = null;
            string bucketName = null;


            if (args.Length == 2)
            {
                // Just parse for a comment.
                if (args[1].StartsWith("-"))
                    PushSyntaxError();
                comment = args[1];
            }
            else if (args.Length > 2)
            {
                // Read named parameters.
                for (int i = 1; i < args.Length; i++)
                {
                    if (i > args.Length - 2)
                        PushSyntaxError();

                    switch (args[i].ToLower())
                    {
                        case "-a":
                        case "--app-key":
                            Guid parsedAppKey;
                            if (!Guid.TryParse(args[++i], out parsedAppKey))
                                PushSyntaxError();
                            tempAppKey = parsedAppKey;
                            break;
                        case "-b":
                        case "--bucket-name":
                            bucketName = args[++i];
                            break;
                        case "-c":
                        case "--comment":
                            comment = args[++i];
                            break;
                        case "-d":
                        case "--directory":
                            directoryPath = args[++i];
                            break;
                        case "-k":
                        case "--aws-access-key-id":
                            awsAccessKeyId = args[++i];
                            break;
                        case "-s":
                        case "--aws-secret-access-key":
                            awsSecretAccessKey = args[++i];
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                PushSyntaxError();
            }

            // Verify comment.
            if (string.IsNullOrWhiteSpace(comment))
            {
                Console.WriteLine("Comment must be specified and cannot be blank.");
                throw new HandledCliException();
            }

            // Verify directory.
            DirectoryInfo directory;
            try
            {
                directory = new DirectoryInfo(directoryPath);
            }
            catch
            {
                Console.WriteLine("Invalid directory.");
                throw new HandledCliException();
            }
            if (!directory.Exists)
            {
                Console.WriteLine("Directory does not exist.");
                throw new HandledCliException();
            }

            // Verify app key.
            if (!tempAppKey.HasValue)
            {
                try
                {
                    using (var stream = File.Open(directory.FullName + @"\.appkey", FileMode.Open))
                    {
                        tempAppKey = Utils.Serialisation.ParseKey(stream);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("No app key specified and failed loading key from .appkey file.");
                    throw new HandledCliException();
                }
            }
            appKey = tempAppKey.Value;

            // Verify configuration.
            if (awsAccessKeyId == null || awsSecretAccessKey == null || bucketName == null)
            {
                // Try loading from file.
                try
                {
                    using (var stream = File.Open(directory.FullName + @"\.config", FileMode.Open))
                    {
                        var fileConfig = Utils.Serialisation.ParseControllerConfiguration(stream);
                        if (awsAccessKeyId == null)
                            awsAccessKeyId = fileConfig.AwsAccessKeyId;
                        if (awsSecretAccessKey == null)
                            awsSecretAccessKey = fileConfig.AwsSecretAccessKey;
                        if (bucketName == null)
                            bucketName = fileConfig.BucketName;
                    }
                }
                catch
                {
                    Console.WriteLine("Missing controller configuration properties and failed loading from .config file.");
                    throw new HandledCliException();
                }
            }
            if (awsAccessKeyId.Length != 20)
            {
                Console.WriteLine("Invalid AWS Access Key ID.");
                throw new HandledCliException();
            }
            if (awsSecretAccessKey.Length != 40)
            {
                Console.WriteLine("Invalid AWS Secret Access Key.");
                throw new HandledCliException();
            }
            if (string.IsNullOrWhiteSpace(bucketName))
            {
                Console.WriteLine("Bucket name cannot be blank.");
                throw new HandledCliException();
            }
            context = new ControllerConfiguration()
            {
                AwsAccessKeyId = awsAccessKeyId,
                AwsSecretAccessKey = awsSecretAccessKey,
                BucketName = bucketName,
            };
        }

        private static void PushSyntaxError()
        {
            Console.WriteLine("Try 'ply help push' for more information.");
            throw new HandledCliException();
        }

        public static void Help(string[] args)
        {
            Console.WriteLine(
@"Usage: 'ply push ([COMMENT] | [OPTION]...)'
Pushes a new version of an application.
[COMMENT]             Comment to be saved as the name of the version.

[OPTION]s
-a, --app-key         Guid of the application to push the new version to.
-c, --comment         Comment to be saved as the name of the version.
-d, --directory       Path of the directory to push as a new version.

If the directory is not specified, the current working directory is used. If no
app key is specified then it will be loaded from the .appkey file of the 
directory to push.");
        }
    }
}
