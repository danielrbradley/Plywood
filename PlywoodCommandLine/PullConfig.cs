using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Plywood.Ply
{
    class PullConfig
    {
        internal static void Run(string[] args)
        {
            if (args.Length < 3)
            {
                SyntaxError();
                return;
            }

            switch (args[2].ToLower())
            {
                case "view":
                    View();
                    break;
                case "load":
                    Load(args);
                    break;
                case "download":
                    Download(args);
                    break;
                default:
                    SyntaxError();
                    break;
            }
        }

        private static void Download(string[] args)
        {
            var userData = UserData.Importer.LoadInstanceLatestUserData();
            if (!userData.Sections.Any(s => s.Name == "Plywood"))
            {
                Console.WriteLine("Error: Plywood section not found!");
                return;
            }
            var config = Utils.Registry.LoadDeploymentConfiguration();
            if (config == null) config = new DeploymentConfiguration();
            var section = userData["Plywood"];
            if (section.Keys.Contains("AwsAccessKeyId"))
                config.AwsAccessKeyId = section["AwsAccessKeyId"];
            if (section.Keys.Contains("AwsSecretAccessKey"))
                config.AwsSecretAccessKey = section["AwsSecretAccessKey"];
            if (section.Keys.Contains("BucketName"))
                config.BucketName = section["BucketName"];
            if (section.Keys.Contains("CheckFrequency"))
                config.CheckFrequency = TimeSpan.Parse(section["CheckFrequency"]);
            if (section.Keys.Contains("DeploymentDirectory"))
                config.DeploymentDirectory = section["DeploymentDirectory"];
            if (section.Keys.Contains("TargetKey"))
                config.TargetKey = Guid.Parse(section["TargetKey"]);

            Utils.Registry.Save(config);
        }

        public static void View()
        {
            try
            {
                var config = Utils.Registry.LoadDeploymentConfiguration();
                if (config != null)
                {
                    Console.WriteLine(Utils.Serialisation.Serialise(config));
                }
                else
                {
                    Console.WriteLine("Configuration not set up.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed parsing the configuration from file.");
                Console.WriteLine(ex.ToString());
            }
        }

        public static void Load(string[] args)
        {
            if (args.Length < 4)
            {
                LoadSyntaxError();
                return;
            }

            try
            {
                string serialised;
                if (args.Length == 4)
                {
                    serialised = LoadFromFile(args[3]);
                }
                else if (args.Length == 5 && args[3] == "-f")
                {
                    serialised = LoadFromFile(args[4]);
                }
                else if (args.Length == 5 && args[3] == "-c")
                {
                    serialised = args[4];
                }
                else
                {
                    LoadSyntaxError();
                    return;
                }
                var config = Utils.Serialisation.ParsePullConfiguration(serialised);
                Utils.Registry.Save(config);
            }
            catch (HandledCliException) { throw; }
            catch (Exception ex)
            {
                Console.WriteLine("Failed parsing the configuration from file.");
                Console.WriteLine(ex.ToString());
            }
        }

        private static string LoadFromFile(string path)
        {
            try
            {
                return File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed reading pull configuration from file.");
                throw new HandledCliException();
            }
        }

        private static void SyntaxError()
        {
            Console.WriteLine("Try 'ply help pull config' for more information.");
        }

        private static void LoadSyntaxError()
        {
            Console.WriteLine("Try 'ply help pull config load' for more information.");
        }

        public static void Help(string[] args)
        {
            if (args.Length == 3)
                GeneralHelp();

            switch (args[3].ToLower())
            {
                case "view":
                    ViewHelp();
                    break;
                case "load":
                    LoadHelp();
                    break;
                case "download":
                    DownloadHelp();
                    break;
                default:
                    Console.WriteLine("Try 'ply help pull config' for more information.");
                    break;
            }
        }

        private static void GeneralHelp()
        {
            Console.WriteLine(
@"Usage: 'ply pull config [view|load|download]'
Manage the configuration for the pull service currently saved in the registry.");
        }

        private static void ViewHelp()
        {
            Console.WriteLine(
@"Usage: 'ply pull config view'
Views the configuration for the pull service currently saved in the registry.
There are no parameters for this command.");
        }

        private static void LoadHelp()
        {
            Console.WriteLine(
@"Usage: 'ply pull config load ([SOURCE FILENAME] | [OPTIONS]...)'
Loads a new configuration for the pull service from a file and saves it into 
the registry.
[SOURCE FILENAME]     Path to a file containing configuration for the pull
                      service.
-f                    Path to a file containing configuration for the pull
                      service.
-c                    Content of the configuration for the pull service.");
        }

        private static void DownloadHelp()
        {
            Console.WriteLine(
@"Usage: 'ply pull config download'
Merges the plywood section from the instance user data into the registry.");
        }

    }
}
