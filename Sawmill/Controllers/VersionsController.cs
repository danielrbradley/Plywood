using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Plywood.Sawmill.Models;

namespace Plywood.Sawmill.Controllers
{
    [Authorize]
    public class VersionsController : Controller
    {
        //
        // GET: /Versions/

        public ActionResult Index(Guid aid, string q = null, DateTime? f = null, DateTime? t = null, int o = 0, int c = 50)
        {
            if (o < 0) o = 0;
            if (c < 1) o = 1;
            if (c > 100) o = 100;

            var versions = new Versions();
            var apps = new Apps();
            var groups = new Groups();

            var versionList = versions.SearchAppVersions(aid, f, t, q, o, c);
            var app = apps.GetApp(aid);
            var group = groups.GetGroup(app.GroupKey);

            var model = new VersionIndex()
            {
                VersionList = versionList,
                App = app,
                Group = group,
            };
            return View(model);
        }

        public ActionResult Autocomplete(Guid aid, string query, int offset = 0, int count = 10)
        {
            var versions = new Versions();
            var versionList = versions.SearchAppVersions(aid, null, null, query, offset, count);
            return Json(versionList);
        }

        public ActionResult Details(Guid id)
        {
            var versions = new Versions();
            var apps = new Apps();
            var groups = new Groups();

            var version = versions.GetVersion(id);
            var app = apps.GetApp(version.AppKey);
            var group = groups.GetGroup(app.GroupKey);

            var model = new VersionDetails()
            {
                Version = version,
                App = app,
                Group = group,
            };
            return View(model);
        }

        public ActionResult Create(Guid aid)
        {
            var apps = new Apps();
            var groups = new Groups();

            var app = apps.GetApp(aid);
            var group = groups.GetGroup(app.GroupKey);
            var version = new Version() { AppKey = aid, GroupKey = group.Key };

            var model = new VersionDetails()
            {
                Version = version,
                App = app,
                Group = group,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Create(Guid aid, string name, DateTime timestamp)
        {
            var newVersion = new Version()
            {
                AppKey = aid,
                Name = name,
                Timestamp = timestamp
            };

            if (string.IsNullOrWhiteSpace(name)) ModelState.AddModelError("name", "Name is required.");
            if (ModelState.IsValid)
            {
                try
                {
                    var versions = new Versions();
                    versions.CreateVersion(newVersion);
                    return RedirectToAction("Details", new { id = newVersion.Key });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex);
                }
            }

            var apps = new Apps();
            var groups = new Groups();

            var app = apps.GetApp(aid);
            var group = groups.GetGroup(app.GroupKey);

            var model = new VersionDetails()
            {
                Version = newVersion,
                App = app,
                Group = group,
            };

            return View(model);
        }

        public ActionResult ConfirmDelete(Guid id)
        {
            var versions = new Versions();
            var apps = new Apps();
            var groups = new Groups();

            var version = versions.GetVersion(id);
            var app = apps.GetApp(version.AppKey);
            var group = groups.GetGroup(app.GroupKey);

            var model = new VersionDetails()
            {
                Version = version,
                App = app,
                Group = group,
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult Delete(Guid id)
        {
            var versions = new Versions();
            var version = versions.GetVersion(id);

            try
            {
                versions.DeleteVersion(id);
                return RedirectToAction("Index", new { aid = version.AppKey });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);

                var apps = new Apps();
                var app = apps.GetApp(version.AppKey);

                var groups = new Groups();
                var group = groups.GetGroup(version.GroupKey);

                var model = new VersionDetails()
                {
                    Version = version,
                    App = app,
                    Group = group,
                };

                return View("ConfirmDelete", model);
            }
        }

        public ActionResult Edit(Guid id)
        {
            var versions = new Versions();
            var apps = new Apps();
            var groups = new Groups();

            var version = versions.GetVersion(id);
            var app = apps.GetApp(version.AppKey);
            var group = groups.GetGroup(app.GroupKey);

            var model = new VersionDetails()
            {
                Version = version,
                App = app,
                Group = group,
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(Guid id, string name)
        {
            Version version = null;
            try
            {
                var versions = new Versions();
                version = versions.GetVersion(id);
                version.Name = name;
                versions.UpdateVersion(version);
                return RedirectToAction("Details", new { id = version.Key });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex);
            }

            var groups = new Groups();
            var apps = new Apps();
            Group group = null;
            App app = null;
            if (version != null)
            {
                group = groups.GetGroup(version.GroupKey);
                app = apps.GetApp(version.AppKey);
            }

            var model = new VersionDetails()
            {
                Version = version,
                App = app,
                Group = group,
            };

            return View(model);
        }

        public ActionResult EditTags(Guid id)
        {
            var versions = new Versions();
            var apps = new Apps();
            var groups = new Groups();

            var version = versions.GetVersion(id);
            var app = apps.GetApp(version.AppKey);
            var group = groups.GetGroup(app.GroupKey);

            var model = new VersionDetails()
            {
                Version = version,
                App = app,
                Group = group,
            };
            return View(model);
        }

        [HttpPost]
        public JsonResult RemoveTag(Guid id, string name)
        {
            var versions = new Versions();
            var version = versions.GetVersion(id);
            if (!version.Tags.ContainsKey(name))
                throw new HttpException(404, "Tag not found");

            version.Tags.Remove(name);
            versions.UpdateVersion(version);
            return Json(null);
        }

        [HttpPost]
        public ActionResult AddTag(Guid id, string name, string value)
        {
            var versions = new Versions();
            var version = versions.GetVersion(id);
            if (version.Tags.ContainsKey(name))
                throw new HttpException(406, "Tag name already exists");

            version.Tags.Add(name, value);
            versions.UpdateVersion(version);
            if (Request.IsAjaxRequest())
                return Json(null);
            else
                return RedirectToAction("EditTags", new { id = id });
        }

        [HttpPost]
        public JsonResult UpdateTag(Guid id, string oldName, string name, string value)
        {
            var versions = new Versions();
            var version = versions.GetVersion(id);
            if (!version.Tags.ContainsKey(oldName))
                throw new HttpException(404, "Tag not found");

            if (oldName == name)
            {
                version.Tags[name] = value;
            }
            else if (version.Tags.ContainsKey(name))
            {
                throw new HttpException(406, "Tag name already exists");
            }
            else
            {
                version.Tags.Remove(oldName);
                version.Tags.Add(name, value);
            }
            versions.UpdateVersion(version);
            return Json(null);
        }
    }
}
