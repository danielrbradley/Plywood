using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml;
using System.IO;
using Microsoft.Web.Deployment;

namespace Plywood.IISDeployApi
{
    public static class IISDeployer
    {
        public const string PLYWOOD_IIS_DEPLOYMENT_CONFIG_FILENAME = "PlywoodIISDeployment.config";
        public static void Deploy(string deploymentSourceDirectory)
        {
            Deploy(deploymentSourceDirectory, Path.Combine(deploymentSourceDirectory, PLYWOOD_IIS_DEPLOYMENT_CONFIG_FILENAME));
        }

        public static void Deploy(string deploymentSourceDirectory, string plywoodIisDeploymentConfigPath)
        {
            XDocument plywoodIisDeploymentConfig;
            using (var plywoodIisDeploymentConfigContent = File.OpenRead(plywoodIisDeploymentConfigPath))
            {
                plywoodIisDeploymentConfig = XDocument.Load(plywoodIisDeploymentConfigContent);
            }
            Deploy(deploymentSourceDirectory, plywoodIisDeploymentConfig);
        }

        public static void Deploy(string deploymentSourceDirectory, XDocument plywoodIisDeploymentConfig)
        {
            if (!Directory.Exists(deploymentSourceDirectory))
                throw new DirectoryNotFoundException("The deployment source directory must exist.");

            ValidateConfig(plywoodIisDeploymentConfig);

            var packagePaths = plywoodIisDeploymentConfig.Root.Elements("package").Select(p => Path.Combine(deploymentSourceDirectory, p.Attribute("path").Value)).ToArray();

            foreach (var packagePath in packagePaths)
            {
                if (!File.Exists(packagePath))
                    throw new FileNotFoundException("Could not find package file.");
            }

            IISSyncroniser.SyncroniseIIS(plywoodIisDeploymentConfig);

            foreach (var packagePath in packagePaths)
            {
                PackageDeployer.DeployPackage(packagePath);
            }

        }

        public static void UnDeploy(string deploymentSourceDirectory)
        {
            UnDeploy(deploymentSourceDirectory, Path.Combine(deploymentSourceDirectory, PLYWOOD_IIS_DEPLOYMENT_CONFIG_FILENAME));
        }

        public static void UnDeploy(string deploymentSourceDirectory, string plywoodIisDeploymentConfigPath)
        {
            XDocument plywoodIisDeploymentConfig;
            using (var plywoodIisDeploymentConfigContent = File.OpenRead(plywoodIisDeploymentConfigPath))
            {
                plywoodIisDeploymentConfig = XDocument.Load(plywoodIisDeploymentConfigContent);
            }
            UnDeploy(deploymentSourceDirectory, plywoodIisDeploymentConfig);
        }

        public static void UnDeploy(string deploymentSourceDirectory, XDocument plywoodIisDeploymentConfig)
        {
            if (!Directory.Exists(deploymentSourceDirectory))
                throw new DirectoryNotFoundException("The deployment source directory must exist.");

            ValidateConfig(plywoodIisDeploymentConfig);

            IISSyncroniser.UninstallIISSetup(plywoodIisDeploymentConfig);
        }

        static void ValidateConfig(XDocument plywoodIisDeploymentConfig)
        {
            try
            {
                var schemas = new XmlSchemaSet();
                using (var schemaStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Plywood.IISDeployApi.WebApplication.xsd"))
                {
                    schemas.Add("", XmlReader.Create(schemaStream));
                }

                plywoodIisDeploymentConfig.Validate(schemas, null);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed validating setup configuration.", ex);
            }
        }
    }
}
