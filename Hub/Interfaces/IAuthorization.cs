﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;

namespace Hub.Interfaces
{
    public interface IAuthorization
    {
        void PrepareAuthToken(ActionDTO actionDTO);

        Task<AuthenticateResponse> AuthenticateInternal(Fr8AccountDO account, TerminalDO terminal,
            string domain, string username, string password);

        Task<AuthenticateResponse> GetOAuthToken(TerminalDO terminal, ExternalAuthenticationDTO externalAuthDTO);

        Task<ExternalAuthUrlDTO> GetOAuthInitiationURL(Fr8AccountDO user, TerminalDO terminal);

        void AddAuthenticationCrate(ActionDTO actionDTO, int authType);

        void RemoveAuthenticationCrate(ActionDTO actionDTO);

        bool ValidateAuthenticationNeeded(string userId, ActionDTO curActionDTO);

        void InvalidateToken(string userId, ActionDTO curActionDto);

        IEnumerable<AuthorizationTokenDO> GetAllTokens(string accountId);

        void SetMainToken(string userId, Guid authTokenId);

        void GrantToken(Guid actionId, Guid authTokenId);

        void RevokeToken(string accountId, Guid authTokenId);
    }
}
