﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web.Http;
using System.Web.Http.Description;
using AutoMapper;
using Hub.Exceptions;
using Hub.Infrastructure;
using HubWeb.Controllers.Helpers;
using Microsoft.AspNet.Identity;
using StructureMap;
using Data.Entities;
using Data.Infrastructure.StructureMap;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Hub.Interfaces;
using System.Threading.Tasks;
using HubWeb.ViewModels;
using Newtonsoft.Json;
using Hub.Managers;
using Data.Crates;
using Data.Interfaces.DataTransferObjects.Helpers;
using Utilities.Interfaces;
using HubWeb.Infrastructure;
using Data.Interfaces.Manifests;
using System.Text;
using Data.Constants;
using Data.Infrastructure;

namespace HubWeb.Controllers
{
    //[RoutePrefix("routes")]
    [Fr8ApiAuthorize]
    public class PlansController : ApiController
    {
        private const string PUSHER_EVENT_GENERIC_SUCCESS = "fr8pusher_generic_success";
        private const string PUSHER_EVENT_GENERIC_FAILURE = "fr8pusher_generic_failure";

        private readonly Hub.Interfaces.IPlan _plan;
        private readonly IFindObjectsPlan _findObjectsPlan;
        private readonly ISecurityServices _security;
        private readonly ICrateManager _crate;
        private readonly IPusherNotifier _pusherNotifier;

        public PlansController()
        {
            _plan = ObjectFactory.GetInstance<IPlan>();
            _security = ObjectFactory.GetInstance<ISecurityServices>();
            _findObjectsPlan = ObjectFactory.GetInstance<IFindObjectsPlan>();
            _crate = ObjectFactory.GetInstance<ICrateManager>();
            _pusherNotifier = ObjectFactory.GetInstance<IPusherNotifier>();
        }
        /*
        //[Route("~/plans")]
        [Fr8ApiAuthorize]
        [Fr8HubWebHMACAuthenticate]
        public IHttpActionResult Post(PlanEmptyDTO planDto, bool updateRegistrations = false)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                if (string.IsNullOrEmpty(planDto.Name))
                {
                    ModelState.AddModelError("Name", "Name cannot be null");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest("Some of the request data is invalid");
                }

                var curPlanDO = Mapper.Map<RouteEmptyDTO, RouteDO>(planDto, opts => opts.Items.Add("ptid", planDto.Id));
                curPlanDO.Fr8Account = _security.GetCurrentAccount(uow);

                //this will return 0 on create operation because of not saved changes
                _plan.CreateOrUpdate(uow, curPlanDO, updateRegistrations);
                uow.SaveChanges();
                planDto.Id = curPlanDO.Id;
                //what a mess lets try this
                /*curPlanDO.StartingSubPlan.Plan = curPlanDO;
                uow.SaveChanges();
                processTemplateDto.Id = curPlanDO.Id;
                return Ok(planDto);
            }
        }
        */
        [Fr8HubWebHMACAuthenticate]
        [ResponseType(typeof(PlanDTO))]
        public IHttpActionResult Post(PlanEmptyDTO planDto, bool updateRegistrations = false)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                if (string.IsNullOrEmpty(planDto.Name))
                {
                    ModelState.AddModelError("Name", "Name cannot be null");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest("Some of the request data is invalid");
                }
                var curPlanDO = Mapper.Map<PlanEmptyDTO, PlanDO>(planDto, opts => opts.Items.Add("ptid", planDto.Id));

                _plan.CreateOrUpdate(uow, curPlanDO, updateRegistrations);

