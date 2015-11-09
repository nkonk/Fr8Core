﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Hosting;
using Owin;
using StructureMap;
using Data.Entities;
using Data.Interfaces;
using Data.Repositories;
using Data.States;
using Hub.Managers;
using Hub.Services;
using Utilities;
using Utilities.Configuration.Azure;
using Utilities.Logging;
using Hub.Interfaces;

[assembly: OwinStartup(typeof(HubWeb.Startup))]

namespace HubWeb
{
    public partial class Startup
    {
        public async void Configuration(IAppBuilder app)
        {
            //ConfigureDaemons();
            ConfigureAuth(app);

            await RegisterTerminalActions();

            app.Use(async (context, next) =>
            {
                if (string.Equals(context.Request.Method, HttpMethod.Post.Method, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(context.Request.Uri.AbsolutePath, "/api/DocuSignNotification", StringComparison.OrdinalIgnoreCase))
                {
                    var configRepository = ObjectFactory.GetInstance<IConfigRepository>();
                    var notificationPortForwardsCsv = configRepository.Get<string>("DocuSignNotificationPortForwards", "");
                    var notificationPortForwards = !string.IsNullOrEmpty(notificationPortForwardsCsv)
                        ? notificationPortForwardsCsv.Split(',')
                        : new string[0];

                    if (notificationPortForwards.Any())
                    {
                        using (var forwarder = new HttpClient())
                        {
                            foreach (var notificationPortForward in notificationPortForwards)
                            {
                                var response = await
                                    forwarder.PostAsync(
                                        new Uri(string.Concat("http://", notificationPortForward, context.Request.Uri.PathAndQuery)),
                                        new StreamContent(context.Request.Body));
                                Logger.GetLogger().DebugFormat("Forwarding request {0} to {1}: {2}", context.Request.Uri.PathAndQuery, notificationPortForward, response);
                            }
                        }
                    }
                }

                await next();
            });
        }

        //SeedDatabases
        //Ensure that critical configuration information is present in the database
        //Twilio SMS messages are based on CommunicationConfigurations
        //Database should have a CommunicationConfiguration that sends an SMS to 14158067915
        //Load Repository and query for CommunicationConfigurations. If null, create one set to 14158067915
        //If not null, make sure that at least one exists where the ToAddress is 14158067915
        public void ConfigureCommunicationConfigs()
        {
            IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>();
            CommunicationConfigurationRepository communicationConfigurationRepo = uow.CommunicationConfigurationRepository;
            List<CommunicationConfigurationDO> curConfigureCommunicationConfigs = communicationConfigurationRepo.GetAll().ToList();



            if (curConfigureCommunicationConfigs.Find(config => config.ToAddress == CloudConfigurationManager.GetSetting("MainSMSAlertNumber")) == null)
            // it is not true that there is at least one commConfig that has the Main alert number
            {
                CommunicationConfigurationDO curCommConfig = new CommunicationConfigurationDO();
                curCommConfig.CommunicationType = CommunicationType.Sms;
                curCommConfig.ToAddress = CloudConfigurationManager.GetSetting("MainSMSAlertNumber");
                communicationConfigurationRepo.Add(curCommConfig);
                uow.SaveChanges();
            }

        }

        public void AddMainSMSAlertToDb(CommunicationConfigurationRepository communicationConfigurationRepo)
        {

        }

        // @alexavrutin here: Daemon-related code needs to be reworked, the code below is no more actual. 

        //private static void ConfigureDaemons()
        //{
        //    DaemonSettings daemonConfig = ConfigurationManager.GetSection("daemonSettings") as DaemonSettings;
        //    if (daemonConfig != null)
        //    {
        //        if (daemonConfig.Enabled)
        //        {
        //            foreach (DaemonConfig daemon in daemonConfig.Daemons)
        //            {
        //                try
        //                {
        //                    if (daemon.Enabled)
        //                    {
        //                        Type type = Type.GetType(daemon.InitClass, true);
        //                        Daemon obj = Activator.CreateInstance(type) as Daemon;
        //                        if (obj == null)
        //                            throw new ArgumentException(
        //                                string.Format(
        //                                    "A daemon must implement IDaemon. Type '{0}' does not implement the interface.",
        //                                    type.Name));
        //                        obj.Start();
        //                    }
        //                }
        //                catch (Exception e)
        //                {
        //                    Logger.GetLogger().Error("Error initializing daemon '" + daemon.Name + "'.", e);
        //                }
        //            }
        //        }
        //    }
        //}

        public async Task RegisterTerminalActions()
        {
            List<string> totalRegisteredTerminalNames = new List<string>();
            var eventReporter = ObjectFactory.GetInstance<EventReporter>();
            var activityTemplateHosts = Utilities.FileUtils.LoadFileHostList();
            var uri = string.Empty;
            foreach (string url in activityTemplateHosts)
            {
                try
                {
                    uri = url.StartsWith("http") ? url : "http://" + url;
                    uri += "/terminals/discover";

                    var terminalService = ObjectFactory.GetInstance<ITerminal>(); ;
                    totalRegisteredTerminalNames.AddRange(await terminalService.RegisterTerminals(uri));
                }
                catch (Exception ex)
                {
                    eventReporter = ObjectFactory.GetInstance<EventReporter>();
                    eventReporter.ActivityTemplateTerminalRegistrationError(
                        string.Format("Failed terminal service: {0}. Error Message: {1} ", uri, ex.Message),
                        ex.GetType().Name);

                }
            }
            eventReporter.ActivityTemplatesSuccessfullyRegistered(totalRegisteredTerminalNames.Count);

            SetInactiveUndiscoveredActivityTemplates(totalRegisteredTerminalNames);
        }

        public void SetInactiveUndiscoveredActivityTemplates(List<string> discoveredActivityTemplateNames)
        {
            try
            {
                using (IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    var undiscoveredTemplates = uow.ActivityTemplateRepository.
                    GetQuery().
                    Where(at => !discoveredActivityTemplateNames.Contains(at.Name));

                    foreach (var activityTemplate in undiscoveredTemplates)
                    {
                        activityTemplate.ActivityTemplateState = Data.States.ActivityTemplateState.Inactive;
                    }
                    uow.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.GetLogger().Error("Error setting undiscovered activity templates inactive ", e);
            }
        }

        public void SetAllActivityTemplatesInactive()
        {
            try
            {
                using (IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    var activityTemplateList = uow.ActivityTemplateRepository.GetAll();
                    foreach(var item in activityTemplateList)
                    {
                        item.ActivityTemplateState = ActivityTemplateState.Inactive;
                    }

                    uow.SaveChanges();
                }
            }
            catch (Exception e)
            {
                Logger.GetLogger().Error("Error setting activity templates inactive ", e);
            }
        }

        public bool CheckForActivityTemplate(string templateName)
        {
            bool found = true;
            try
            {
                using (IUnitOfWork uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    ActivityTemplateRepository activityTemplateRepositary = uow.ActivityTemplateRepository;
                    List<ActivityTemplateDO> activityTemplateRepositaryItems = activityTemplateRepositary.GetAll().ToList();

                    if (!activityTemplateRepositaryItems.Any(item => item.Name == templateName))
                    {
                        found = false;
                    }

                }
            }
            catch (Exception e)
            {
                Logger.GetLogger().Error(String.Format("Error checking for activity template \"{0}\"", templateName), e);
            }
            return found;
        }

        public static IDisposable CreateServer(string url)
        {
            return WebApp.Start<Startup>(url: url);
        }
    }
}
