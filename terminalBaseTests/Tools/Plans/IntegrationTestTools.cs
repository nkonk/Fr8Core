﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using HealthMonitor.Utility;
using NUnit.Framework;
using UtilitiesTesting.Fixtures;

namespace terminaBaselTests.Tools.Plans
{
    public class IntegrationTestTools
    {
        private readonly BaseHubIntegrationTest _baseHubITest;

        public IntegrationTestTools(BaseHubIntegrationTest baseHubIntegrationTest)
        {
            _baseHubITest = baseHubIntegrationTest;
        }

        public async Task<PlanDTO> CreateNewPlan()
        {
            var newPlan = FixtureData.CreateTestPlanDTO();

            var planDTO = await _baseHubITest.HttpPostAsync<PlanEmptyDTO, PlanDTO>(_baseHubITest.GetHubApiBaseUrl() + "plans", newPlan);

            Assert.AreNotEqual(planDTO.Plan.Id, Guid.Empty, "New created Plan doesn't have a valid Id. Plan failed to be crated.");
            Assert.True(planDTO.Plan.SubPlans.Any(), "New created Plan doesn't have an existing sub plan.");

            return await Task.FromResult(planDTO);
        }

        public async Task RunPlan(Guid planId)
        {
            var executionContainer = await _baseHubITest.HttpPostAsync<string, ContainerDTO>(_baseHubITest.GetHubApiBaseUrl() + "plans/run?planId=" + planId, null);

            Assert.IsNotNull(executionContainer, "Execution of plan failed. ContainerDTO is missing as a response");
            Assert.AreEqual(executionContainer.ContainerState, ContainerState.Completed, "Execution of plan failed. Container state is not completed");
        }
    }
}