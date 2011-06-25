using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Plywood.Sawmill.Controllers
{
    [Authorize]
    public class GroupsController : Controller
    {
        public ActionResult Index(string q = null, int o = 0, int c = 50)
        {
            if (o < 0) o = 0;
            if (c < 1) o = 1;
            if (c > 100) o = 100;

            var groups = new Groups();
            var model = groups.SearchGroups(q, o, c);

            return View(model);
        }

        public ActionResult Details(Guid id)
        {
            var groups = new Groups();
            var model = groups.GetGroup(id);
            return View(model);
        }

        public ActionResult Create()
        {
            return View(new Group());
        }

        [HttpPost]
        public ActionResult Create(Group postedGroup)
        {
            var newGroup = new Group()
            {
                Name = postedGroup.Name,
            };

            try
            {
                var groups = new Groups();
                groups.CreateGroup(newGroup);
                return RedirectToAction("Details", new { id = newGroup.Key });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex);
            }

            return View(newGroup);
        }

        public ActionResult ConfirmDelete(Guid id)
        {
            var groups = new Groups();
            var model = groups.GetGroup(id);
            return View(model);
        }

        [HttpPost]
        public ActionResult Delete(Guid id)
        {
            var groups = new Groups();
            try
            {
                groups.DeleteGroup(id);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                var model = groups.GetGroup(id);
                return View("ConfirmDelete", model);
            }
        }

        public ActionResult Edit(Guid id)
        {
            var groups = new Groups();
            var model = groups.GetGroup(id);
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(Guid id, Group postedGroup)
        {
            var groups = new Groups();
            var model = groups.GetGroup(id);
            try
            {
                model.Name = postedGroup.Name;
                groups.UpdateGroup(model);
                return RedirectToAction("Details", new { id = model.Key });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex);
            }

            return View(model);
        }

        public ActionResult EditTags(Guid id)
        {
            var groups = new Groups();
            var model = groups.GetGroup(id);
            return View(model);
        }

        [HttpPost]
        public JsonResult RemoveTag(Guid id, string name)
        {
            var groups = new Groups();
            var group = groups.GetGroup(id);
            if (!group.Tags.ContainsKey(name))
                throw new HttpException(404, "Tag not found");

            group.Tags.Remove(name);
            groups.UpdateGroup(group);
            return Json(null);
        }

        [HttpPost]
        public ActionResult AddTag(Guid id, string name, string value)
        {
            var groups = new Groups();
            var group = groups.GetGroup(id);
            if (group.Tags.ContainsKey(name))
                throw new HttpException(406, "Tag name already exists");

            group.Tags.Add(name, value);
            groups.UpdateGroup(group);
            if (Request.IsAjaxRequest())
                return Json(null);
            else
                return RedirectToAction("EditTags", new { id = id });
        }

        [HttpPost]
        public JsonResult UpdateTag(Guid id, string oldName, string name, string value)
        {
            var groups = new Groups();
            var group = groups.GetGroup(id);
            if (!group.Tags.ContainsKey(oldName))
                throw new HttpException(404, "Tag not found");

            if (oldName == name)
            {
                group.Tags[name] = value;
            }
            else if (group.Tags.ContainsKey(name))
            {
                throw new HttpException(406, "Tag name already exists");
            }
            else
            {
                group.Tags.Remove(oldName);
                group.Tags.Add(name, value);
            }
            groups.UpdateGroup(group);
            return Json(null);
        }

    }
}
