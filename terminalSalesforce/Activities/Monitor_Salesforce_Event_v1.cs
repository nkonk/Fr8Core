﻿using System.Collections.Generic;
using TerminalBase.BaseClasses;
using System.Threading.Tasks;
using terminalSalesforce.Infrastructure;
using StructureMap;
using System.Linq;
using Fr8Data.Constants;
using Fr8Data.Control;
using Fr8Data.Crates;
using Fr8Data.DataTransferObjects;
using Fr8Data.Managers;
using Fr8Data.Manifests;
using Fr8Data.States;
using ServiceStack;
using TerminalBase.Infrastructure;

namespace terminalSalesforce.Actions
{
    public class Monitor_Salesforce_Event_v1 : BaseSalesforceTerminalActivity<Monitor_Salesforce_Event_v1.ActivityUi>
    {
        public static ActivityTemplateDTO ActivityTemplateDTO = new ActivityTemplateDTO
        {
            Version = "1",
            Name = "Monitor_Salesforce_Event",
            Label = "Monitor Salesforce Events",
            NeedsAuthentication = true,
            Category = ActivityCategory.Monitors,
            MinPaneWidth = 330,
            WebService = TerminalData.WebServiceDTO,
            Terminal = TerminalData.TerminalDTO
        };
        protected override ActivityTemplateDTO MyTemplate => ActivityTemplateDTO;

        private const string CreatedEventname = "Created";
        private const string UpdatedEventname = "Updated";

        public class ActivityUi : StandardConfigurationControlsCM
        {
            public DropDownList SalesforceObjectList { get; set; }

            public TextBlock EventDescription { get; set; }

            public CheckBox Created { get; set; }

            public CheckBox Updated { get; set; }

            public ActivityUi()
            {
                SalesforceObjectList = new DropDownList
                {
                    Label = "Which object do you want to monitor?",
                    Name = nameof(SalesforceObjectList),
                    Required = true,
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig }
                };
                Controls.Add(SalesforceObjectList);

                EventDescription = new TextBlock()
                {
                    Label = "Detect objects that have been: ",
                    Name = nameof(EventDescription)
                };
                Controls.Add(EventDescription);

                Created = new CheckBox
                {
                    Label = "Created",
                    Name = nameof(Created),
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig },
                    Selected = false
                };
                Controls.Add(Created);

