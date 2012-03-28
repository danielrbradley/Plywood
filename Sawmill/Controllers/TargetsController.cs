using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Plywood.Sawmill.Models;

namespace Plywood.Sawmill.Controllers
{
    [Authorize]
    public class TargetsController : Controller
    {
        //
        // GET: /App/

        public ActionResult Index(Guid gid, string q = null, int o = 0, int c = 50)
        {
            if (o < 0) o = 0;
            if (c < 1) o = 1;
            if (c > 100) o = 100;

            var targets = new Targets();
            var groups = new Groups();

            var targetList = targets.SearchGroupTargets(gid, q, o, c);
            var group = groups.GetGroup(gid);

            var model = new TargetIndex()
            {
                TargetList = targetList,
                Group = group,
            };

            return View(model);
        }

        public ActionResult Details(Guid id)
        {
            var targets = new Targets();
            var groups = new Groups();

            var target = targets.GetTarget(id);
            var group = groups.GetGroup(target.GroupKey);

            var model = new TargetDetails()
            {
                Target = target,
                Group = group,
            };

            return View(model);
        }

        public ActionResult Create(Guid gid)
        {
            var groups = new Groups();
            var group = groups.GetGroup(gid);

            var model = new TargetDetails()
            {
                Target = new Target() { GroupKey = gid },
                Group = group,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Create(Guid gid, string name)
        {
            var newTarget = new Target()
            {
                GroupKey = gid,
                Name = name,
            };

            if (string.IsNullOrWhiteSpace(name)) ModelState.AddModelError("name", "Name is required.");

            if (ModelState.IsValid)
            {
                try
                {
                    var targets = new Targets();
                    targets.CreateTarget(newTarget);
                    try
                    {
                        var defaultAppValue = System.Configuration.ConfigurationManager.AppSettings["Plywood.DefaultApp"];
                        if (!string.IsNullOrEmpty(defaultAppValue))
                        {
                            Guid defaultApp;
                            if (Guid.TryParse(defaultAppValue, out defaultApp))
                            {
                                var targetApps = new TargetApps();
                                targetApps.AddApp(newTarget.Key, defaultApp);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // TODO: Log failing to add the toolkit to a new target.
                        // Just carry on - this isn't too important!
                    }
                    return RedirectToAction("Details", new { id = newTarget.Key });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex);
                }
            }

            var groups = new Groups();
            var group = groups.GetGroup(gid);

            var model = new TargetDetails()
            {
                Target = new Target() { GroupKey = gid },
                Group = group,
            };

            return View(model);
        }

        public ActionResult ConfirmDelete(Guid id)
        {
            var targets = new Targets();
            var groups = new Groups();

            var target = targets.GetTarget(id);
            var group = groups.GetGroup(target.GroupKey);

            var model = new TargetDetails()
            {
                Target = target,
                Group = group,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Delete(Guid id)
        {
            var targets = new Targets();
            var target = targets.GetTarget(id);
            try
            {
                targets.DeleteTarget(id);
                return RedirectToAction("Index", new { gid = target.GroupKey });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);

                var groups = new Groups();
                var group = groups.GetGroup(target.GroupKey);

                var model = new TargetDetails()
                {
                    Target = target,
                    Group = group,
                };

                return View("ConfirmDelete", model);
            }
        }

        public ActionResult Edit(Guid id)
        {
            var targets = new Targets();
            var groups = new Groups();

            var target = targets.GetTarget(id);
            var group = groups.GetGroup(target.GroupKey);

            var model = new TargetDetails()
            {
                Target = target,
                Group = group,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(Guid id, string name)
        {
            Target target = null;
            try
            {
                var targets = new Targets();
                target = targets.GetTarget(id);
                target.Name = name;
                targets.UpdateTarget(target);
                return RedirectToAction("Details", new { id = target.Key });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex);
            }

            var groups = new Groups();
            Group group = null;
            if (target != null)
                group = groups.GetGroup(target.GroupKey);

            var model = new TargetDetails()
            {
                Target = target,
                Group = group,
            };

            return View(model);
        }

        public ActionResult EditTags(Guid id)
        {
            var targets = new Targets();
            var groups = new Groups();

            var target = targets.GetTarget(id);
            var group = groups.GetGroup(target.GroupKey);

            var model = new TargetDetails()
            {
                Target = target,
                Group = group,
            };

            return View(model);
        }

        [HttpPost]
        public JsonResult RemoveTag(Guid id, string name)
        {
            var targets = new Targets();
            var target = targets.GetTarget(id);
            if (!target.Tags.ContainsKey(name))
                throw new HttpException(404, "Tag not found");

            target.Tags.Remove(name);
            targets.UpdateTarget(target);
            return Json(null);
        }

        [HttpPost]
        public ActionResult AddTag(Guid id, string name, string value)
        {
            var targets = new Targets();
            var target = targets.GetTarget(id);
            if (target.Tags.ContainsKey(name))
                throw new HttpException(406, "Tag name already exists");

            target.Tags.Add(name, value);
            targets.UpdateTarget(target);
            if (Request.IsAjaxRequest())
                return Json(null);
            else
                return RedirectToAction("EditTags", new { id = id });
        }

        [HttpPost]
        public JsonResult UpdateTag(Guid id, string oldName, string name, string value)
        {
            var targets = new Targets();
            var target = targets.GetTarget(id);
            if (!target.Tags.ContainsKey(oldName))
                throw new HttpException(404, "Tag not found");

            if (oldName == name)
            {
                target.Tags[name] = value;
            }
            else if (target.Tags.ContainsKey(name))
            {
                throw new HttpException(406, "Tag name already exists");
            }
            else
            {
                target.Tags.Remove(oldName);
                target.Tags.Add(name, value);
            }
            targets.UpdateTarget(target);
            return Json(null);
        }
    }
}
