using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.IO;

namespace Plywood.PullService
{
    public partial class PlywoodPullService : ServiceBase
    {
        System.Threading.Thread worker;
        bool stopping;
        DeploymentConfiguration config;
        bool reregister = false;

        public PlywoodPullService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                stopping = false;
                // Load configuration from registry.
                config = Utils.Registry.LoadDeploymentConfiguration();
                // Update config.
                UpdateConfig();
                // Register instance under target.
                Register();

                if (worker == null)
                {
                    worker = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadProc));
                    worker.Start();
                }
            }
            catch (Exception ex)
            {
                this.EventLog.WriteEntry(string.Format("Failed starting service : ", ex.ToString()), EventLogEntryType.Error);
            }
        }

        private void Register()
        {
            try
            {
                if (!config.InstanceKey.HasValue || reregister)
                {
                    try
                    {
                        var instances = new Instances(config);
                        Instance instance = new Instance() { TargetKey = config.TargetKey };
                        instances.CreateInstance(instance);
                        config.InstanceKey = instance.Key;
                    }
                    catch (Exception ex)
                    {
                        throw new DeploymentException("Failed registering instance.", ex);
                    }
                    try
                    {
                        Utils.Registry.Save(config);
                    }
                    catch (Exception ex)
                    {
                        throw new DeploymentException("Failed updating config after instance registration.", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                this.EventLog.WriteEntry("Failed service registration: " + ex.ToString(), EventLogEntryType.Error);
                throw;
            }
            reregister = false;
        }

        protected override void OnStop()
        {
            stopping = true;
            worker.Join(TimeSpan.FromSeconds(10));
            if (worker.ThreadState == System.Threading.ThreadState.Running)
                worker.Abort();
            worker = null;
        }

        private void ThreadProc()
        {
            try
            {
                while (!stopping)
                {
                    RunUpdate();
                    for (int i = 0; i < config.CheckFrequency.TotalSeconds; i++)
                    {
                        if (!stopping)
                            System.Threading.Thread.Sleep(TimeSpan.FromSeconds(1));
                    }
                }
            }
            catch (Exception ex)
            {
                this.EventLog.WriteEntry("Fatal exception in thread: " + ex.ToString(), EventLogEntryType.Error);
            }
        }

        private void UpdateConfig()
        {
            try
            {
                var userData = UserData.Importer.LoadInstanceLatestUserData();
                if (userData.Sections.Any(s => s.Name == "Plywood"))
                {
                    if (config == null) config = new DeploymentConfiguration() { CheckFrequency = TimeSpan.FromSeconds(30) };
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
                    {
                        Guid newTargetKey = Guid.Parse(section["TargetKey"]);
                        if (newTargetKey != config.TargetKey)
                        {
                            reregister = true;
                            config.TargetKey = Guid.Parse(section["TargetKey"]);
                        }
                    }
                    if (section.Keys.Contains("InstanceKey"))
                    {
                        Guid newInstanceKey = Guid.Parse(section["InstanceKey"]);
                        if (newInstanceKey != config.InstanceKey)
                        {
                            config.InstanceKey = Guid.Parse(section["InstanceKey"]);
                        }
                    }

                    Utils.Registry.Save(config);
                }
                else
                {
                    this.EventLog.WriteEntry("Failed updating instance plywood config from user data: Plywood section not found.", EventLogEntryType.Information);
                }
            }
            catch (Exception ex)
            {
                this.EventLog.WriteEntry("Failed updating instance plywood config from user data: " + ex.ToString(), EventLogEntryType.Warning);
            }
        }

        private void RunUpdate()
        {
            var logWriter = new StringWriter();
            var appDeployment = new AppDeployment(config, logWriter);
            appDeployment.SynchroniseAllApplications();
            logWriter.Flush();
            string logContent = logWriter.ToString();
            if (config.InstanceKey.HasValue && !string.IsNullOrWhiteSpace(logContent))
            {
                var logs = new Logs(config);
                logs.AddLogEntry(new LogEntry() { InstanceKey = config.InstanceKey.Value, Status = appDeployment.DeploymentStatus, LogContent = logContent });
            }
        }
    }
}
