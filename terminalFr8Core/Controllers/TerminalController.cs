﻿using System.Collections.Generic;
using System.Web.Http.Description;
using System.Web.Http;
using Data.Entities;
using Data.States;
using Hub.Services;
using Utilities.Configuration.Azure;
using Data.Interfaces.Manifests;

namespace terminalFr8Core.Controllers
{

    [RoutePrefix("terminals")]
    public class TerminalController : ApiController
    {
        /// <summary>
        /// Terminal discovery infrastructure.
        /// Action returns list of supported actions by terminal.

        /// </summary>
        [HttpGet]
        [Route("discover")]
        [ResponseType(typeof(StandardFr8TerminalCM))]

        public IHttpActionResult DiscoverTerminals()
        {
            var result = new List<ActivityTemplateDO>();
            
            var terminal = new TerminalDO
            {
                Endpoint = CloudConfigurationManager.GetSetting("TerminalEndpoint"),
                TerminalStatus = TerminalStatus.Active,
                Name = "terminalFr8Core",
                Version = "1"
            };

	        var webService = new WebServiceDO
	        {
		        Name = "fr8 Core"
	        };

            result.Add(new ActivityTemplateDO
            {
                Name = "FilterUsingRunTimeData",
                Label = "Filter Using Runtime Data",
                Category = ActivityCategory.Processors,
                Terminal = terminal,

                AuthenticationType = AuthenticationType.None,
                Version = "1",
				MinPaneWidth = 330,
				WebService = webService
            });

            result.Add(new ActivityTemplateDO
            {
                Name = "MapFields",
                Label = "Map Fields",
                Category = ActivityCategory.Processors,
                Terminal = terminal,

                AuthenticationType = AuthenticationType.None,
                Version = "1",
				MinPaneWidth = 380,
				WebService = webService
            });

            result.Add(new ActivityTemplateDO
            {
                Name = "AddPayloadManually",
                Label = "Add Payload Manually",
                Category = ActivityCategory.Processors,
                Terminal = terminal,

                AuthenticationType = AuthenticationType.None,
                Version = "1",
				MinPaneWidth = 330,
				WebService = webService
            });

            result.Add(new ActivityTemplateDO
            {
                Name = "StoreMTData",
                Label = "Store MT Data",
                Category = ActivityCategory.Processors,
                Terminal = terminal,
                WebService = webService,
                Version = "1"
            });

            result.Add(new ActivityTemplateDO
            {
                Name = "Select_Fr8_Object",
                Label = "Select Fr8 Object",
                Category = ActivityCategory.Processors,
                Terminal = terminal,
                WebService = webService,
                Version = "1",
                MinPaneWidth = 330
            });

            result.Add(new ActivityTemplateDO
            {
                Name = "ConnectToSql",
                Label = "Connect To SQL",
                Category = ActivityCategory.Processors,
                Terminal = terminal,
                WebService = webService,
                Version = "1"
            });

            result.Add(new ActivityTemplateDO
            {
                Name = "BuildQuery",
                Label = "Build Query",
                Category = ActivityCategory.Processors,
                Terminal = terminal,

                Version = "1"
            });

            result.Add(new ActivityTemplateDO
            {
                Name = "ExecuteSql",
                Label = "Execute Sql Query",
                Category = ActivityCategory.Processors,
                Terminal = terminal,
                Version = "1"
            });

            result.Add(new ActivityTemplateDO
            {
                Name = "ManageRoute",
                Label = "Manage Route",
                Category = ActivityCategory.Processors,
                Terminal = terminal,
                Version = "1"
            });

            var curStandardFr8TerminalCM = new StandardFr8TerminalCM()
            {
                Definition = terminal,
                Actions = result
            };

            return Json(curStandardFr8TerminalCM);
        }
    }
}