                Updated = new CheckBox
                {
                    Label = "Updated",
                    Name = nameof(Updated),
                    Events = new List<ControlEvent> { ControlEvent.RequestConfig },
                    Selected = false
                };
                Controls.Add(Updated);
            }
        }

        readonly ISalesforceManager _salesforceManager;

        public Monitor_Salesforce_Event_v1(ICrateManager crateManager, ISalesforceManager salesforceManager)
            : base(crateManager)
        {
            _salesforceManager = salesforceManager;
        }

        public override Task Initialize()
        {
            ActivitiesHelper.GetAvailableFields(ActivityUI.SalesforceObjectList);
            return Task.FromResult(0);
        }

        public override async Task FollowUp()
        {
            string curSfChosenObject = ActivityUI.SalesforceObjectList.selectedKey;

            if(string.IsNullOrEmpty(curSfChosenObject))
            {
                return;
            }
            var eventSubscriptionCrate = PackEventSubscriptionsCrate();
            Storage.ReplaceByLabel(eventSubscriptionCrate);
            CrateSignaller.ClearAvailableCrates();
            CrateSignaller.MarkAvailableAtRuntime<SalesforceEventCM>("Salesforce Event");
            var selectedObjectProperties = await _salesforceManager.GetProperties(curSfChosenObject.ToEnum<SalesforceObjectType>(), AuthorizationToken);
            CrateSignaller.MarkAvailableAtRuntime<StandardTableDataCM>(GenerateRuntimeDataLabel(), true).AddFields(selectedObjectProperties);
        }

        public override async Task Run()
        {
            //get the event payload from the Salesforce notification event
            var sfEventPayloads = Payload.CratesOfType<EventReportCM>().ToList().SelectMany(er => er.Content.EventPayload).ToList();
            
            if (sfEventPayloads.Count == 0 || 
                !sfEventPayloads.Any(payload => payload.Label.Equals("Salesforce Event Notification Payload")))
            {
                RequestHubExecutionTermination("External event data is missing");
                return;
            }

            //if payload contains Salesforce Notification, get it from the payload storage
            var curEventReport = Payload.CratesOfType<EventReportCM>().Single(er => er.Content.Manufacturer.Equals("Salesforce")).Content;
            var curEventPayloads = curEventReport.EventPayload.CrateContentsOfType<StandardPayloadDataCM>().Single().PayloadObjects;

            //for each payload,
            curEventPayloads.ForEach(p =>
            {
                //create SalesforceEventCM with the values. The values are:
                //Object ID          -> Id of the newly created or updated Salesforce Object
                //Object Type        -> Type of the newly created or updated Salesforce Object (ex., Lead, Account or Contact
                //Occured Event      -> Cause of this notification (ex., Create or Updated)
                //Created Date       -> Date at which the object is created
                //LastModified Date  -> Date at which the object is last modified
                var sfEvent = new SalesforceEventCM
                {
                    ObjectId = p.PayloadObject.Single(requiredProperty => requiredProperty.Key.Equals("ObjectId")).Value,
                    ObjectType = p.PayloadObject.Single(requiredProperty => requiredProperty.Key.Equals("ObjectType")).Value,
                    OccuredEvent = p.PayloadObject.Single(requiredProperty => requiredProperty.Key.Equals("OccuredEvent")).Value,
                    CreatedDate = p.PayloadObject.Single(requiredProperty => requiredProperty.Key.Equals("CreatedDate")).Value,
                    LastModifiedDate = p.PayloadObject.Single(requiredProperty => requiredProperty.Key.Equals("LastModifiedDate")).Value                    
                };

                //store the SalesforceEventCM into the current payload
                Payload.ReplaceByLabel(Crate.FromContent("Salesforce Event", sfEvent));
            });

            var runtimeCrateLabel = GenerateRuntimeDataLabel();

            var salesforceObjectFields = Storage.FirstCrate<CrateDescriptionCM>().Content.CrateDescriptions.First(x => x.Label == runtimeCrateLabel).Fields.Select(x => x.Key).ToArray();

            //for each Salesforce event notification
            var sfEventsList = Payload.CrateContentsOfType<SalesforceEventCM>().ToList();
            foreach (var sfEvent in sfEventsList)
            {
                //get the object fields as Standard Table Data
                var resultObjects = await _salesforceManager.Query(
                                                sfEvent.ObjectType.ToEnum<SalesforceObjectType>(),
                                                salesforceObjectFields, 
                                                $"ID = '{sfEvent.ObjectId}'", 
                                                AuthorizationToken);

                Payload.Add(Crate<StandardTableDataCM>.FromContent(runtimeCrateLabel, resultObjects));
            }
        }

        private string GenerateRuntimeDataLabel()
        {
            var curSfChosenObject = ActivityUI.SalesforceObjectList.selectedKey;

            var eventSubscriptions = new List<string>();
            
            if (ActivityUI.Created.Selected)
            {
                eventSubscriptions.Add(CreatedEventname);
            }
            if (ActivityUI.Updated.Selected)
            {
                eventSubscriptions.Add(UpdatedEventname);
            }

            var modifiers = string.Join("/", eventSubscriptions);

            return $"{curSfChosenObject} {modifiers} on Salesforce.com";
        }

        private Crate PackEventSubscriptionsCrate()
        {
            var curSfChosenObject = ActivityUI.SalesforceObjectList.selectedKey;

            var eventSubscriptions = new List<string>();

            if (ActivityUI.Created.Selected)
            {
                eventSubscriptions.Add($"{curSfChosenObject}{CreatedEventname}");
            }
            if (ActivityUI.Updated.Selected)
            {
                eventSubscriptions.Add($"{curSfChosenObject}{UpdatedEventname}");
            }

            return CrateManager.CreateStandardEventSubscriptionsCrate(
                "Standard Event Subscriptions",
                "Salesforce",
                eventSubscriptions.ToArray());
        }
    }
}