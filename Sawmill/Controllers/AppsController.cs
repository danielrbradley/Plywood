using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Plywood.Sawmill.Models;

namespace Plywood.Sawmill.Controllers
{
    [Authorize]
    public class AppsController : Controller
    {
        //
        // GET: /App/

        public ActionResult Index(Guid gid, string q = null, int o = 0, int c = 50)
        {
            if (o < 0) o = 0;
            if (c < 1) o = 1;
            if (c > 100) o = 100;

            var apps = new Apps();
            var groups = new Groups();

            var appList = apps.SearchGroupApps(gid, q, o, c);
            var group = groups.GetGroup(gid);

            var model = new AppIndex()
            {
                AppList = appList,
                Group = group,
            };

            return View(model);
        }

        private static bool _defaultAppChecked = false;
        private static object _defaultAppLock = new object();
        private static App _defaultApp = null;
        public App DefaultApp
        {
            get
            {
                if (!_defaultAppChecked)
                {
                    lock (_defaultAppLock)
                    {
                        if (!_defaultAppChecked)
                        {
                            try
                            {
                                var defaultAppSetting = System.Configuration.ConfigurationManager.AppSettings["Plywood.DefaultApp"];
                                if (!string.IsNullOrEmpty(defaultAppSetting))
                                {
                                    Guid defaultAppKey;
                                    if (Guid.TryParse(defaultAppSetting, out defaultAppKey))
                                    {
                                        var apps = new Apps();
                                        _defaultApp = apps.GetApp(defaultAppKey);
                                    }
                                }
                            }
                            catch (Exception) { }
                            _defaultAppChecked = true;
                        }
                    }
                }
                return _defaultApp;
            }
        }

        public JsonResult Autocomplete(Guid gid, string query, int offset = 0, int count = 10)
        {
            var apps = new Apps();
            AppList appList;
            if (DefaultApp != null && DefaultApp.Name.ToLower().Contains(query.ToLower().Trim()))
            {
                appList = apps.SearchGroupApps(gid, query, offset, (offset > 0) ? count : count - 1);
                var appendedApps = appList.Apps.ToList();
                appendedApps.Insert(0, new AppListItem() { Key = DefaultApp.Key, Name = DefaultApp.Name });
                appList.Apps = appendedApps;
                appList.TotalCount += 1;
                if (offset == 0) appList.PageSize += 1;
            }
            else
            {
                appList = apps.SearchGroupApps(gid, query, offset, count);
            }
            return Json(appList);
        }

        public ActionResult Details(Guid id)
        {
            var apps = new Apps();
            var groups = new Groups();

            var app = apps.GetApp(id);
            var group = groups.GetGroup(app.GroupKey);

            var model = new AppDetails()
            {
                App = app,
                Group = group,
            };

            return View(model);
        }

