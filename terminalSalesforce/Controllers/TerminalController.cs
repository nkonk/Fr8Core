﻿using System;
using System.Web.Http;
using Data.Interfaces.DataTransferObjects;
using AutoMapper;
using Data.Entities;
using Newtonsoft.Json;
using System.Reflection;
using TerminalBase.BaseClasses;
using System.Collections.Generic;
using Data.States;
using Utilities.Configuration.Azure;
using System.Web.Http.Description;
using Data.Interfaces.Manifests;
using Data.Constants;

namespace terminalSalesforce.Controllers
{
     [RoutePrefix("terminals")]
    public class TerminalController : ApiController
    {
        [HttpGet]
        [Route("discover")]
        [ResponseType(typeof(StandardFr8TerminalCM))]
        public IHttpActionResult Get()
        {
            var terminal = new TerminalDTO()
            {
                Name = "terminalSalesforce",
                TerminalStatus = TerminalStatus.Active,
                Endpoint = CloudConfigurationManager.GetSetting("terminalSalesforce.TerminalEndpoint"),
                Version = "1",
                AuthenticationType = AuthenticationType.External
            };

	        var webService = new WebServiceDTO
	        {
				Name = "Salesforce"
	        };

            var saveToSalesforce = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Save_To_SalesforceDotCom",
                Label = "Save to Salesforce.Com",
                Terminal = terminal,
                NeedsAuthentication = true,
                Category = ActivityCategory.Forwarders,
                MinPaneWidth = 330,
                WebService = webService
            };

            var getDataAction = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Get_Data",
                Label = "Get Data from Salesforce",
                Terminal = terminal,
                NeedsAuthentication = true,
                Category = ActivityCategory.Receivers,
                MinPaneWidth = 330,
                WebService = webService,
                Tags = Tags.TableDataGenerator
            };

            var postToChatterAction = new ActivityTemplateDTO()
            {
                Version = "1",
                Name = "Post_To_Chatter",
                Label = "Post To Salesforce Chatter",
                Terminal = terminal,
                NeedsAuthentication = true,
                Category = ActivityCategory.Forwarders,
                MinPaneWidth = 330,
                WebService = webService
            };

            var mailMergeFromSalesforce = new ActivityTemplateDTO
            {
                Version = "1",
                Name = "Mail_Merge_From_Salesforce",
                Label = "Mail Merge from Salesforce",
                Terminal = terminal,
                NeedsAuthentication = true,
                Category = ActivityCategory.Solution,
                MinPaneWidth = 500,
                WebService = webService,
                Tags = Tags.UsesReconfigureList
            };

            var actionList = new List<ActivityTemplateDTO>()
            {
                saveToSalesforce, getDataAction, postToChatterAction, mailMergeFromSalesforce
            };

            StandardFr8TerminalCM curStandardFr8TerminalCM = new StandardFr8TerminalCM()
            {
                Definition = terminal,
                Activities = actionList
            };

            return Json(curStandardFr8TerminalCM);
        }
    }
}