using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Plywood.PullService
{
    public class AppDeployment
    {
        private const string STR_LOCAL_APP_INDEX_FILE = @"\.apps.index";
        private DeploymentConfiguration config;
        private TextWriter logWriter;
        private LogStatus deploymentStatus = LogStatus.Ok;

        public AppDeployment() { }

        public AppDeployment(DeploymentConfiguration config, TextWriter logWriter = null)
        {
            this.config = config;
            this.logWriter = logWriter;
        }

        private void WriteLog(string message)
        {
            if (logWriter != null)
            {
                logWriter.Write("{0} {1}\r\n", DateTime.Now.ToString(), message);
            }
        }

        public LogStatus DeploymentStatus { get { return deploymentStatus; } }
        private void ElevateStatusTo(LogStatus newStatus)
        {
            if (newStatus > deploymentStatus)
            {
                deploymentStatus = newStatus;
            }
        }

        public Dictionary<Guid, Guid> GetLocalAppVersionsIndex()
        {
            var localAppIndexFile = new FileInfo(config.DeploymentDirectory + STR_LOCAL_APP_INDEX_FILE);
            if (!localAppIndexFile.Exists)
                return new Dictionary<Guid, Guid>();

            List<Internal.EntityIndexEntry> index;
            using (var stream = localAppIndexFile.OpenRead())
            {
                index = Internal.Indexes.ParseIndex(stream);
            }
            try
            {
                return index.ToDictionary(e => e.Key, e => Guid.ParseExact(e.Name, "N"));
            }
            catch (Exception ex)
            {
                throw new DeserialisationException("Failed reading local app versions index.", ex);
            }
        }

        public void PutAppVersionIndexEntry(Guid appKey, Guid versionKey)
        {
            var localAppIndexFile = new FileInfo(config.DeploymentDirectory + STR_LOCAL_APP_INDEX_FILE);
            string serialised;
            if (localAppIndexFile.Exists)
            {
                List<Plywood.Internal.EntityIndexEntry> index;
                using (var stream = localAppIndexFile.Open(FileMode.Open, FileAccess.Read))
                {
                    index = Internal.Indexes.ParseIndex(stream);
                }
                var existingCount = index.Count(e => e.Key == appKey);
                if (existingCount == 0)
                    index.Add(new Internal.EntityIndexEntry() { Key = appKey, Name = versionKey.ToString("N") });
                else if (existingCount == 1)
                    index.Single(e => e.Key == appKey).Name = versionKey.ToString("N");
                else
                {
                    index.RemoveAll(e => e.Key == appKey);
                    index.Add(new Internal.EntityIndexEntry() { Key = appKey, Name = versionKey.ToString("N") });
                }
                index.Sort(delegate(Internal.EntityIndexEntry a, Internal.EntityIndexEntry b)
                {
                    return string.Compare(a.Name, b.Name, true);
                });
                serialised = Internal.Indexes.SerialiseIndex(index);
            }
            else
            {
                serialised = Internal.Indexes.SerialiseIndex(new List<Internal.EntityIndexEntry>(1) { new Internal.EntityIndexEntry() { Key = appKey, Name = versionKey.ToString("N") } });
            }
            using (var stream = localAppIndexFile.Open(FileMode.Create, FileAccess.Write))
            {
                var writer = new StreamWriter(stream);
                writer.Write(serialised);
                writer.Flush();
                stream.Flush();
            }
        }

        public void DeleteAppVersionIndexEntry(Guid appKey)
        {
            var localAppIndexFile = new FileInfo(config.DeploymentDirectory + STR_LOCAL_APP_INDEX_FILE);
            string serialised;
            if (localAppIndexFile.Exists)
            {
                List<Plywood.Internal.EntityIndexEntry> index;
                using (var stream = localAppIndexFile.Open(FileMode.Open, FileAccess.Read))
                {
                    index = Internal.Indexes.ParseIndex(stream);
                }
                if (index.Any(e => e.Key == appKey))
                {
                    index.RemoveAll(e => e.Key == appKey);
                    index.Sort(delegate(Internal.EntityIndexEntry a, Internal.EntityIndexEntry b)
                    {
                        return string.Compare(a.Name, b.Name, true);
                    });
                    serialised = Internal.Indexes.SerialiseIndex(index);
                    using (var stream = localAppIndexFile.Open(FileMode.Create, FileAccess.Write))
                    {
                        var writer = new StreamWriter(stream);
                        writer.Write(serialised);
                        writer.Flush();
                        stream.Flush();
                    }
                }
            }
        }

        public void PullApp(Guid appKey)
        {
            var appsController = new Apps(config);
            var versionsController = new Versions(config);
            var localAppVersions = GetLocalAppVersionsIndex();

            var app = appsController.GetApp(appKey);
            var latestVersion = versionsController.SearchAppVersions(appKey, pageSize: 1);
            if (!latestVersion.Versions.Any()) throw new VersionNotFoundException("No versions available for installation.");
            Guid versionKey = latestVersion.Versions.First().Key;

            if (localAppVersions.ContainsKey(appKey))
            {
                if (localAppVersions[appKey] != versionKey)
                {
                    RunUpdate(appKey, versionKey);
                }
            }
            else
            {
                Install(appKey, versionKey);
            }
        }

        public void SynchroniseAllApplications()
        {
            var targetAppsController = new TargetApps(config);
            var remoteApps = targetAppsController.GetTargetAppKeys(config.TargetKey);

            var localAppVersions = GetLocalAppVersionsIndex();

            var installList = remoteApps.Where(r => !localAppVersions.ContainsKey(r));
            var uninstallList = localAppVersions.Keys.Where(l => !remoteApps.Contains(l));
            var checkList = localAppVersions.Where(l => remoteApps.Contains(l.Key));

            foreach (var app in uninstallList)
            {
                try
                {
                    WriteLog(string.Format("Uninstalling app {0} ...", app));
                    Uninstall(app);
                    WriteLog(string.Format("Completed uninstalling app {0}", app));
                }
                catch (Exception ex)
                {
                    ElevateStatusTo(LogStatus.Error);
                    string errorMessage = string.Format("Failed uninstalling app {0} : {1}", app, ex.ToString());
                    WriteLog(errorMessage);
                    System.Diagnostics.EventLog.WriteEntry("Plywood", errorMessage, System.Diagnostics.EventLogEntryType.Error, 3005);
                }
            }
            foreach (var app in installList)
            {
                Guid versionKey;
                try
                {
                    versionKey = GetInstallVersion(app);
                    try
                    {
                        WriteLog(string.Format("Installing app {0} with version {1} ...", app, versionKey));
                        Install(app, versionKey);
                        WriteLog(string.Format("Completed installing app {0} with version {1}", app, versionKey));
                    }
                    catch (Exception ex)
                    {
                        ElevateStatusTo(LogStatus.Error);
                        string errorMessage = string.Format("Failed installing app {0} with version {1} : {2}", app, versionKey, ex.ToString());
                        WriteLog(errorMessage);
                        System.Diagnostics.EventLog.WriteEntry("Plywood", errorMessage, System.Diagnostics.EventLogEntryType.Error, 3005);
                    }
                }
                catch (Exception ex)
                {
                    ElevateStatusTo(LogStatus.Warning);
                    string errorMessage = string.Format("Failed loading app {0} version to install : {1}", app, ex.ToString());
                    WriteLog(errorMessage);
                    System.Diagnostics.EventLog.WriteEntry("Plywood", "Failed installing app: " + ex.ToString(), System.Diagnostics.EventLogEntryType.Error, 3005);
                }
            }
            foreach (var app in checkList)
            {
                try
                {
                    Update(app.Key, app.Value);
                }
                catch (Exception ex)
                {
                    ElevateStatusTo(LogStatus.Error);
                    string errorMessage = string.Format("Failed updating app {0} from version {1} : {2}", app.Key, app.Value, ex.ToString());
                    WriteLog(errorMessage);
                    System.Diagnostics.EventLog.WriteEntry("Plywood", errorMessage, System.Diagnostics.EventLogEntryType.Error, 3005);
                }
            }
        }

        public void Install(Guid appKey, Guid versionKey)
        {
            var apps = new Apps(config);
            var app = apps.GetApp(appKey);
            string workingDirectory = String.Format("{0}\\{1}", config.DeploymentDirectory, app.DeploymentDirectory);

            if (app.Tags.ContainsKey("hook-install"))
            {
                // Run hook-install
                logWriter.Write("Running install hook.");
                RunHook(app.Tags["hook-install"], config.DeploymentDirectory);
            }

            PullFolders(appKey, versionKey, true, true);
            PutAppVersionIndexEntry(appKey, versionKey);

            var directory = new DirectoryInfo(workingDirectory);

            if (File.Exists(Path.Combine(workingDirectory, Plywood.IISDeployApi.IISDeployer.PLYWOOD_IIS_DEPLOYMENT_CONFIG_FILENAME)))
                Plywood.IISDeployApi.IISDeployer.Deploy(workingDirectory);

            if (app.Tags.ContainsKey("hook-installed"))
            {
                // Run hook-installed
                logWriter.Write("Running installed hook.");
                RunHook(app.Tags["hook-installed"], workingDirectory);
            }
            foreach (var hookFile in directory.EnumerateFiles("hook-installed.*"))
            {
                try
                {
                    RunCommand(hookFile.FullName);
                }
                catch (Exception ex)
                {
                    ElevateStatusTo(LogStatus.Warning);
                    WriteLog(string.Format("Failed running installed hook: {0}", ex.ToString()));
                }
            }
        }

        private Guid GetInstallVersion(Guid appKey)
        {
            var targetAppVersionController = new TargetAppVersions(config);
            System.Guid? updatedVersionKey = targetAppVersionController.GetTargetAppVersion(config.TargetKey, appKey);
            if (updatedVersionKey.HasValue)
                return updatedVersionKey.Value;

            var versionController = new Versions(config);
            var res = versionController.SearchAppVersions(appKey, pageSize: 1);
            if (res.Versions.Count() > 0)
                return res.Versions.First().Key;

            throw new AppDeploymentException(string.Format("Failed updating application \"{0}\", no versions found.", appKey));
        }

        public void Uninstall(Guid appKey)
        {
            var apps = new Apps(config);
            var app = apps.GetApp(appKey);
            string workingDirectory = String.Format("{0}\\{1}", config.DeploymentDirectory, app.DeploymentDirectory);
            var targetDirectory = new DirectoryInfo(workingDirectory);

            if (File.Exists(Path.Combine(workingDirectory, Plywood.IISDeployApi.IISDeployer.PLYWOOD_IIS_DEPLOYMENT_CONFIG_FILENAME)))
                Plywood.IISDeployApi.IISDeployer.UnDeploy(workingDirectory);

            if (app.Tags.ContainsKey("hook-uninstall"))
            {
                // Run hook-install
                WriteLog("Running uninstall hook.");
                RunHook(app.Tags["hook-uninstall"], workingDirectory);
            }
            foreach (var hookFile in targetDirectory.EnumerateFiles("hook-uninstall.*"))
            {
                try
                {
                    RunCommand(hookFile.FullName);
                }
                catch (Exception ex)
                {
                    ElevateStatusTo(LogStatus.Warning);
                    WriteLog(string.Format("Failed running uninstall hook: {0}", ex.ToString()));
                }
            }

            targetDirectory.Delete(true);
            DeleteAppVersionIndexEntry(appKey);

            if (app.Tags.ContainsKey("hook-uninstalled"))
            {
                // Run hook-installed
                WriteLog("Running uninstalled hook.");
                RunHook(app.Tags["hook-uninstalled"], config.DeploymentDirectory);
            }
        }

        public void Update(Guid appKey, Guid versionKey)
        {
            var targetAppVersionController = new TargetAppVersions(config);
            VersionCheckResult changeResult = targetAppVersionController.TargetAppVersionChanged(config.TargetKey, appKey, versionKey);
            if (changeResult == VersionCheckResult.Changed)
            {
                System.Guid? updatedVersionKey = targetAppVersionController.GetTargetAppVersion(config.TargetKey, appKey);
                if (updatedVersionKey.HasValue)
                    RunUpdate(appKey, updatedVersionKey.Value);
            }
            else if (changeResult == VersionCheckResult.NotSet)
            {
                // Check for latest version
                var versionController = new Versions(config);
                var res = versionController.SearchAppVersions(appKey, pageSize: 1);
                if (res.Versions.Count() != 1)
                {
                    throw new AppDeploymentException(string.Format("Failed updating application \"{0}\", no versions found.", appKey));
                }
                if (res.Versions.Single().Key != versionKey)
                {
                    RunUpdate(appKey, res.Versions.Single().Key);
                }
            }
        }

        private void RunUpdate(Guid appKey, Guid newVersionKey)
        {
            WriteLog(string.Format("Updating app {0} to version {1} ...", appKey, newVersionKey));
            try
            {
                var apps = new Apps(config);
                var versions = new Versions(config);
                var app = apps.GetApp(appKey);
                var version = versions.GetVersion(newVersionKey);
                string workingDirectory = String.Format("{0}\\{1}", config.DeploymentDirectory, app.DeploymentDirectory);
                var directory = new DirectoryInfo(workingDirectory);

                if (app.Tags.ContainsKey("hook-update"))
                {
                    // Run hook-install
                    WriteLog("Running app update hook.");
                    RunHook(app.Tags["hook-update"], workingDirectory);
                }
                if (version.Tags.ContainsKey("hook-update"))
                {
                    // Run hook-install
                    WriteLog("Running version update hook.");
                    RunHook(version.Tags["hook-update"], workingDirectory);
                }
                foreach (var hookFile in directory.EnumerateFiles("hook-update.*"))
                {
                    try
                    {
                        RunCommand(hookFile.FullName);
                    }
                    catch (Exception ex)
                    {
                        ElevateStatusTo(LogStatus.Warning);
                        WriteLog(string.Format("Failed running update hook: {0}", ex.ToString()));
                    }
                }

                PullFolders(appKey, newVersionKey);
                PutAppVersionIndexEntry(appKey, newVersionKey);

                directory.Refresh();

                if (File.Exists(Path.Combine(workingDirectory, Plywood.IISDeployApi.IISDeployer.PLYWOOD_IIS_DEPLOYMENT_CONFIG_FILENAME)))
                    Plywood.IISDeployApi.IISDeployer.Deploy(workingDirectory);

                if (version.Tags.ContainsKey("hook-updated"))
                {
                    // Run hook-install
                    WriteLog("Running version updated hook.");
                    RunHook(version.Tags["hook-updated"], workingDirectory);
                }
                if (app.Tags.ContainsKey("hook-updated"))
                {
                    // Run hook-install
                    WriteLog("Running app updated hook.");
                    RunHook(app.Tags["hook-updated"], workingDirectory);
                }
                foreach (var hookFile in directory.EnumerateFiles("hook-updated.*"))
                {
                    try
                    {
                        RunCommand(hookFile.FullName);
                    }
                    catch (Exception ex)
                    {
                        ElevateStatusTo(LogStatus.Warning);
                        WriteLog(string.Format("Failed running updated hook: {0}", ex.ToString()));
                    }
                }
                WriteLog(string.Format("Completed update of app {0} to version {1} ...", appKey, newVersionKey));
            }
            catch (Exception ex)
            {
                string errorMessage = string.Format("Failed update of app {0} to version {1} : {2}", appKey, newVersionKey, ex.ToString());
                WriteLog(errorMessage);
                System.Diagnostics.EventLog.WriteEntry("Plywood", errorMessage, System.Diagnostics.EventLogEntryType.Error, 3005);
            }
        }

        private void RunCommand(string path, string arguments = null, TimeSpan? timeout = null)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentException("Path is null or empty.", "path");
            if (!timeout.HasValue)
                timeout = TimeSpan.FromSeconds(30);
            var file = new FileInfo(path);
            if (!file.Exists)
                throw new FileNotFoundException("The specified path to run was not found.");

            var commandLogBuilder = new StringBuilder();
            commandLogBuilder.AppendLine("---- Running command ----");
            commandLogBuilder.AppendFormat(" -- Working Directory: {0}\r\n", file.DirectoryName);
            commandLogBuilder.AppendFormat(" -- Path: {0}\r\n", path);
            if (arguments != null)
                commandLogBuilder.AppendFormat(" -- Arguments: {0}\r\n", arguments);

            var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
            {
                FileName = path,
                WorkingDirectory = file.DirectoryName,
                UseShellExecute = false,
            });

            // Will timeout and kill the process after 30 seconds.
            proc.WaitForExit((int)timeout.Value.TotalMilliseconds);
            if (!proc.HasExited)
            {
                commandLogBuilder.AppendLine("Forcing thread to exit.");
                ElevateStatusTo(LogStatus.Warning);
                proc.Kill();
            }

            WriteLog(commandLogBuilder.ToString());
        }

        private void RunHook(string commands, string workingDirectory = null)
        {
            if (workingDirectory == null)
                workingDirectory = Directory.GetCurrentDirectory();

            var hookLogBuilder = new StringBuilder("---- Running hook ----\r\n");

            var path = workingDirectory + "\\" + "hook.bat";
            bool cleanup = false;
            try
            {
                if (!string.IsNullOrWhiteSpace(commands))
                {
                    cleanup = true;
                    File.WriteAllText(path, commands);

                    var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo() { FileName = path, WorkingDirectory = workingDirectory, UseShellExecute = false });

                    // Will timeout and kill the process after 30 seconds.
                    proc.WaitForExit(30 * 1000);
                    if (!proc.HasExited)
                    {
                        hookLogBuilder.AppendLine("Forcing exit of thread.");
                        proc.Kill();
                    }
                }
            }
            catch (Exception ex)
            {
                hookLogBuilder.AppendFormat("Failed running hook: {0}\r\n", ex.ToString());
                ElevateStatusTo(LogStatus.Warning);
            }
            finally
            {
                if (cleanup)
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (Exception ex)
                    {
                        hookLogBuilder.AppendFormat("Failed hook cleanup: {0}\r\n", ex.ToString());
                        ElevateStatusTo(LogStatus.Warning);
                    }
                }
            }
            WriteLog(hookLogBuilder.ToString());
        }

        private void PullFolders(Guid appKey, Guid newVersionKey, bool create = false, bool clean = true)
        {
            var appsController = new Apps(config);
            var app = appsController.GetApp(appKey);
            var targetDirectory = new DirectoryInfo(config.DeploymentDirectory + "\\" + app.DeploymentDirectory);
            if (!targetDirectory.Exists)
            {
                if (create)
                {
                    targetDirectory.Create();
                    targetDirectory.Refresh();
                }
                else
                {
                    throw new AppDeploymentException(string.Format("Failed pulling version \"{0}\" for app \"{1}\".", newVersionKey, appKey));
                }
            }
            var folderContent = targetDirectory.EnumerateFileSystemInfos();
            if (folderContent.Count() > 0)
            {
                foreach (var item in folderContent)
                {
                    if (item.GetType() == typeof(DirectoryInfo))
                    {
                        (item as DirectoryInfo).Delete(true);
                    }
                    else
                    {
                        item.Delete();
                    }
                }
            }
            var versionsController = new Versions(config);
            versionsController.PullVersion(newVersionKey, targetDirectory);
        }

    }

    public class AppDeploymentException : Exception
    {
        public AppDeploymentException() : base() { }
        public AppDeploymentException(string message) : base(message) { }
        public AppDeploymentException(string message, Exception ex) : base(message, ex) { }
        public AppDeploymentException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

}