        public ActionResult Create(Guid gid)
        {
            var groups = new Groups();
            var group = groups.GetGroup(gid);

            var model = new AppDetails()
            {
                App = new App() { GroupKey = gid },
                Group = group,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Create(Guid gid, string name, string deploymentDirectory, string majorVersion, int revision)
        {
            var newApp = new App()
            {
                GroupKey = gid,
                Name = name,
                DeploymentDirectory = deploymentDirectory,
                MajorVersion = majorVersion,
                Revision = revision,
            };

            if (string.IsNullOrWhiteSpace(name)) ModelState.AddModelError("name", "Name is required.");
            if (string.IsNullOrWhiteSpace(deploymentDirectory)) ModelState.AddModelError("deploymentDirectory", "Deployment Directory is required.");
            if (string.IsNullOrWhiteSpace(majorVersion)) ModelState.AddModelError("majorVersion", "Major version is required.");
            if (!Plywood.Utils.Validation.IsMajorVersionValid(majorVersion)) ModelState.AddModelError("majorVersion", "Major version is not valid.");
            if (revision < 0) ModelState.AddModelError("revision", "Revision must be a positive number.");

            try
            {
                if (ModelState.IsValid)
                {
                    var apps = new Apps();
                    apps.CreateApp(newApp);
                    return RedirectToAction("Details", new { id = newApp.Key });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex);
            }

            var groups = new Groups();
            var group = groups.GetGroup(gid);

            var model = new AppDetails()
            {
                App = newApp,
                Group = group,
            };

            return View(model);
        }

        public ActionResult ConfirmDelete(Guid id)
        {
            var apps = new Apps();
            var groups = new Groups();

            var app = apps.GetApp(id);
            var group = groups.GetGroup(app.GroupKey);

            var model = new AppDetails()
            {
                App = app,
                Group = group,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Delete(Guid id)
        {
            var apps = new Apps();
            var app = apps.GetApp(id);
            try
            {
                apps.DeleteApp(id);
                return RedirectToAction("Index", new { gid = app.GroupKey });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);

                var groups = new Groups();
                var group = groups.GetGroup(app.GroupKey);

                var model = new AppDetails()
                {
                    App = app,
                    Group = group,
                };

                return View("ConfirmDelete", model);
            }
        }

        public ActionResult Edit(Guid id)
        {
            var apps = new Apps();
            var groups = new Groups();

            var app = apps.GetApp(id);
            var group = groups.GetGroup(app.GroupKey);

            var model = new AppDetails()
            {
                App = app,
                Group = group,
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(Guid id, string name, string deploymentDirectory, string majorVersion, string revision)
        {
            var apps = new Apps();
            var app = apps.GetApp(id);
            int revisionNum;
            bool revisionValid = int.TryParse(revision, out revisionNum);

            app.Name = name;
            app.DeploymentDirectory = deploymentDirectory;
            app.MajorVersion = majorVersion;
            if (revisionValid)
                app.Revision = revisionNum;

            if (string.IsNullOrWhiteSpace(name)) ModelState.AddModelError("name", "Name is required.");
            if (string.IsNullOrWhiteSpace(deploymentDirectory)) ModelState.AddModelError("deploymentDirectory", "Deployment Directory is required.");
            if (string.IsNullOrWhiteSpace(majorVersion)) ModelState.AddModelError("majorVersion", "Major version is required.");
            if (!Plywood.Utils.Validation.IsMajorVersionValid(majorVersion)) ModelState.AddModelError("majorVersion", "Major version is not valid.");
            if (!revisionValid) ModelState.AddModelError("revision", "Revision must be a positive number.");
            if (revisionNum < 0) ModelState.AddModelError("revision", "Revision must be a positive number.");

            if (ModelState.IsValid)
            {
                try
                {
                    apps.UpdateApp(app);
                    return RedirectToAction("Details", new { id = app.Key });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex);
                }
            }

            var groups = new Groups();
            var group = groups.GetGroup(app.GroupKey);

            var model = new AppDetails()
            {
                App = app,
                Group = group,
            };

            return View(model);
        }

        public ActionResult EditTags(Guid id)
        {
            var apps = new Apps();
            var groups = new Groups();

            var app = apps.GetApp(id);
            var group = groups.GetGroup(app.GroupKey);

            var model = new AppDetails()
            {
                App = app,
                Group = group,
            };

            return View(model);
        }

        [HttpPost]
        public JsonResult RemoveTag(Guid id, string name)
        {
            var apps = new Apps();
            var app = apps.GetApp(id);
            if (!app.Tags.ContainsKey(name))
                throw new HttpException(404, "Tag not found");

            app.Tags.Remove(name);
            apps.UpdateApp(app);
            return Json(null);
        }

        [HttpPost]
        public ActionResult AddTag(Guid id, string name, string value)
        {
            var apps = new Apps();
            var app = apps.GetApp(id);
            if (app.Tags.ContainsKey(name))
                throw new HttpException(406, "Tag name already exists");

            app.Tags.Add(name, value);
            apps.UpdateApp(app);
            if (Request.IsAjaxRequest())
                return Json(null);
            else
                return RedirectToAction("EditTags", new { id = id });
        }

        [HttpPost]
        public JsonResult UpdateTag(Guid id, string oldName, string name, string value)
        {
            var apps = new Apps();
            var app = apps.GetApp(id);
            if (!app.Tags.ContainsKey(oldName))
                throw new HttpException(404, "Tag not found");

            if (oldName == name)
            {
                app.Tags[name] = value;
            }
            else if (app.Tags.ContainsKey(name))
            {
                throw new HttpException(406, "Tag name already exists");
            }
            else
            {
                app.Tags.Remove(oldName);
                app.Tags.Add(name, value);
            }
            apps.UpdateApp(app);
            return Json(null);
        }
    }
}
