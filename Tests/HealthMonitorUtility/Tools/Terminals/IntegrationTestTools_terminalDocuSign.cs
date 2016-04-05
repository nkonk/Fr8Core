﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using HealthMonitor.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using StructureMap;
using terminalGoogle.DataTransferObjects;

namespace terminaBaselTests.Tools.Terminals
{
    public class IntegrationTestTools_terminalDocuSign
    {
        private readonly BaseHubIntegrationTest _baseHubITest;

        public IntegrationTestTools_terminalDocuSign(BaseHubIntegrationTest baseHubIntegrationTest)
        {
            _baseHubITest = baseHubIntegrationTest;
        }

        /// <summary>
        /// Generate new DocuSign authorization token from provided credentials
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="terminalId"></param>
        /// <returns></returns>
        public async Task<AuthorizationTokenDTO> GenerateAuthToken(string username, string password, int terminalId)
        {
            var creds = new CredentialsDTO()
            {
                Username = username,
                Password = password, 
                IsDemoAccount = true,
                TerminalId = terminalId
            };

            var token = await _baseHubITest.HttpPostAsync<CredentialsDTO, JObject>(_baseHubITest.GetHubApiBaseUrl() + "authentication/token", creds);
        
            Assert.AreNotEqual(token["error"].ToString(), "Unable to authenticate in DocuSign service, invalid login name or password.",
                "DocuSign auth error. Perhaps max number of tokens is exceeded.");
            Assert.AreEqual(false, String.IsNullOrEmpty(token["authTokenId"].Value<string>()),
                "AuthTokenId is missing in API response.");

            Guid tokenGuid = Guid.Parse(token["authTokenId"].Value<string>());

            return await Task.FromResult(new AuthorizationTokenDTO { Token = tokenGuid.ToString() });
        }

        /// <summary>
        /// Authenticate to DocuSign and accociate generated token with activity
        /// </summary>
        /// <param name="activityId"></param>
        /// <param name="credentials"></param>
        /// <param name="terminalId"></param>
        /// <returns></returns>
        public async Task<Guid> AuthenticateDocuSignAndAssociateTokenWithAction(Guid activityId, CredentialsDTO credentials, int terminalId)
        {
            //
            // Authenticate with DocuSign Credentials
            //
            var authTokenDTO = await GenerateAuthToken(credentials.Username, credentials.Password, terminalId);
            var tokenGuid = Guid.Parse(authTokenDTO.Token);
           
            //
            // Asociate token with action
            //
            var applyToken = new ManageAuthToken_Apply()
            {
                ActivityId = activityId,
                AuthTokenId = tokenGuid,
                IsMain = true
            };
            await _baseHubITest.HttpPostAsync<ManageAuthToken_Apply[], string>(_baseHubITest.GetHubApiBaseUrl() + "ManageAuthToken/apply", new[] { applyToken });

            return tokenGuid;
        }

        
        /// <summary>
        /// Get GoogleAuthToken from the TokenRepository based on authorizationTokenId 
        /// </summary>
        /// <param name="authorizationTokenId"></param>
        /// <returns></returns>
        public AuthorizationTokenDO GetDocuSignAuthToken(Guid authorizationTokenId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var validToken = uow.AuthorizationTokenRepository.FindTokenById(authorizationTokenId);

                Assert.IsNotNull(validToken, "Reading default docusSign token from AuthorizationTokenRepository failed. Please provide default account for authenticating terminalDocuSign.");

                return validToken;
            }
        }
    }
}