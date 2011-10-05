﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Indexes;

namespace Plywood
{
    public class RolePackages : ControllerBase
    {
        public RolePackages(IStorageProvider provider) : base(provider) { }

        public void AddApp(Guid targetKey, Guid appKey)
        {
            try
            {
                var targets = new Roles(StorageProvider);
                var apps = new Packages(StorageProvider);
                var target = targets.Get(targetKey);
                var app = apps.Get(appKey);

                var indexes = new Indexes.IndexEntries(StorageProvider);
                var targetApp = new RolePackage()
                {
                    TargetKey = target.Key,
                    TargetName = target.Name,
                    AppKey = app.Key,
                    AppName = app.Name,
                    AppDeploymentDirectory = app.DeploymentDirectory,
                };

                indexes.PutEntity(targetApp);
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed adding app with key \"{0}\" to target \"{1}\".", appKey, targetKey), ex);
            }
        }

        public void AddApp(RolePackage targetApp)
        {
            try
            {
                var indexes = new Indexes.IndexEntries(StorageProvider);
                indexes.PutEntity(targetApp);
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed adding app with key \"{0}\" to target \"{1}\".", targetApp.AppKey, targetApp.TargetKey), ex);
            }
        }

        public RolePackageList Search(Guid targetKey, string query = null, string marker = null, int pageSize = 50)
        {
            if (pageSize < 0)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be less than 0.");
            if (pageSize > 100)
                throw new ArgumentOutOfRangeException("pageSize", "Page size cannot be greater than 100.");

            try
            {
                var startLocation = string.Format("t/{0}/ai", targetKey.ToString("N"));

                var basePaths = new List<string>();
                if (string.IsNullOrWhiteSpace(query))
                {
                    basePaths.Add(string.Format("{0}/e", startLocation));
                }
                else
                {
                    basePaths.AddRange(
                        new SimpleTokeniser().Tokenise(query).Select(
                            token =>
                                string.Format("{0}/t/{1}", startLocation, Indexes.IndexEntries.GetTokenHash(token))));
                }

                var indexEntries = new IndexEntries(StorageProvider);
                var rawResults = indexEntries.PerformRawQuery(pageSize, marker, basePaths);

                var apps = rawResults.FileNames.Select(fileName => new RolePackageListItem(fileName));

                var list = new RolePackageList()
                {
                    Packages = apps,
                    RoleKey = targetKey,
                    Query = query,
                    Marker = marker,
                    PageSize = pageSize,
                    IsTruncated = rawResults.IsTruncated,
                    NextMarker = apps.Any() ? apps.Last().Marker : marker,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting app keys for target \"{0}\".", targetKey), ex);
            }
        }

        public void RemoveApp(Guid targetKey, Guid appKey)
        {
            try
            {
                var targets = new Roles(StorageProvider);
                var apps = new Packages(StorageProvider);
                var target = targets.Get(targetKey);
                var app = apps.Get(appKey);

                var targetApp = new RolePackage()
                {
                    TargetKey = target.Key,
                    TargetName = target.Name,
                    AppKey = app.Key,
                    AppName = app.Name,
                    AppDeploymentDirectory = app.DeploymentDirectory,
                };

                var indexes = new IndexEntries(StorageProvider);
                indexes.DeleteEntity(targetApp);
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed removing app with key \"{0}\" from target \"{1}\".", appKey, targetKey), ex);
            }
            throw new NotImplementedException();
        }

        public void RemoveApp(RolePackage targetApp)
        {
            try
            {
                var indexes = new IndexEntries(StorageProvider);
                indexes.DeleteEntity(targetApp);
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed removing app with key \"{0}\" from target \"{1}\".", targetApp.AppKey, targetApp.TargetKey), ex);
            }
        }
    }
}