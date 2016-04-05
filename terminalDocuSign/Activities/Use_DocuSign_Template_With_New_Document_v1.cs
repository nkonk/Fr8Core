﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Data.Constants;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Data.States;
using Hub.Managers;
using terminalDocuSign.DataTransferObjects;
using terminalDocuSign.Services;
using TerminalBase.Infrastructure;
using TerminalBase.Infrastructure.Behaviors;
using terminalDocuSign.Services.New_Api;
using terminalDocuSign.Actions;

namespace terminalDocuSign.Actions
{
    public class Use_DocuSign_Template_With_New_Document_v1 : Send_DocuSign_Envelope_v1
    {
        protected override string ActivityUserFriendlyName => "Use DocuSign Template With New Document";

        protected override PayloadDTO SendAnEnvelope(ICrateStorage curStorage, DocuSignApiConfiguration loginInfo, PayloadDTO payloadCrates,
            List<FieldDTO> rolesList, List<FieldDTO> fieldList, string curTemplateId)
        {
            try
            {
                var fileCrateLabel = (FindControl(curStorage, "document_Override_DDLB") as DropDownList).selectedKey;
                var file_crate = Crate.FromDto(payloadCrates.CrateStorage.Crates.Where(a => a.ManifestType == CrateManifestTypes.StandardFileDescription && a.Label == fileCrateLabel).Single()).Get<StandardFileDescriptionCM>();
                DocuSignManager.SendAnEnvelopeFromTemplate(loginInfo, rolesList, fieldList, curTemplateId, file_crate);
            }
            catch (Exception ex)
            {
                return Error(payloadCrates, $"Couldn't send an envelope. {ex}");
            }
            return Success(payloadCrates);
        }

        protected async override Task<Crate> CreateDocusignTemplateConfigurationControls(ActivityDO curActivity)
        {
            var infoBox = new TextBlock() { Value = @"This Activity overlays the tabs from an existing Template onto a new Document and sends out a DocuSign Envelope. 
                                                        When this Activity executes, it will look for and expect to be provided from upstream with one Excel or Word file." };

            var fieldSelectDocusignTemplateDTO = new DropDownList
            {
                Label = "Use DocuSign Template",
                Name = "target_docusign_template",
                Required = true,
                Events = new List<ControlEvent>()
                {
                     ControlEvent.RequestConfig
                },
                Source = null
            };

            var documentOverrideDDLB = new DropDownList
            {
                Label = "Use new document",
                Name = "document_Override_DDLB",
                Required = true,
                ListItems = await GetFilesCrates(curActivity)
            };

            var fieldsDTO = new List<ControlDefinitionDTO>
            {
                infoBox, documentOverrideDDLB, fieldSelectDocusignTemplateDTO
            };

            var controls = new StandardConfigurationControlsCM
            {
                Controls = fieldsDTO
            };

            return CrateManager.CreateStandardConfigurationControlsCrate("Configuration_Controls", fieldsDTO.ToArray());
        }

        protected override async Task<ActivityDO> FollowupConfigurationResponse(ActivityDO curActivityDO, AuthorizationTokenDO authTokenDO)
        {
            var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthTokenDTO>(authTokenDO.Token);

            using (var crateStorage = CrateManager.GetUpdatableStorage(curActivityDO))
            {
                curActivityDO = await HandleFollowUpConfiguration(curActivityDO, authTokenDO, crateStorage);
                curActivityDO = await UpdateFilesDD(curActivityDO, crateStorage);
            }

            return await Task.FromResult(curActivityDO);
        }

        private async Task<ActivityDO> UpdateFilesDD(ActivityDO curActivityDO, IUpdatableCrateStorage crateStorage)
        {
            var ddlb = (DropDownList)FindControl(crateStorage, "document_Override_DDLB");
            ddlb.ListItems = await GetFilesCrates(curActivityDO);
            return curActivityDO;
        }

        private async Task<List<ListItem>> GetFilesCrates(ActivityDO curActivityDO)
        {
            var crates = await GetCratesByDirection(curActivityDO, CrateDirection.Upstream);
            var file_crates = crates.Where(a => a.Label == "Runtime Available Crates").FirstOrDefault().Get<CrateDescriptionCM>().CrateDescriptions.Where(a => a.ManifestType == CrateManifestTypes.StandardFileDescription);
            return new List<ListItem>(file_crates.Select(a => new ListItem() { Key = a.Label }));
        }
    }
}