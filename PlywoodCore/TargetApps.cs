using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Plywood.Internal;
using Amazon.S3;

namespace Plywood
{
    public class TargetApps : ControllerBase
    {
        public const string STR_TARGET_APPS_INDEX_PATH = ".apps.index";

        public TargetApps() : base() { }
        public TargetApps(ControllerConfiguration context) : base(context) { }

        public void AddApp(Guid targetKey, Guid appKey)
        {
            try
            {
                //var indexes = new Indexes(Context);
                // We don't need the name so just set to a dash.
                //indexes.PutIndexEntry(GetTargetAppsIndexPath(targetKey), new EntityIndexEntry() { Key = appKey, Name = "-" });
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed adding app with key \"{0}\" to target \"{1}\".", appKey, targetKey), ex);
            }
        }

        public IEnumerable<Guid> GetTargetAppKeys(Guid targetKey)
        {
            try
            {
                //var indexes = new Indexes(Context);
                //var index = indexes.LoadIndex(GetTargetAppsIndexPath(targetKey));
                //return index.Entries.Select(e => e.Key);
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting app keys for target \"{0}\".", targetKey), ex);
            }
            throw new NotImplementedException();
        }

        public TargetAppList SearchTargetApps(Guid targetKey, string query = null, int offset = 0, int pageSize = 50)
        {
            try
            {
                //var indexes = new Indexes(Context);
                var targets = new Targets(Context);
                var apps = new Apps(Context);
                //var index = indexes.LoadIndex(GetTargetAppsIndexPath(targetKey));
                var target = targets.GetTarget(targetKey);
                //var groupAppIndex = indexes.LoadIndex(Apps.GetGroupAppsIndexPath(target.GroupKey));

                //var filteredIndex = index.Entries.AsQueryable();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var queryParts = query.ToLower().Split(new char[] { ' ', '\t', ',' }).Where(qp => !string.IsNullOrWhiteSpace(qp)).ToArray();
                    //filteredIndex = filteredIndex.Where(e => queryParts.Any(q => e.Name.ToLower().Contains(q)));
                }

                //var count = filteredIndex.Count();

                //var listItems = filteredIndex.Skip(offset).Take(pageSize).ToList().Select(e =>
                //    {
                //        var groupApp = groupAppIndex.Entries.FirstOrDefault(a => a.Key == e.Key);
                //        return new AppListItem()
                //        {
                //            Key = e.Key,
                //            Name = (groupApp != null) ? groupApp.Name : TryResolveAppNameDirect(e.Key, apps)
                //        };
                //    }).ToList().OrderBy(app => app.Name);

                var list = new TargetAppList()
                {
                    //Apps = listItems,
                    TargetKey = targetKey,
                    Query = query,
                    Offset = offset,
                    PageSize = pageSize,
                    //TotalCount = count,
                };

                return list;
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed getting app keys for target \"{0}\".", targetKey), ex);
            }
        }

        private string TryResolveAppNameDirect(Guid appKey, Apps apps = null, string defaultName = "[FailedResolvingName]")
        {
            if (apps == null)
            {
                apps = new Apps(Context);
            }
            try
            {
                var app = apps.GetApp(appKey);
                return app.Name;
            }
            catch (Exception) { }
            return defaultName;
        }

        public void RemoveApp(Guid targetKey, Guid appKey)
        {
            try
            {
                //var indexes = new Indexes(Context);
                //indexes.DeleteIndexEntry(GetTargetAppsIndexPath(targetKey), appKey);
            }
            catch (Exception ex)
            {
                throw new DeploymentException(string.Format("Failed removing app with key \"{0}\" from target \"{1}\".", appKey, targetKey), ex);
            }
            throw new NotImplementedException();
        }

        [Obsolete]
        public string GetTargetAppsIndexPath(Guid targetKey)
        {
            return string.Format("{0}/{1}/{2}", Targets.STR_TARGETS_CONTAINER_PATH, targetKey.ToString("N"), STR_TARGET_APPS_INDEX_PATH);
        }
    }
}
