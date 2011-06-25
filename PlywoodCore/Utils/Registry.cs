using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Plywood.Utils
{
    public static class Registry
    {
        public const string STR_CONFIG_REGISTRY_KEY_NAME = @"HKEY_LOCAL_MACHINE\SOFTWARE\Plywood";
        public const string STR_DEPLOYMENT_CONFIG_NAME = "Deployment.config";

        public static DeploymentConfiguration LoadDeploymentConfiguration()
        {
            DeploymentConfiguration config = new DeploymentConfiguration();
            config.AwsAccessKeyId = Microsoft.Win32.Registry.GetValue(STR_CONFIG_REGISTRY_KEY_NAME, "AwsAccessKeyId", null) as string;
            config.AwsSecretAccessKey = Microsoft.Win32.Registry.GetValue(STR_CONFIG_REGISTRY_KEY_NAME, "AwsSecretAccessKey", null) as string;
            config.BucketName = Microsoft.Win32.Registry.GetValue(STR_CONFIG_REGISTRY_KEY_NAME, "BucketName", null) as string;
            config.DeploymentDirectory = Microsoft.Win32.Registry.GetValue(STR_CONFIG_REGISTRY_KEY_NAME, "DeploymentDirectory", null) as string;

            Guid targetKey;
            if (Guid.TryParseExact(Microsoft.Win32.Registry.GetValue(STR_CONFIG_REGISTRY_KEY_NAME, "TargetKey", null) as string, "N", out targetKey))
                config.TargetKey = targetKey;

            TimeSpan checkFrequency;
            if (TimeSpan.TryParse(Microsoft.Win32.Registry.GetValue(STR_CONFIG_REGISTRY_KEY_NAME, "CheckFrequency", null) as string, out checkFrequency))
                config.CheckFrequency = checkFrequency;

            Guid instanceKey;
            if (Guid.TryParseExact(Microsoft.Win32.Registry.GetValue(STR_CONFIG_REGISTRY_KEY_NAME, "InstanceKey", null) as string, "N", out instanceKey))
                config.InstanceKey = instanceKey;

            if (config.AwsAccessKeyId != null)
            {
                return config;
            }
            else
            {
                // Fail over to old style config.
                var serialised = Microsoft.Win32.Registry.GetValue(STR_CONFIG_REGISTRY_KEY_NAME, STR_DEPLOYMENT_CONFIG_NAME, null) as string;
                if (serialised != null)
                {
                    return Utils.Serialisation.ParsePullConfiguration(serialised);
                }
            }
            return null;
        }

        public static void Save(DeploymentConfiguration config)
        {
            Microsoft.Win32.Registry.SetValue(STR_CONFIG_REGISTRY_KEY_NAME, "AwsAccessKeyId", config.AwsAccessKeyId, Microsoft.Win32.RegistryValueKind.String);
            Microsoft.Win32.Registry.SetValue(STR_CONFIG_REGISTRY_KEY_NAME, "AwsSecretAccessKey", config.AwsSecretAccessKey, Microsoft.Win32.RegistryValueKind.String);
            Microsoft.Win32.Registry.SetValue(STR_CONFIG_REGISTRY_KEY_NAME, "BucketName", config.BucketName, Microsoft.Win32.RegistryValueKind.String);
            Microsoft.Win32.Registry.SetValue(STR_CONFIG_REGISTRY_KEY_NAME, "CheckFrequency", config.CheckFrequency.ToString(), Microsoft.Win32.RegistryValueKind.String);
            Microsoft.Win32.Registry.SetValue(STR_CONFIG_REGISTRY_KEY_NAME, "DeploymentDirectory", config.DeploymentDirectory, Microsoft.Win32.RegistryValueKind.String);
            Microsoft.Win32.Registry.SetValue(STR_CONFIG_REGISTRY_KEY_NAME, "TargetKey", config.TargetKey.ToString("N"), Microsoft.Win32.RegistryValueKind.String);
            if (config.InstanceKey.HasValue)
                Microsoft.Win32.Registry.SetValue(STR_CONFIG_REGISTRY_KEY_NAME, "InstanceKey", config.InstanceKey.Value.ToString("N"), Microsoft.Win32.RegistryValueKind.String);
        }
    }
}
