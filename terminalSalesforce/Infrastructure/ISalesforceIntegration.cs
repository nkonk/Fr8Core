﻿using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Data.Interfaces.Manifests;

namespace terminalSalesforce.Infrastructure
{
    public interface ISalesforceIntegration
    {
        bool CreateLead(ActionDO actionDO, AuthorizationTokenDO authTokenDO);

        bool CreateContact(ActionDO actionDTO, AuthorizationTokenDO authTokenDO);

        bool CreateAccount(ActionDO actionDTO, AuthorizationTokenDO authTokenDO);

        Task<IList<FieldDTO>> GetFields(AuthorizationTokenDO authTokenDO, string salesforceObjectName);

        Task<StandardPayloadDataCM> GetObject(AuthorizationTokenDO authTokenDO, string salesforceObjectName, string condition);
    }
}