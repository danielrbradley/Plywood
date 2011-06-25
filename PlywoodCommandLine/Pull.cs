using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.PullService;

namespace Plywood.Ply
{
    class Pull
    {
        internal static void Run(string[] args)
        {
            if (args.Length < 2)
            {
                SyntaxError();
                return;
            }

            switch (args[1].ToLower())
            {
                case "all":
                    PullAll(args);
                    break;
                case "config":
                    PullConfig.Run(args);
                    break;
                default:
                    PullApp(args);
                    break;
            }
        }

        private static void PullApp(string[] args)
        {
            Guid appKey;
            if (!Guid.TryParse(args[1], out appKey))
            {
                SyntaxError();
                return;
            }
            var config = ParseConfig(args);
            var appDeploymentController = new AppDeployment(config);
            appDeploymentController.PullApp(appKey);
        }

        private static void PullAll(string[] args)
        {
            var config = ParseConfig(args);
            var appDeploymentController = new AppDeployment(config);
            appDeploymentController.SynchroniseAllApplications();
        }

        public static DeploymentConfiguration ParseConfig(string[] args, int argSkipCount = 2)
        {
            var config = Utils.Registry.LoadDeploymentConfiguration();
            for (int i = argSkipCount; i < args.Length; i++)
            {
                if (i < args.Length - 2)
                {
                    switch (args[i].ToLower())
                    {
                        case "--aws-access-key-id":
                            config.AwsAccessKeyId = args[++i];
                            break;
                        case "--aws-secret-access-key":
                            config.AwsSecretAccessKey = args[++i];
                            break;
                        case "--bucket-name":
                            config.BucketName = args[++i];
                            break;
                        case "--check-frequency":
                            TimeSpan checkFrequency;
                            if (!TimeSpan.TryParse(args[++i], out checkFrequency))
                                throw new CliSyntaxException("Timespan not in correct format for --check-frequency.");
                            break;
                        case "--target-key":
                            Guid targetKey;
                            if (!Guid.TryParse(args[++i], out targetKey))
                                throw new CliSyntaxException("Guid not in correct format for --target-key.");
                            break;
                        case "--deployment-directory":
                            config.DeploymentDirectory = args[++i];
                            break;
                        default:
                            break;
                    }
                }
            }

            if (config.AwsAccessKeyId == null)
                throw new CliSyntaxException("AwsAccessKeyId is missing.");
            if (config.AwsSecretAccessKey == null)
                throw new CliSyntaxException("AwsSecretAccessKey is missing.");
            if (config.BucketName == null)
                throw new CliSyntaxException("BucketName is missing.");
            if (config.DeploymentDirectory == null)
                throw new CliSyntaxException("DeploymentDirectory is missing.");
            if (config.TargetKey == Guid.Empty)
                throw new CliSyntaxException("TargetKey is missing.");

            return config;
        }

        #region Help Functions

        private static void SyntaxError()
        {
            Console.WriteLine("Try 'ply help pull' for more information.");
        }

        internal static void Help(string[] args)
        {
            if (args.Length == 2)
                HelpGeneral();
            else
            {
                switch (args[2].ToLower())
                {
                    case "all":
                        HelpAll();
                        break;
                    case "config":
                        PullConfig.Help(args);
                        break;
                    default:
                        SyntaxError();
                        break;
                }
            }
        }

        private static void HelpGeneral()
        {
            Console.WriteLine(
@"Usage: 'ply pull (all|[APPKEY]) [OPTIONS]...'
Pull apps from Plywood to the local system.

all                      Synchronise all the targets apps - 
[APPKEY]                 Synchronise changes to a specific app.

[OPTION]s                These override the registry configuration options.
--aws-access-key-id      
--aws-secret-access-key  
--bucket-name
--check-frequency
--target-key
--deployment-directory");
        }

        private static void HelpAll()
        {
            SyntaxError();
        }

        #endregion

    }
}
