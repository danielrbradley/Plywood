using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Plywood.Sawmill.Models
{
    public class LogsController : Controller
    {
        //
        // GET: /Logs/

        public ActionResult Index(Guid iid, string m = null, int c = 50)
        {
            var logs = new Logs();
            var instances = new Instances();
            var targets = new Targets();
            var groups = new Groups();

            var logPage = logs.GetLogEntryPage(iid, m, c);
            var instance = instances.GetInstance(iid);
            var target = targets.GetTarget(instance.TargetKey);
            var group = groups.GetGroup(target.GroupKey);

            var model = new LogIndex()
            {
                LogEntryPage = logPage,
                Instance = instance,
                Target = target,
                Group = group,
            };

            return View(model);
        }

        public ActionResult Details(Guid iid, long t, LogStatus s)
        {
            var logs = new Logs();
            var instances = new Instances();
            var targets = new Targets();
            var groups = new Groups();

            var log = logs.GetLogEntry(iid, new DateTime(t, DateTimeKind.Utc), s);
            var instance = instances.GetInstance(iid);
            var target = targets.GetTarget(instance.TargetKey);
            var group = groups.GetGroup(target.GroupKey);

            var model = new LogDetails()
            {
                LogEntry = log,
                Instance = instance,
                Target = target,
                Group = group,
            };

            return View(model);
        }
    }
}
