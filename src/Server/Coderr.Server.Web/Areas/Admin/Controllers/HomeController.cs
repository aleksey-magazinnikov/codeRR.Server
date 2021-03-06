﻿using System;
using System.Web.Mvc;
using codeRR.Server.App.Configuration;
using codeRR.Server.Infrastructure;
using codeRR.Server.Infrastructure.Configuration;
using codeRR.Server.Web.Areas.Admin.Models;

namespace codeRR.Server.Web.Areas.Admin.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private IConnectionFactory _connectionFactory;

        public HomeController(IConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
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
                model.BaseUrl = Request.Url.ToString().Replace("installation/setup/basics/", "");
                ViewBag.NextLink = "";
            }


            return View(model);
        }

        [HttpPost]
        public ActionResult Basics(BasicsViewModel model)
        {
            var settings = new BaseConfiguration
            {
                BaseUrl = new Uri(model.BaseUrl),
                SupportEmail = model.SupportEmail
            };
            ConfigurationStore.Instance.Store(settings);
            return Redirect(Url.GetNextWizardStep());
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
                ViewBag.NextLink = "";

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
            WebApiApplication.ReportTocodeRR = model.ActivateTracking;
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
    }
}