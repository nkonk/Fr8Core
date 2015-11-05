﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Data.Constants;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Hub.Enums;
using TerminalBase;
using Data.Interfaces.Manifests;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using terminalDocuSign.DataTransferObjects;
using terminalDocuSign.Services;

namespace terminalDocuSign.Actions
{
    public class Receive_DocuSign_Envelope_v1 : BasePluginAction
    {
        DocuSignManager _docuSignManager; 
        public Receive_DocuSign_Envelope_v1()
        {
            _docuSignManager = new DocuSignManager();
        }

        public async Task<ActionDO> Configure(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            if (NeedsAuthentication(authTokenDO))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            return await ProcessConfigurationRequest(curActionDO, dto => ConfigurationRequestType.Initial, authTokenDO);
        }

        public void Activate(ActionDO curActionDO)
        {
            return; // Will be changed when implementation is plumbed in.
        }

        public void Deactivate(ActionDO curActionDO)
        {
            return; // Will be changed when implementation is plumbed in.
        }

        public async Task<PayloadDTO> Run(ActionDO actionDO, int containerId, AuthorizationTokenDO authTokenDO)
        {
            if (NeedsAuthentication(authTokenDO))
            {
                throw new ApplicationException("No AuthToken provided.");
            }

            var processPayload = await GetProcessPayload(containerId);

            //Get envlopeId
            string envelopeId = GetEnvelopeId(processPayload);
            if (envelopeId == null)
            {
                throw new PluginCodedException(PluginErrorCode.PAYLOAD_DATA_MISSING, "EnvelopeId");
            }

            var payload = CreateActionPayload(actionDO,authTokenDO, envelopeId);
            var cratesList = new List<CrateDTO>()
            {
                Crate.Create("DocuSign Envelope Data",
                    JsonConvert.SerializeObject(payload),
                    CrateManifests.STANDARD_PAYLOAD_MANIFEST_NAME,
                    CrateManifests.STANDARD_PAYLOAD_MANIFEST_ID)
            };

            processPayload.UpdateCrateStorageDTO(cratesList);

            return processPayload;
        }

        public IList<FieldDTO> CreateActionPayload(ActionDO curActionDO, AuthorizationTokenDO authTokenDO, string curEnvelopeId)
        {
            var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthDTO>(authTokenDO.Token);

            var docusignEnvelope = new DocuSignEnvelope(
                docuSignAuthDTO.Email,
                docuSignAuthDTO.ApiPassword);

            var curEnvelopeData = docusignEnvelope.GetEnvelopeData(curEnvelopeId);
            var fields = GetFields(curActionDO);

            if (fields == null || fields.Count == 0)
            {
                throw new InvalidOperationException("Field mappings are empty on ActionDO with id " + curActionDO.Id);
            }

            return docusignEnvelope.ExtractPayload(fields, curEnvelopeId, curEnvelopeData);
        }

        private List<FieldDTO> GetFields(ActionDO curActionDO)
        {
            var fieldsCrate = curActionDO.CrateStorageDTO().CrateDTO
                .Where(x => x.ManifestType == CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME
                    && x.Label == "DocuSignTemplateUserDefinedFields")
                .FirstOrDefault();

            if (fieldsCrate == null) return null;

            var manifestSchema = JsonConvert.DeserializeObject<StandardDesignTimeFieldsCM>(fieldsCrate.Contents);

            if (manifestSchema == null
                || manifestSchema.Fields == null
                || manifestSchema.Fields.Count == 0)
            {
                return null;
            }

            return manifestSchema.Fields;
        }

        private string GetEnvelopeId(PayloadDTO curPayloadDTO)
        {
            var crate = curPayloadDTO.CrateStorageDTO().CrateDTO
                .SingleOrDefault(x => x.ManifestType == CrateManifests.STANDARD_PAYLOAD_MANIFEST_NAME);
            if (crate == null) return null; //TODO: log it

            var fields = JsonConvert.DeserializeObject<List<FieldDTO>>(crate.Contents);
            if (fields == null || fields.Count == 0)
            {
                return null; // TODO: log it
            }

            var envelopeIdField = fields.SingleOrDefault(f => f.Key == "EnvelopeId");
            if (envelopeIdField == null || string.IsNullOrEmpty(envelopeIdField.Value))
            {
                return null; // TODO: log it
            }

            return envelopeIdField.Value;
        }

        protected override async Task<ActionDO> InitialConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO = null)
        {
            var docuSignAuthDTO = JsonConvert.DeserializeObject<DocuSignAuthDTO>(
                authTokenDO.Token);

            //get envelopeIdFromUpstreamActions
            var envelopeIdFromUpstreamActions = await Action.FindKeysByCrateManifestType(
                                                            Mapper.Map<ActionDO>(curActionDO),
                                                            new Manifest(MT.StandardDesignTimeFields),
                                                            "EnvelopeId",
                                                            "Key",
                                                            GetCrateDirection.Upstream);

            //In order to Receive a DocuSign Envelope as fr8, an upstream action needs to provide a DocuSign EnvelopeID.
            TextBlockControlDefinitionDTO textBlock;
            if (envelopeIdFromUpstreamActions.Any())
            {
                textBlock = new TextBlockControlDefinitionDTO()
                {
                    Label = "Docu Sign Envelope",
                    Value = "This Action doesn't require any configuration.",
                    CssClass = "well well-lg"
                };
            }
            else
            {
                textBlock = new TextBlockControlDefinitionDTO()
                {
                    Label = "Docu Sign Envelope",
                    Value = "In order to Receive a DocuSign Envelope as fr8, an upstream action needs to provide a DocuSign EnvelopeID.",
                    CssClass = "alert alert-warning"
                };
            }

            //add the text block crate
            var crateControls = PackControlsCrate(textBlock);
            curActionDO.CrateStorageDTO().CrateDTO.Add(crateControls);

            //get the template ID from the upstream actions
            string docuSignTemplateId = string.Empty;
            var docusignTemplateIdFromUpstreamActions = await Action.FindKeysByCrateManifestType(
                                                                    Mapper.Map<ActionDO>(curActionDO),
                                                                    new Manifest(MT.StandardDesignTimeFields),
                                                                    "TemplateId",
                                                                    "Key",
                                                                    GetCrateDirection.Upstream);

            var templateIdFromUpstreamActions = docusignTemplateIdFromUpstreamActions as JObject[] ?? docusignTemplateIdFromUpstreamActions.ToArray();
            if (templateIdFromUpstreamActions.Length == 1)
            {
                docuSignTemplateId = templateIdFromUpstreamActions[0]["Value"].Value<string>();
            }

            // If DocuSignTemplate Id was found, then add design-time fields.
            _docuSignManager.ExtractFieldsAndAddToCrate(docuSignTemplateId, docuSignAuthDTO, curActionDO);

            return curActionDO;
        }
    }
}