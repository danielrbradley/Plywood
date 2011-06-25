using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;

namespace Plywood
{
    public static class Configuration
    {
        public static class Registry
        {
            public static ControllerConfiguration LoadControllerConfiguration()
            {
                throw new NotImplementedException();
            }

            public static DeploymentConfiguration LoadDeploymentConfiguration()
            {
                throw new NotImplementedException();
            }
        }

        public static class AppSettings
        {
            public const string AWS_ACCESS_KEY_ID_CONFIG_KEY = "Plywood.AwsAccessKeyId";
            public const string AWS_SECRET_ACCESS_KEY_CONFIG_KEY = "Plywood.AwsSecretAccessKey";
            public const string BUCKET_NAME_CONFIG_KEY = "Plywood.BucketName";
            public const string CHECK_FREQEUENCY_CONFIG_KEY = "Plywood.CheckFrequency";
            public const string DEPLOYMENT_DIRECTORY_CONFIG_KEY = "Plywood.DeploymentDirectory";
            public const string INSTANCE_KEY_CONFIG_KEY = "Plywood.InstanceKey";
            public const string TARGET_KEY_CONFIG_KEY = "Plywood.TargetKey";

            public static ControllerConfiguration LoadControllerConfiguration()
            {
                IEnumerable<string> appSettingsKeys = ConfigurationManager.AppSettings.Keys.Cast<string>();
                if (!appSettingsKeys.Contains(AWS_ACCESS_KEY_ID_CONFIG_KEY))
                    throw new ConfigurationException(string.Format("Application configuration key \"{0}\" not found.", AWS_ACCESS_KEY_ID_CONFIG_KEY));
                if (!appSettingsKeys.Contains(AWS_SECRET_ACCESS_KEY_CONFIG_KEY))
                    throw new ConfigurationException(string.Format("Application configuration key \"{0}\" not found.", AWS_SECRET_ACCESS_KEY_CONFIG_KEY));
                if (!appSettingsKeys.Contains(BUCKET_NAME_CONFIG_KEY))
                    throw new ConfigurationException(string.Format("Application configuration key \"{0}\" not found.", BUCKET_NAME_CONFIG_KEY));

                return new ControllerConfiguration()
                {
                    AwsAccessKeyId = ConfigurationManager.AppSettings[AWS_ACCESS_KEY_ID_CONFIG_KEY],
                    AwsSecretAccessKey = ConfigurationManager.AppSettings[AWS_SECRET_ACCESS_KEY_CONFIG_KEY],
                    BucketName = ConfigurationManager.AppSettings[BUCKET_NAME_CONFIG_KEY],
                };
            }

            public static DeploymentConfiguration LoadDeploymentConfiguration()
            {
                var controllerConfiguration = LoadControllerConfiguration();

                TimeSpan checkFrequency;
                Guid? instanceKey = null;
                Guid targetKey;

                IEnumerable<string> appSettingsKeys = ConfigurationManager.AppSettings.Keys.Cast<string>();
                if (!appSettingsKeys.Contains(CHECK_FREQEUENCY_CONFIG_KEY))
                    throw new ConfigurationException(string.Format("Application configuration key \"{0}\" not found.", CHECK_FREQEUENCY_CONFIG_KEY));
                if (!appSettingsKeys.Contains(DEPLOYMENT_DIRECTORY_CONFIG_KEY))
                    throw new ConfigurationException(string.Format("Application configuration key \"{0}\" not found.", DEPLOYMENT_DIRECTORY_CONFIG_KEY));
                if (!appSettingsKeys.Contains(TARGET_KEY_CONFIG_KEY))
                    throw new ConfigurationException(string.Format("Application configuration key \"{0}\" not found.", TARGET_KEY_CONFIG_KEY));

                if (appSettingsKeys.Contains(INSTANCE_KEY_CONFIG_KEY) && 
                    !string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[INSTANCE_KEY_CONFIG_KEY]))
                {
                    try
                    {
                        instanceKey = Guid.Parse(ConfigurationManager.AppSettings[INSTANCE_KEY_CONFIG_KEY]);
                    }
                    catch (Exception ex)
                    {
                        throw new ConfigurationException("Failed parsing instance key from app config.", ex);
                    }
                }

                try
                {
                    checkFrequency = TimeSpan.Parse(ConfigurationManager.AppSettings[CHECK_FREQEUENCY_CONFIG_KEY]);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException("Failed parsing check frequency from app config.", ex);
                }

                try
                {
                    targetKey = Guid.Parse(ConfigurationManager.AppSettings[TARGET_KEY_CONFIG_KEY]);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationException("Failed parsing target key from app config.", ex);
                }

                return new DeploymentConfiguration()
                {
                    AwsAccessKeyId = controllerConfiguration.AwsAccessKeyId,
                    AwsSecretAccessKey = controllerConfiguration.AwsSecretAccessKey,
                    BucketName = controllerConfiguration.BucketName,
                    CheckFrequency = checkFrequency,
                    DeploymentDirectory = ConfigurationManager.AppSettings[AWS_ACCESS_KEY_ID_CONFIG_KEY],
                    InstanceKey = instanceKey,
                    TargetKey = targetKey,
                };
            }
        }
    }

    public class ControllerConfiguration
    {
        public string AwsAccessKeyId { get; set; }
        public string AwsSecretAccessKey { get; set; }
        public string BucketName { get; set; }
    }

    public class DeploymentConfiguration : ControllerConfiguration
    {
        /// <summary>
        /// Frequency to poll the central system for updates.
        /// Default: 10 seconds.
        /// </summary>
        public TimeSpan CheckFrequency { get; set; }

        /// <summary>
        /// The current target's key to pull updates from.
        /// </summary>
        public Guid TargetKey { get; set; }

        /// <summary>
        /// Path to pull deployment folders into. 
        /// Default: "C:\Deployment"
        /// </summary>
        public string DeploymentDirectory { get; set; }

        /// <summary>
        /// Unique key to identify a specific instance pulling from a target.
        /// </summary>
        public Guid? InstanceKey { get; set; }

    }
}