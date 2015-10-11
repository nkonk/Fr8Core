﻿using System.Linq;
using Data.Interfaces;
using NUnit.Framework;
using StructureMap;
using UtilitiesTesting;
using UtilitiesTesting.Fixtures;
using Core.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.ManifestSchemas;
using Web.Controllers;

namespace DockyardTest.Entities
{
    [TestFixture]
    [Category("ProcessTemplate")]
    public class ProcessTemplateTests : BaseTest
    {
        [Test]
        public void ProcessTemplate_ShouldBeAssignedStartingProcessNodeTemplate()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var processTemplate = FixtureData.TestProcessTemplate2();
                uow.ProcessTemplateRepository.Add(processTemplate);

                var processNodeTemplate = FixtureData.TestProcessNodeTemplateDO2();
                uow.ProcessNodeTemplateRepository.Add(processNodeTemplate);
                processTemplate.StartingProcessNodeTemplate = processNodeTemplate;

                uow.SaveChanges();

                var result = uow.ProcessTemplateRepository.GetQuery()
                    .SingleOrDefault(pt => pt.StartingProcessNodeTemplateId == processNodeTemplate.Id);

                Assert.AreEqual(processNodeTemplate.Id, result.StartingProcessNodeTemplate.Id);
                Assert.AreEqual(processNodeTemplate.Name, result.StartingProcessNodeTemplate.Name);
            }
        }

        [Test]
        public void GetStandardEventSubscribers_ReturnsProcessTemplates()
        {
            FixtureData.TestProcessTemplateWithSubscribeEvent();
            IProcessTemplate curProcessTemplate = ObjectFactory.GetInstance<IProcessTemplate>();
            EventReportMS curEventReport = FixtureData.StandardEventReportFormat();

            var result = curProcessTemplate.GetMatchingProcessTemplates("testuser1", curEventReport);

            Assert.IsNotNull(result);
            Assert.Greater(result.Count, 0);
            Assert.Greater(result.Where(name => name.Name.Contains("StandardEventTesting")).Count(), 0);
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(System.ArgumentNullException))]
        public void GetStandardEventSubscribers_UserIDEmpty_ThrowsException()
        {
            IProcessTemplate curProcessTemplate = ObjectFactory.GetInstance<IProcessTemplate>();

            curProcessTemplate.GetMatchingProcessTemplates("", new EventReportMS());
        }

        [Test]
        [ExpectedException(ExpectedException = typeof(System.ArgumentNullException))]
        public void GetStandardEventSubscribers_EventReportMSNULL_ThrowsException()
        {
            IProcessTemplate curProcessTemplate = ObjectFactory.GetInstance<IProcessTemplate>();

            curProcessTemplate.GetMatchingProcessTemplates("UserTest", null);
        }


    }
}