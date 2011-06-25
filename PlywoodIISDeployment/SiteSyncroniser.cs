using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.Schema;
using System.Xml;
using System.IO;
using Microsoft.Web.Administration;

namespace Plywood.IISDeployApi
{
    public static class IISSyncroniser
    {
        public static void SyncroniseIIS(XDocument plywoodIisDeploymentConfig)
        {
            var server = new ServerManager();

            SyncAppPoolSettings(server, plywoodIisDeploymentConfig);

            SyncSiteSettings(server, plywoodIisDeploymentConfig);

            server.CommitChanges();
        }

        public static void UninstallIISSetup(XDocument plywoodIisDeploymentConfig)
        {
            var server = new ServerManager();

            server.Sites.Remove(server.Sites[plywoodIisDeploymentConfig.Root.Element("site").Attribute("name").Value]);

            server.ApplicationPools.Remove(server.ApplicationPools[plywoodIisDeploymentConfig.Root.Element("applicationPool").Attribute("name").Value]);

            server.CommitChanges();

            foreach (var siteApplicationPhysicalPath in plywoodIisDeploymentConfig.Root.Element("site").Element("applications").Elements("add").Select(e => e.Attribute("physicalPath").Value))
            {
                Directory.Delete(siteApplicationPhysicalPath, true);
            }
        }

        static void SyncAppPoolSettings(ServerManager server, XDocument plywoodIisDeploymentConfig)
        {
            foreach (var appPoolElement in plywoodIisDeploymentConfig.Root.Elements("applicationPool"))
            {
                var appPoolName = appPoolElement.Attribute("name").Value;

                ApplicationPool applicationPool;
                if (server.ApplicationPools.Any(pool => pool.Name == appPoolName))
                {
                    applicationPool = server.ApplicationPools[appPoolName];
                }
                else
                {
                    applicationPool = server.ApplicationPools.Add(appPoolName);
                }

                var autoStartAttribute = appPoolElement.Attribute("autoStart");
                applicationPool.AutoStart = false;
                if (autoStartAttribute != null)
                {
                    bool autoStart;
                    if (bool.TryParse(autoStartAttribute.Value, out autoStart))
                        applicationPool.AutoStart = autoStart;
                }

                var enabled32BitAppOnWin64Attribute = appPoolElement.Attribute("enable32BitAppOnWin64");
                if (enabled32BitAppOnWin64Attribute != null)
                {
                    bool enable32BitAppOnWin64;
                    if (bool.TryParse(enabled32BitAppOnWin64Attribute.Value, out enable32BitAppOnWin64))
                        applicationPool.Enable32BitAppOnWin64 = enable32BitAppOnWin64;
                }

                var managedPipelineModeAttribute = appPoolElement.Attribute("managedPipelineMode");
                if (managedPipelineModeAttribute != null)
                {
                    ManagedPipelineMode managedPipelineMode;
                    if (Enum.TryParse<ManagedPipelineMode>(managedPipelineModeAttribute.Value, out managedPipelineMode))
                        applicationPool.ManagedPipelineMode = managedPipelineMode;
                }

                var managedRuntimeVersionAttribute = appPoolElement.Attribute("managedRuntimeVersion");
                if (managedRuntimeVersionAttribute != null) applicationPool.ManagedRuntimeVersion = managedRuntimeVersionAttribute.Value;

                var queueLenthAttribute = appPoolElement.Attribute("queueLength");
                if (queueLenthAttribute != null)
                {
                    long queueLength;
                    if (long.TryParse(queueLenthAttribute.Value, out queueLength))
                        applicationPool.QueueLength = queueLength;
                }

                // TODO: Set up all the other properties of the app pool.
            }
        }

        static void SyncSiteSettings(ServerManager server, XDocument plywoodIisDeploymentConfig)
        {
            var siteElement = plywoodIisDeploymentConfig.Root.Element("site");
            var siteName = siteElement.Attribute("name").Value;
            var bindingsElement = siteElement.Element("bindings");
            var applicationsElement = siteElement.Element("applications");
            var applicationsAddElements = applicationsElement.Elements("add");

            Site site;
            if (server.Sites.Any(s => s.Name == siteName))
            {
                site = server.Sites.First(s => s.Name == siteName);
            }
            else
            {
                var firstBindingAddElement = bindingsElement.Element("add");
                site = server.Sites.Add(
                    siteName,
                    firstBindingAddElement.Attribute("protocol").Value,
                    firstBindingAddElement.Attribute("information").Value,
                    applicationsAddElements.First().Attribute("physicalPath").Value);
            }

            // Sync applications.
            if (applicationsElement.Elements("clear").Any())
            {
                site.Applications.Clear();
            }

            foreach (var applicationAddElement in applicationsAddElements)
            {
                Application app;
                var applicationPath = applicationAddElement.Attribute("path").Value;

                if (!site.Applications.Any(s => s.Path == applicationPath))
                    app = site.Applications.Add(applicationPath, applicationAddElement.Attribute("physicalPath").Value);
                else
                    app = site.Applications[applicationPath];

                var applicationPoolNameAttribute = applicationAddElement.Attribute("applicationPoolName");
                if (applicationPoolNameAttribute != null) app.ApplicationPoolName = applicationPoolNameAttribute.Value;

                var enabledProtocolsAttribute = applicationAddElement.Attribute("enabledProtocols");
                if (enabledProtocolsAttribute != null) app.EnabledProtocols = enabledProtocolsAttribute.Value;
            }

            // Sync bindings.
            if (bindingsElement.Elements("clear").Any())
                site.Bindings.Clear();

            foreach (var bindingAddElement in bindingsElement.Elements("add"))
            {
                var newBinding = site.Bindings.Add(bindingAddElement.Attribute("information").Value, bindingAddElement.Attribute("protocol").Value);
                // TODO: Implement certificate and ds mapper setup.
            }
        }
    }
}
