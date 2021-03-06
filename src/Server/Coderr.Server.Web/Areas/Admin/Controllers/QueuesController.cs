﻿using System.Web.Mvc;
using codeRR.Server.Infrastructure;
using codeRR.Server.Infrastructure.Configuration;
using codeRR.Server.Infrastructure.Queueing;
using codeRR.Server.Infrastructure.Queueing.Msmq;
using codeRR.Server.Web.Areas.Admin.Models;

namespace codeRR.Server.Web.Areas.Admin.Controllers
{
    [Authorize]
    public class QueuesController : Controller
    {
        public ActionResult Index()
        {
            var model = new QueueViewModel();

            var settings = ConfigurationStore.Instance.Load<MessageQueueSettings>();
            if (settings == null)
            {
                model.UseSql = true;
                return View(model);
            }

            model.UseSql = settings.UseSql;
            model.EventQueue = settings.EventQueue;
            model.EventAuthentication = settings.EventAuthentication;
            model.EventTransactions = settings.EventTransactions;
            model.EventQueue = settings.ReportQueue;
            model.ReportTransactions = settings.ReportTransactions;
            model.ReportAuthentication = settings.ReportAuthentication;
            model.FeedbackQueue = settings.FeedbackQueue;
            model.FeedbackAuthentication = settings.FeedbackAuthentication;
            model.FeedbackTransactions = settings.FeedbackTransactions;

            return View(model);
        }


        [HttpPost]
        public ActionResult Index(QueueViewModel model)
        {
            var config = new MessageQueueSettings();
            if (model.UseSql)
            {
                config.UseSql = true;
                ConfigurationStore.Instance.Store(config);
                return Redirect(Url.GetNextWizardStep());
            }

            if (
                !MsMqTools.ValidateMessageQueue(model.ReportQueue, model.ReportAuthentication, model.ReportTransactions,
                    out var errorMessage))
            {
                ModelState.AddModelError("ReportQueue", errorMessage);
            }
            if (
                !MsMqTools.ValidateMessageQueue(model.FeedbackQueue, model.FeedbackAuthentication,
                    model.FeedbackTransactions, out errorMessage))
            {
                ModelState.AddModelError("FeedbackQueue", errorMessage);
            }
            if (
                !MsMqTools.ValidateMessageQueue(model.EventQueue, model.EventAuthentication, model.EventTransactions,
                    out errorMessage))
            {
                ModelState.AddModelError("EventQueue", errorMessage);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            config.EventQueue = model.EventQueue;
            config.EventAuthentication = model.EventAuthentication;
            config.EventTransactions = model.EventTransactions;
            config.EventQueue = model.ReportQueue;
            config.ReportTransactions = model.ReportTransactions;
            config.ReportAuthentication = model.ReportAuthentication;
            config.FeedbackQueue = model.FeedbackQueue;
            config.FeedbackAuthentication = model.FeedbackAuthentication;
            config.FeedbackTransactions = model.FeedbackTransactions;
            ConfigurationStore.Instance.Store(config);

            return Redirect(Url.GetNextWizardStep());
        }
    }
}