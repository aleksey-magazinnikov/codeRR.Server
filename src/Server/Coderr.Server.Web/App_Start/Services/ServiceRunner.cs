﻿using System;
using System.Configuration.Internal;
using System.Diagnostics;
using System.IO;
using System.Linq;
using codeRR.Server.Infrastructure;
using codeRR.Server.Web.Cqs;
using codeRR.Server.Web.Infrastructure;
using DotNetCqs;
using Griffin.ApplicationServices;
using Griffin.Container;
using Griffin.Data;
using log4net;
using Owin;

namespace codeRR.Server.Web.Services
{
    /// <summary>
    ///     Used to configure the back-end. It's a mess, but a limited mess.
    /// </summary>
    public class ServiceRunner : IDisposable
    {
        private readonly CompositionRoot _compositionRoot = new CompositionRoot();
        private readonly CqsBuilder _cqsBuilder;
        private readonly ILog _log = LogManager.GetLogger(typeof(ServiceRunner));
        private readonly PluginManager _pluginManager = new PluginManager();
        private ApplicationServiceManager _appManager;
        private BackgroundJobManager _backgroundJobManager;
        private PluginConfiguration _pluginConfiguration;

        public ServiceRunner(IConnectionFactory connectionFactory)
        {
            _cqsBuilder = new CqsBuilder(connectionFactory);
        }

        public IContainer Container => CompositionRoot.Container;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start(IAppBuilder app)
        {
            WarmupPlugins();


            _log.Debug("Starting ...");
            try
            {
                _compositionRoot.Build(registrar =>
                {
                    LetPluginsRegisterServices(registrar);

                    registrar.RegisterService(x => _cqsBuilder.CreateCommandBus(Container), Lifetime.Singleton);
                    registrar.RegisterService(x => _cqsBuilder.CreateQueryBus(Container), Lifetime.Singleton);
                    registrar.RegisterService(x => _cqsBuilder.CreateEventBus(Container), Lifetime.Singleton);

                    // let us guard it since it runs events in the background.
                    var service = registrar.Registrations.First(x => x.Implements(typeof(IEventBus)));
                    service.AddService(typeof(IApplicationService));

                    registrar.RegisterService(x => _cqsBuilder.CreateRequestReplyBus(Container), Lifetime.Singleton);
                }, Startup.ConfigurationStore);

                BuildServices();
                _appManager.Start();
                _backgroundJobManager.Start();
                _log.Debug("...started");
            }
            catch (Exception exception)
            {
                _log.Error("Failed to start.", exception);
                throw;
            }
        }

        public void Stop()
        {
            _backgroundJobManager.Stop();
            _appManager.Stop();
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_backgroundJobManager != null)
            {
                _backgroundJobManager.Dispose();
                _backgroundJobManager = null;
            }
        }

        private void BuildServices()
        {
            _appManager = new ApplicationServiceManager(CompositionRoot.Container)
            {
                Settings = new ApplicationServiceManagerSettingsWithDefaultOn()
            };
            _appManager.ServiceFailed += OnServiceFailed;

            _backgroundJobManager = new BackgroundJobManager(CompositionRoot.Container);
            _backgroundJobManager.JobFailed += OnJobFailed;
            _backgroundJobManager.StartInterval = TimeSpan.FromSeconds(Debugger.IsAttached ? 0 : 10);
            _backgroundJobManager.ExecuteInterval = TimeSpan.FromMinutes(5);

            _backgroundJobManager.ScopeClosing += OnScopeClosing;
        }

        private void LetPluginsRegisterServices(ContainerRegistrar registrar)
        {
            _pluginConfiguration = new PluginConfiguration(registrar, _cqsBuilder.EventHandlerRegistry);
            _pluginManager.ConfigurePlugins(_pluginConfiguration);
        }


        private void OnJobFailed(object sender, BackgroundJobFailedEventArgs e)
        {
            _log.Error("Failed to execute " + e.Job, e.Exception);
        }


        private void OnScopeClosing(object sender, ScopeClosingEventArgs e)
        {
            try
            {
                e.Scope.Resolve<IAdoNetUnitOfWork>().SaveChanges();
            }
            catch (Exception exception)
            {
                _log.Error("Failed to close scope. Err: " + exception, e.Exception);
            }
        }

        private void OnServiceFailed(object sender, ApplicationServiceFailedEventArgs e)
        {
            _log.Error("Failed to execute " + e.ApplicationService, e.Exception);
        }

        private void WarmupPlugins()
        {
            var pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            _log.Debug("Loading from " + pluginPath);
            _pluginManager.Load(pluginPath);
            _pluginManager.PreloadPlugins();
        }
    }
}