                uow.SaveChanges();
                var result = PlanMappingHelper.MapPlanToDto(uow, uow.PlanRepository.GetById<PlanDO>(curPlanDO.Id));
                return Ok(result);
            }
        }

        [Fr8ApiAuthorize]
        //[Route("full/{id:guid}")]
        [ActionName("full")]
        [ResponseType(typeof(PlanDTO))]
        [HttpGet]
        public IHttpActionResult GetFullPlan(Guid id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var plan = uow.PlanRepository.GetById<PlanDO>(id);
                var result = PlanMappingHelper.MapPlanToDto(uow, plan);

                return Ok(result);
            };
        }

        //[Route("getByAction/{id:guid}")]
        [Fr8HubWebHMACAuthenticate]
        [ResponseType(typeof(PlanDTO))]
        [HttpGet]
        public IHttpActionResult GetByActivity(Guid id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var plan = _plan.GetPlanByActivityId(uow, id);
                var result = PlanMappingHelper.MapPlanToDto(uow, plan);

                return Ok(result);
            };
        }

        [Fr8ApiAuthorize]
        [ActionName("status")]
        [HttpGet]
        public IHttpActionResult GetByStatus(Guid? id = null, int? status = null, string category = "")
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curPlans = _plan.GetForUser(
                    uow,
                    _security.GetCurrentAccount(uow),
                    _security.IsCurrentUserHasRole(Roles.Admin),
                    id,
                    status,
                    category
                );

                if (curPlans.Any())
                {
                    var queryableRepoOrdered = curPlans.OrderByDescending(x => x.LastUpdated);
                    return Ok(queryableRepoOrdered.Select(Mapper.Map<PlanEmptyDTO>).ToArray());
                }
            }

            return Ok();
        }

        [Fr8ApiAuthorize]
        [Fr8HubWebHMACAuthenticate]
        [HttpGet]
        [ResponseType(typeof(IEnumerable<PlanDTO>))]
        public IHttpActionResult GetByName(string name)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curPlans = _plan.GetByName(uow, _security.GetCurrentAccount(uow), name);
                var fullPlans = curPlans.Select(curPlan => PlanMappingHelper.MapPlanToDto(uow, curPlan)).ToList();
                return Ok(fullPlans);

            }

        }

        [Fr8ApiAuthorize]
        //[Route("copy")]
        [HttpPost]
        public IHttpActionResult Copy(Guid id, string name)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curPlanDO = uow.PlanRepository.GetById<PlanDO>(id);
                if (curPlanDO == null)
                {
                    throw new ApplicationException("Unable to find plan with specified id.");
                }

                var plan = _plan.Copy(uow, curPlanDO, name);
                uow.SaveChanges();

                return Ok(new { id = plan.Id });
            }
        }

        // GET api/<controller>
        [Fr8ApiAuthorize]
        public IHttpActionResult Get(Guid? id = null)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curPlans = _plan.GetForUser(
                    uow,
                    _security.GetCurrentAccount(uow),
                    _security.IsCurrentUserHasRole(Roles.Admin),
                    id
                );

                if (curPlans.Any())
                {
                    // Return first record from curPlans, in case id parameter was provided.
                    // User intentionally wants to receive a single JSON object in response.
                    if (id.HasValue)
                    {
                        return Ok(Mapper.Map<PlanEmptyDTO>(curPlans.First()));
                    }

                    // Return JSON array of objects, in case no id parameter was provided.
                    return Ok(curPlans.Select(Mapper.Map<PlanEmptyDTO>).ToArray());
                }
            }

            //DO-840 Return empty view as having empty process templates are valid use case.
            return Ok();
        }

        [HttpPost]
        [ActionName("activity")]
        [Fr8ApiAuthorize]
        public IHttpActionResult PutActivity(ActivityDTO activityDto)
        {
            //A stub until the functionaltiy is ready
            return Ok();
        }



        [HttpDelete]
        //[Route("{id:guid}")]
        [Fr8ApiAuthorize]
        public IHttpActionResult Delete(Guid id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                _plan.Delete(uow, id);

                uow.SaveChanges();
                return Ok(id);
            }
        }


        [ActionName("triggersettings"), ResponseType(typeof(List<ExternalEventDTO>))]
        [Fr8ApiAuthorize]
        public IHttpActionResult GetTriggerSettings()
        {
            return Ok("This is no longer used due to V2 Event Handling mechanism changes.");
        }

        [HttpPost]
        [Fr8ApiAuthorize("Admin", "Customer", "Terminal")]
        [Fr8HubWebHMACAuthenticate]
        public async Task<IHttpActionResult> Activate(Guid planId, bool planBuilderActivate = false)
        {
            string pusherChannel = String.Format("fr8pusher_{0}", User.Identity.Name);

            try
            {
                var activateDTO = await _plan.Activate(planId, planBuilderActivate);

                //check if the response contains any error message and show it to the user 
                if (activateDTO != null && activateDTO.ErrorMessage != string.Empty)
                    _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_FAILURE, activateDTO.ErrorMessage);

                EventManager.PlanActivated(planId);

                return Ok(activateDTO);
            }
            catch (ApplicationException ex)
            {
                _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_FAILURE, ex.Message);
                throw;
            }
            catch (Exception)
            {
                _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_FAILURE, "There is a problem with activating this plan. Please try again later.");
                throw;
            }
        }

        [HttpPost]
        [Fr8ApiAuthorize]
        public async Task<IHttpActionResult> Deactivate(Guid planId)
        {
            string activityDTO = await _plan.Deactivate(planId);
            EventManager.PlanDeactivated(planId);
            
            return Ok(activityDTO);
        }

        [HttpPost]
        [Fr8ApiAuthorize]
        public IHttpActionResult CreateFindObjectsPlan()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var account = uow.UserRepository.GetByKey(User.Identity.GetUserId());
                var plan = _findObjectsPlan.CreatePlan(uow, account);

                uow.SaveChanges();

                return Ok(new { id = plan.Id });
            }
        }

        [Fr8ApiAuthorize("Admin", "Customer")]
        [HttpPost]
        public async Task<IHttpActionResult> Run(Guid planId, [FromBody]PayloadVM model)
        {
            string currentPlanType = string.Empty;

            //ACTIVATE - activate route if its inactive
            
            bool inActive = false;
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var planDO = uow.PlanRepository.GetById<PlanDO>(planId);

                if (planDO.PlanState == PlanState.Inactive)
                    inActive = true;
            }

            string pusherChannel = String.Format("fr8pusher_{0}", User.Identity.Name);

            if (inActive)
            {
                var activateDTO = await _plan.Activate(planId, false);

                if (activateDTO != null && activateDTO.Status == "validation_error")
                {
                    //this container holds wrapped inside the ErrorDTO
                    using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                    {
                        var routeDO = uow.PlanRepository.GetById<PlanDO>(planId);
                        activateDTO.Container.CurrentPlanType = routeDO.IsOngoingPlan() ? Data.Constants.PlanType.Ongoing : Data.Constants.PlanType.RunOnce;
                    }

                    return Ok(activateDTO.Container);
                }
                
            }

            //RUN
            CrateDTO curCrateDto;
            Crate curPayload = null;

            if (model != null)
            {
                try
                {
                    curCrateDto = JsonConvert.DeserializeObject<CrateDTO>(model.Payload);
                    curPayload = _crate.FromDto(curCrateDto);
                }
                catch (Exception ex)
                {
                    _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_FAILURE, "Your payload is invalid. Make sure that it represents a valid crate object JSON.");

                    using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                    {
                        var planDO = uow.PlanRepository.GetById<PlanDO>(planId);
                        currentPlanType = planDO.IsOngoingPlan() ? Data.Constants.PlanType.Ongoing.ToString() : Data.Constants.PlanType.RunOnce.ToString();
                    }
                    return BadRequest(currentPlanType);
                }
            }

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var planDO = uow.PlanRepository.GetById<PlanDO>(planId);

                try
                {
                    if (planDO != null)
                    {
                        _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_SUCCESS, $"Launching a new Container for Plan \"{planDO.Name}\"");

                        var containerDO = await _plan.Run(planDO, curPayload);
                        if (!planDO.IsOngoingPlan())
                        {
                            await _plan.Deactivate(planId);
                        }

                        var response = _crate.GetContentType<OperationalStateCM>(containerDO.CrateStorage);

                        var responseMsg = "";

                        ResponseMessageDTO responseMessage;
                        if (response?.CurrentActivityResponse != null 
                            && response.CurrentActivityResponse.TryParseResponseMessageDTO(out responseMessage) 
                            && !string.IsNullOrEmpty(responseMessage?.Message))
                        {
                            responseMsg = "\n" + responseMessage.Message;
                        }

                        var message = $"Complete processing for Plan \"{planDO.Name}\".{responseMsg}";

                        _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_SUCCESS, message);
                        EventManager.ContainerLaunched(containerDO);

                        var containerDTO = Mapper.Map<ContainerDTO>(containerDO);
                        containerDTO.CurrentPlanType = planDO.IsOngoingPlan() ? Data.Constants.PlanType.Ongoing : Data.Constants.PlanType.RunOnce;

                        EventManager.ContainerExecutionCompleted(containerDO);

                        return Ok(containerDTO);
                    }

                    currentPlanType = planDO.IsOngoingPlan() ? Data.Constants.PlanType.Ongoing.ToString() : Data.Constants.PlanType.RunOnce.ToString();
                    return BadRequest(currentPlanType);
                }
                catch (ErrorResponseException ex)
                {
                    _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_FAILURE, $"Plan \"{planDO.Name}\" failed");
                    ex.ContainerDTO.CurrentPlanType = planDO.IsOngoingPlan() ? Data.Constants.PlanType.Ongoing : Data.Constants.PlanType.RunOnce;
                    
                    throw;
                }
                catch (Exception ex)
                {
                    _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_FAILURE, $"Plan \"{planDO.Name}\" failed");
                    throw;
                }
                finally
                {
                    if (!planDO.IsOngoingPlan())
                    {
                        await _plan.Deactivate(planId);
                    }
                }
            }
        }


        [Fr8ApiAuthorize("Admin", "Customer")]
        [Fr8HubWebHMACAuthenticate]
        [HttpPost]
        public async Task<IHttpActionResult> RunWithPayload(Guid planId, [FromBody]List<CrateDTO> payload)
        {
            string currentPlanType = string.Empty;

            //ACTIVATE - activate route if its inactive

            bool inActive = false;
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var planDO = uow.PlanRepository.GetById<PlanDO>(planId);

                if (planDO.PlanState == PlanState.Inactive)
                    inActive = true;
            }

            string pusherChannel = String.Format("fr8pusher_{0}", User.Identity.Name);

            if (inActive)
            {
                var activateDTO = await _plan.Activate(planId, false);

                if (activateDTO != null && activateDTO.Status == "validation_error")
                {
                    //this container holds wrapped inside the ErrorDTO
                    using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                    {
                        var routeDO = uow.PlanRepository.GetById<PlanDO>(planId);
                        activateDTO.Container.CurrentPlanType = routeDO.IsOngoingPlan() ? Data.Constants.PlanType.Ongoing : Data.Constants.PlanType.RunOnce;
                    }

                    return Ok(activateDTO.Container);
                }

            }

            //RUN

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var planDO = uow.PlanRepository.GetById<PlanDO>(planId);

                try
                {
                    if (planDO != null)
                    {
                        _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_SUCCESS, $"Launching a new Container for Plan \"{planDO.Name}\"");

                        var crates = payload.Select(c => _crate.FromDto(c)).ToArray();
                        var containerDO = await _plan.Run(uow , planDO, crates);
                        if (!planDO.IsOngoingPlan())
                        {
                            await _plan.Deactivate(planId);
                        }

                        var response = _crate.GetContentType<OperationalStateCM>(containerDO.CrateStorage);

                        var responseMsg = "";

                        ResponseMessageDTO responseMessage;
                        if (response?.CurrentActivityResponse != null
                            && response.CurrentActivityResponse.TryParseResponseMessageDTO(out responseMessage)
                            && !string.IsNullOrEmpty(responseMessage?.Message))
                        {
                            responseMsg = "\n" + responseMessage.Message;
                        }

                        var message = $"Complete processing for Plan \"{planDO.Name}\".{responseMsg}";

                        _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_SUCCESS, message);
                        EventManager.ContainerLaunched(containerDO);

                        var containerDTO = Mapper.Map<ContainerDTO>(containerDO);
                        containerDTO.CurrentPlanType = planDO.IsOngoingPlan() ? Data.Constants.PlanType.Ongoing : Data.Constants.PlanType.RunOnce;

                        EventManager.ContainerExecutionCompleted(containerDO);

                        return Ok(containerDTO);
                    }

                    currentPlanType = planDO.IsOngoingPlan() ? Data.Constants.PlanType.Ongoing.ToString() : Data.Constants.PlanType.RunOnce.ToString();
                    return BadRequest(currentPlanType);
                }
                catch (ErrorResponseException ex)
                {
                    _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_FAILURE, $"Plan \"{planDO.Name}\" failed");
                    ex.ContainerDTO.CurrentPlanType = planDO.IsOngoingPlan() ? Data.Constants.PlanType.Ongoing : Data.Constants.PlanType.RunOnce;

                    throw;
                }
                catch (Exception ex)
                {
                    _pusherNotifier.Notify(pusherChannel, PUSHER_EVENT_GENERIC_FAILURE, $"Plan \"{planDO.Name}\" failed");
                    throw;
                }
                finally
                {
                    if (!planDO.IsOngoingPlan())
                    {
                        await _plan.Deactivate(planId);
                    }
                }
            }
        }

    }
}