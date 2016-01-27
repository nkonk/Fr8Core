﻿using System;
using System.Collections.Generic;
using System.Linq;
using Data.Crates;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Newtonsoft.Json;

namespace Data.Migrations
{
    partial class MigrationConfiguration
    {
        private class RouteBuilder
        {
            private readonly string _name;
            private readonly Fr8AccountDO _fr8AccountDO;
            private readonly List<Crate> _crates = new List<Crate>();
            private Guid _ptId;

            public RouteBuilder(string name, Fr8AccountDO fr8AccountDO)
            {
                _name = name;
                _fr8AccountDO = fr8AccountDO;
            }

            public RouteBuilder AddCrate(Crate crateDto)
            {
                _crates.Add(crateDto);
                return this;
            }
            
            public void Store(IUnitOfWork uow)
            {
                StoreTemplate(uow);

                var container = uow.ContainerRepository.GetQuery().FirstOrDefault(x => x.Name == _name);

                var add = container == null;
                
                if (add)
                {
                    container = new ContainerDO()
                    {
                        Id = Guid.NewGuid()
                    };
                }

                ConfigureProcess(container);

                if (add)
                {
                    uow.ContainerRepository.Add(container);
                }
            }

            

            private void StoreTemplate(IUnitOfWork uow)
            {
                var plan = uow.RouteRepository.GetQuery().FirstOrDefault(x => x.Name == _name);
                bool add = plan == null;

                if (add)
                {
                    plan = new PlanDO();
                    plan.Id = Guid.NewGuid();
                }

                plan.Fr8Account = _fr8AccountDO;
                plan.Name = _name;
                plan.Description = "Template for testing";
				plan.CreateDate = DateTime.UtcNow;
				plan.LastUpdated = DateTime.UtcNow;
                plan.RouteState = RouteState.Inactive; // we don't want this process template can be executed ouside of tests

                if (add)
                {
                    uow.RouteRepository.Add(plan);
                    uow.SaveChanges();
                }

                _ptId = plan.Id;
            }

            private void ConfigureProcess(ContainerDO container)
            {
                container.Name = _name;
                container.PlanId = _ptId;
                container.ContainerState = ContainerState.Executing;
                
                container.CrateStorage = JsonConvert.SerializeObject(CrateStorageSerializer.Default.ConvertToDto(new CrateStorage(_crates)));
            }

            
        }
    }
}
