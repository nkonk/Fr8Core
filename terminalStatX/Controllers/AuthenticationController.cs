﻿using System;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Fr8.Infrastructure.Data.DataTransferObjects;
using Fr8.Infrastructure.Interfaces;
using Fr8.TerminalBase.Services;
using Newtonsoft.Json;
using terminalStatX.Interfaces;

namespace terminalStatX.Controllers
{
    [RoutePrefix("authentication")]
    public class AuthenticationController : ApiController
    {
        private readonly IStatXIntegration _statXIntegration;
        private readonly IHubEventReporter _eventReporter;

        public AuthenticationController(IHubEventReporter eventReporter, IStatXIntegration statXIntegration)
        {
            _eventReporter = eventReporter;
            _statXIntegration = statXIntegration;
        }

        [HttpPost]
        [Route("send_code")]
        public async Task<PhoneNumberCredentialsDTO> SendAuthenticationCodeToMobilePhone(PhoneNumberCredentialsDTO credentialsDTO)
        {
            try
            {
                var statXAuthResponse = await _statXIntegration.Login(credentialsDTO.ClientName, credentialsDTO.PhoneNumber);

                if (!string.IsNullOrEmpty(statXAuthResponse.Error))
                {
                    credentialsDTO.Error = statXAuthResponse.Error;
                }
               
                credentialsDTO.ClientId = statXAuthResponse.ClientId;

                return credentialsDTO;
            }
            catch (Exception ex)
            {
                await _eventReporter.ReportTerminalError(ex, credentialsDTO.Fr8UserId);
                credentialsDTO.Error = "An error occurred while trying to send login code, please try again later.";

                return credentialsDTO;
            }
        }

        [HttpPost]
        [Route("token")]
        public async Task<AuthorizationTokenDTO> GenerateAccessTokenAndApiKey(PhoneNumberCredentialsDTO credentialsDTO)
        {
            try
            {
                if (string.IsNullOrEmpty(credentialsDTO.ClientId) || string.IsNullOrEmpty(credentialsDTO.PhoneNumber) || string.IsNullOrEmpty(credentialsDTO.VerificationCode))
                {
                    throw new ApplicationException("Provided data about verification code or phone number are missing.");
                }

                var authResponseDTO = await _statXIntegration.VerifyCodeAndGetAuthToken(credentialsDTO.ClientId, credentialsDTO.PhoneNumber, credentialsDTO.VerificationCode);

                return new AuthorizationTokenDTO()
                {
                    Token = authResponseDTO.AuthToken,
                    ExternalAccountId = credentialsDTO.ClientName,
                    AdditionalAttributes = "apiKey="+ authResponseDTO.ApiKey
                };
            }
            catch (Exception ex)
            {
                await _eventReporter.ReportTerminalError(ex, credentialsDTO.Fr8UserId);

                return new AuthorizationTokenDTO()
                {
                    Error = "An error occurred while trying to authorize, please try again later."
                };
            }
        }
    }
}