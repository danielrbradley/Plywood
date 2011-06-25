using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Plywood.Sawmill.Models;

namespace Plywood.Sawmill.Controllers
{
    [Authorize]
    public class TargetAppsController : Controller
    {
        //
        // GET: /TargetApps/

        public ActionResult Index(Guid tid, string q = null, int o = 0, int c = 50)
        {
            var targetApps = new TargetApps();
            var targets = new Targets();
            var groups = new Groups();

            var targetAppList = targetApps.SearchTargetApps(tid, q, o, c);
            var target = targets.GetTarget(tid);
            var group = groups.GetGroup(target.GroupKey);

            var model = new TargetAppsIndex()
            {
                TargetAppList = targetAppList,
                Target = target,
                Group = group,
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult AddApp(Guid tid, Guid aid)
        {
            var targetApps = new TargetApps();
            targetApps.AddApp(tid, aid);
            return RedirectToAction("Index", new { tid = tid });
        }

        [HttpPost]
        public ActionResult RemoveApp(Guid tid, Guid aid)
        {
            var targetApps = new TargetApps();
            targetApps.RemoveApp(tid, aid);
            return RedirectToAction("Index", new { tid = tid });
        }

        public ActionResult Version(Guid tid, Guid aid)
        {
            var targetAppVersions = new TargetAppVersions();
            var versions = new Versions();
            var apps = new Apps();
            var targets = new Targets();
            var groups = new Groups();

            Version version;
            var versionKey = targetAppVersions.GetTargetAppVersion(tid, aid);
            if (versionKey.HasValue)
            {
                try
                {
                    version = versions.GetVersion(versionKey.Value);
                }
                catch (Exception)
                {
                    version = null;
                }
            }
            else
            {
                version = null;
            }

            var app = apps.GetApp(aid);
            var target = targets.GetTarget(tid);
            var group = groups.GetGroup(target.GroupKey);

            var model = new TargetAppVersionDetails()
            {
                Version = version,
                App = app,
                Target = target,
                Group = group,
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult ChangeVersion(Guid tid, Guid aid, Guid vid)
        {
            var targetAppVersions = new TargetAppVersions();
            targetAppVersions.SetTargetAppVersion(tid, aid, vid);
            return RedirectToAction("Version", new { tid = tid, aid = aid });
        }

        [HttpPost]
        public ActionResult RemoveVersion(Guid tid, Guid aid)
        {
            var targetAppVersions = new TargetAppVersions();
            targetAppVersions.SetTargetAppVersion(tid, aid, null);
            return RedirectToAction("Version", new { tid = tid, aid = aid });
        }
    }
}
