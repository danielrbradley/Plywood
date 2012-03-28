using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Web.Deployment;

namespace Plywood.IISDeployApi
{
    public static class PackageDeployer
    {
        public static void DeployPackage(string packageFilePath)
        {
            var sourceProvider = DeploymentWellKnownProvider.Package;
            var sourcePath = packageFilePath;
            var sourceBaseOptions = new DeploymentBaseOptions();

            var destinationProvider = DeploymentWellKnownProvider.Auto;
            var destinationPath = "";
            DeploymentBaseOptions destinationBaseOptions = new DeploymentBaseOptions();
            DeploymentSyncOptions destinationSyncOptions = new DeploymentSyncOptions();

            using (DeploymentObject deploymentObject = DeploymentManager.CreateObject(sourceProvider, sourcePath, sourceBaseOptions))
            {
                deploymentObject.SyncTo(destinationProvider, destinationPath, destinationBaseOptions, destinationSyncOptions);
            }
        }

    }
}
