using System;
using System.Collections.Generic;
using Data.Entities;
using Fr8.Infrastructure.Data.DataTransferObjects;
using Fr8.Infrastructure.Data.Manifests;
using Fr8.TerminalBase.Models;
using Newtonsoft.Json.Linq;
using terminalDocuSign.DataTransferObjects;

namespace terminalDocuSign.Services.New_Api
{
    public interface IDocuSignManager
    {
        DocuSignApiConfiguration SetUp(AuthorizationToken authTokenDO);
        List<FieldDTO> GetTemplatesList(DocuSignApiConfiguration conf);
        JObject DownloadDocuSignTemplate(DocuSignApiConfiguration config, string selectedDocusignTemplateId);
        IEnumerable<FieldDTO> GetEnvelopeRecipientsAndTabs(DocuSignApiConfiguration conf, string envelopeId);
        IEnumerable<FieldDTO> GetTemplateRecipientsAndTabs(DocuSignApiConfiguration conf, string templateId);
        Tuple<IEnumerable<FieldDTO>, IEnumerable<DocuSignTabDTO>> GetTemplateRecipientsTabsAndDocuSignTabs(DocuSignApiConfiguration conf, string templateId);
        void SendAnEnvelopeFromTemplate(DocuSignApiConfiguration loginInfo, List<FieldDTO> rolesList, List<FieldDTO> fieldList, string curTemplateId, StandardFileDescriptionCM file = null);
        bool DocuSignTemplateDefaultNames(IEnumerable<DocuSignTabDTO> templateDefinedFields);
    }
}