﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Mvc;
using codeRR.Server.App.Configuration;
using codeRR.Server.Infrastructure;
using codeRR.Server.Infrastructure.Configuration;
using codeRR.Server.Web.Areas.Installation.Models;

namespace codeRR.Server.Web.Areas.Installation.Controllers
{
    [OutputCache(Duration = 0, NoStore = true)]
    public class SetupController : Controller
    {
        private IConnectionFactory _connectionFactory;

        public SetupController(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Activate()
        {
            ConfigurationManager.RefreshSection("appSettings");
            if (ConfigurationManager.AppSettings["Configured"] != "true")
                return RedirectToAction("Completed", new
                {
                    displayError = 1
                });
            return Redirect("~/?#/welcome");
        }

        public ActionResult Basics()
        {
            var model = new BasicsViewModel();
            var config = ConfigurationStore.Instance.Load<BaseConfiguration>();
            if (config != null)
            {
                model.BaseUrl = config.BaseUrl.ToString();
                model.SupportEmail = config.SupportEmail;
            }
            else
            {
                model.BaseUrl = Request.Url.ToString().Replace("installation/setup/basics/", "")
                    .Replace("localhost", "yourServerName");
                ViewBag.NextLink = "";
            }


            return View(model);
        }

        [HttpPost]
        public ActionResult Basics(BasicsViewModel model)
        {
            var settings = new BaseConfiguration();
            if (!model.BaseUrl.EndsWith("/"))
                model.BaseUrl += "/";

            if (model.BaseUrl.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) != -1)
            {
                ModelState.AddModelError("BaseUrl",
                    "Use the servers real DNS name instead of 'localhost'. If you don't the Ajax request wont work as CORS would be enforced by IIS.");
                return View(model);
            }
            settings.BaseUrl = new Uri(model.BaseUrl);
            settings.SupportEmail = model.SupportEmail;
            ConfigurationStore.Instance.Store(settings);
            return Redirect(Url.GetNextWizardStep());
        }


        public ActionResult Completed(string displayError = null)
        {
            ViewBag.DisplayError = displayError == "1";
            return View();
        }

        public ActionResult Errors()
        {
            var model = new ErrorTrackingViewModel();
            var config = ConfigurationStore.Instance.Load<codeRRConfigSection>();
            if (config != null)
            {
                model.ActivateTracking = config.ActivateTracking;
                model.ContactEmail = config.ContactEmail;
                model.InstallationId = config.InstallationId;
            }
            else
            {
                ViewBag.NextLink = "";
            }

            return View("ErrorTracking", model);
        }

        [HttpPost]
        public ActionResult Errors(ErrorTrackingViewModel model)
        {
            if (!ModelState.IsValid)
                return View("ErrorTracking", model);

            var settings = new codeRRConfigSection
            {
                ActivateTracking = model.ActivateTracking,
                ContactEmail = model.ContactEmail,
                InstallationId = model.InstallationId
            };
            ConfigurationStore.Instance.Store(settings);
            return Redirect(Url.GetNextWizardStep());
        }

        // GET: Installation/Home
        public ActionResult Index()
        {
            try
            {
                _connectionFactory.Open();
            }
            catch
            {
                ViewBag.Ready = false;
            }
            return View();
        }

        [HttpPost]
        public ActionResult Index(string key)
        {
            if (key == "change_this_to_your_own_password_before_running_the_installer")
            {
                ModelState.AddModelError("",
                    "Change the 'ConfigurationKey' appSetting in web.config and then try again.");
                return View();
            }

            if (key != ConfigurationManager.AppSettings["ConfigurationKey"])
            {
                ModelState.AddModelError("",
                    "Enter the value from the 'ConfigurationKey' appSetting in web.config and then try again.");
                return View();
            }

            return Redirect(Url.GetNextWizardStep());
        }

        public ActionResult Support()
        {
            return View(new SupportViewModel());
        }

        [HttpPost]
        public async Task<ActionResult> Support(SupportViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var client = new HttpClient();
                var content =
                    new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("EmailAddress", model.Email),
                        new KeyValuePair<string, string>("CompanyName", model.CompanyName)
                    });
                await client.PostAsync("https://coderrapp.com/support/register/", content);
                return Redirect(Url.GetNextWizardStep());
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.PrevLink = Url.GetPreviousWizardStepLink();
            ViewBag.NextLink = Url.GetNextWizardStepLink();
            base.OnActionExecuting(filterContext);
        }
    }
}