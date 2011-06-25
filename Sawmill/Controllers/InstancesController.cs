using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Plywood.Sawmill.Models;

namespace Plywood.Sawmill.Controllers
{
    public class InstancesController : Controller
    {
        //
        // GET: /Instances/

        public ActionResult Index(Guid tid, string q = null, int o = 0, int c = 50)
        {
            if (o < 0) o = 0;
            if (c < 1) o = 1;
            if (c > 100) o = 100;

            var instances = new Instances();
            var targets = new Targets();
            var groups = new Groups();

            var instanceList = instances.SearchInstances(tid, q, o, c);
            var target = targets.GetTarget(tid);
            var group = groups.GetGroup(target.GroupKey);

            var model = new InstanceIndex()
            {
                InstanceList = instanceList,
                Target = target,
                Group = group,
            };

            return View(model);
        }

        //
        // GET: /Instances/Details/5

        public ActionResult Details(Guid id)
        {
            var instances = new Instances();
            var targets = new Targets();
            var groups = new Groups();

            var instance = instances.GetInstance(id);
            var target = targets.GetTarget(instance.TargetKey);
            var group = groups.GetGroup(target.GroupKey);

            var model = new InstanceDetails()
            {
                Instance = instance,
                Target = target,
                Group = group,
            };

            return View(model);
        }

        //
        // GET: /Instances/Create

        public ActionResult Create(Guid tid)
        {
            var targets = new Targets();
            var groups = new Groups();

            var target = targets.GetTarget(tid);
            var group = groups.GetGroup(target.GroupKey);

            var model = new InstanceDetails()
            {
                Instance = new Instance(),
                Target = target,
                Group = group,
            };

            return View(model);
        }

        //
        // POST: /Instances/Create

        [HttpPost]
        public ActionResult Create(Guid tid, string name)
        {
            var newInstance = new Instance()
            {
                Name = name,
                TargetKey = tid,
            };

            if (string.IsNullOrWhiteSpace(name)) ModelState.AddModelError("name", "Name is required.");

            if (ModelState.IsValid)
            {
                try
                {
                    var instances = new Instances();
                    instances.CreateInstance(newInstance);

                    return RedirectToAction("Details", new { id = newInstance.Key });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex);
                }
            }

            var targets = new Targets();
            var groups = new Groups();

            var target = targets.GetTarget(tid);
            var group = groups.GetGroup(target.GroupKey);

            var model = new InstanceDetails()
            {
                Instance = newInstance,
                Target = target,
                Group = group,
            };

            return View(model);
        }

        //
        // GET: /Instances/Edit/5

        public ActionResult Edit(Guid id)
        {
            var instances = new Instances();
            var targets = new Targets();
            var groups = new Groups();

            var instance = instances.GetInstance(id);
            var target = targets.GetTarget(instance.TargetKey);
            var group = groups.GetGroup(target.GroupKey);

            var model = new InstanceDetails()
            {
                Instance = instance,
                Target = target,
                Group = group,
            };

            return View(model);
        }

        //
        // POST: /Instances/Edit/5

        [HttpPost]
        public ActionResult Edit(Guid id, string name)
        {
            var instances = new Instances();
            var instance = instances.GetInstance(id);
            instance.Name = name;

            if (string.IsNullOrWhiteSpace(name)) ModelState.AddModelError("name", "Name is required.");

            if (ModelState.IsValid)
            {
                try
                {
                    instances.UpdateInstance(instance);

                    return RedirectToAction("Details", new { id = instance.Key });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex);
                }
            }

            var targets = new Targets();
            var groups = new Groups();

            var target = targets.GetTarget(instance.TargetKey);
            var group = groups.GetGroup(target.GroupKey);

            var model = new InstanceDetails()
            {
                Instance = instance,
                Target = target,
                Group = group,
            };

            return View(model);
        }

        //
        // GET: /Instances/ConfirmDelete/5

        public ActionResult ConfirmDelete(Guid id)
        {
            var instances = new Instances();
            var targets = new Targets();
            var groups = new Groups();

            var instance = instances.GetInstance(id);
            var target = targets.GetTarget(instance.TargetKey);
            var group = groups.GetGroup(target.GroupKey);

            var model = new InstanceDetails()
            {
                Instance = instance,
                Target = target,
                Group = group,
            };

            return View(model);
        }

        //
        // POST: /Instances/Delete/5

        [HttpPost]
        public ActionResult Delete(Guid id, FormCollection collection)
        {
            var instances = new Instances();
            var instance = instances.GetInstance(id);
            try
            {
                instances.DeleteInstance(id);

                return RedirectToAction("Index", new { tid = instance.TargetKey });
            }
            catch(Exception ex)
            {
                ModelState.AddModelError("Error", ex);
            }

            var targets = new Targets();
            var groups = new Groups();

            var target = targets.GetTarget(instance.TargetKey);
            var group = groups.GetGroup(target.GroupKey);

            var model = new InstanceDetails()
            {
                Instance = instance,
                Target = target,
                Group = group,
            };

            return View("ConfirmDelete", model);
        }

        public ActionResult EditTags(Guid id)
        {
            var instances = new Instances();
            var targets = new Targets();
            var groups = new Groups();

            var instance = instances.GetInstance(id);
            var target = targets.GetTarget(instance.TargetKey);
            var group = groups.GetGroup(target.GroupKey);

            var model = new InstanceDetails()
            {
                Instance = instance,
                Target = target,
                Group = group,
            };

            return View(model);
        }

        [HttpPost]
        public JsonResult RemoveTag(Guid id, string name)
        {
            var instances = new Instances();
            var instance = instances.GetInstance(id);
            if (!instance.Tags.ContainsKey(name))
                throw new HttpException(404, "Tag not found");

            instance.Tags.Remove(name);
            instances.UpdateInstance(instance);
            return Json(null);
        }

        [HttpPost]
        public ActionResult AddTag(Guid id, string name, string value)
        {
            var instances = new Instances();
            var instance = instances.GetInstance(id);
            if (instance.Tags.ContainsKey(name))
                throw new HttpException(406, "Tag name already exists");

            instance.Tags.Add(name, value);
            instances.UpdateInstance(instance);
            if (Request.IsAjaxRequest())
                return Json(null);
            else
                return RedirectToAction("EditTags", new { id = id });
        }

        [HttpPost]
        public JsonResult UpdateTag(Guid id, string oldName, string name, string value)
        {
            var instances = new Instances();
            var instance = instances.GetInstance(id);
            if (!instance.Tags.ContainsKey(oldName))
                throw new HttpException(404, "Tag not found");

            if (oldName == name)
            {
                instance.Tags[name] = value;
            }
            else if (instance.Tags.ContainsKey(name))
            {
                throw new HttpException(406, "Tag name already exists");
            }
            else
            {
                instance.Tags.Remove(oldName);
                instance.Tags.Add(name, value);
            }
            instances.UpdateInstance(instance);
            return Json(null);
        }
    }
}
