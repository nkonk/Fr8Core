﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web.Razor.Generator;
using Data.Crates;
using Hub.Managers;
using Newtonsoft.Json;
using Data.Interfaces;
using Data.Entities;
using Data.Infrastructure;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Enums;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using terminalFr8Core.Interfaces;

namespace terminalFr8Core.Actions
{
    public class FilterUsingRunTimeData_v1 : BaseTerminalAction
    {

        public FilterUsingRunTimeData_v1()
        {
        }

        /// <summary>
        /// Action processing infrastructure.
        /// </summary>
        public async Task<PayloadDTO> Execute(ActionDTO curActionDTO)
        {
            var curPayloadDTO = await GetProcessPayload(curActionDTO.ProcessId);

            ActionDO curAction = AutoMapper.Mapper.Map<ActionDO>(curActionDTO);
            var controlsMS = Action.GetControlsManifest(curAction);

            ControlDefinitionDTO filterPaneControl = controlsMS.Controls.FirstOrDefault(x => x.Type == ControlTypes.FilterPane);
            if (filterPaneControl == null)
            {
                throw new ApplicationException("No control found with Type == \"filterPane\"");
            }

            var valuesCrates = Crate.FromDto(curPayloadDTO.CrateStorage).CrateContentsOfType<StandardPayloadDataCM>();
            var curValues = new List<FieldDTO>();

            foreach (var valuesCrate in valuesCrates)
            {
                curValues.AddRange(valuesCrate.AllValues());
            }

            // Prepare envelope data.

            // Evaluate criteria using Contents json body of found Crate.
            var result = Evaluate(filterPaneControl.Value, curPayloadDTO.ProcessId, curValues);

            return curPayloadDTO;
        }

        private bool Evaluate(string criteria, int processId, IEnumerable<FieldDTO> values)
        {
            if (criteria == null)
                throw new ArgumentNullException("criteria");
            if (criteria == string.Empty)
                throw new ArgumentException("criteria is empty", "criteria");
            if (values == null)
                throw new ArgumentNullException("envelopeData");

            return Filter(criteria, processId, values.AsQueryable()).Any();
        }

        private IQueryable<FieldDTO> Filter(string criteria,
            int processId, IQueryable<FieldDTO> values)
        {
            if (criteria == null)
                throw new ArgumentNullException("criteria");
            if (criteria == string.Empty)
                throw new ArgumentException("criteria is empty", "criteria");
            if (values == null)
                throw new ArgumentNullException("envelopeData");

            var filterDataDTO = JsonConvert.DeserializeObject<FilterDataDTO>(criteria);
            if (filterDataDTO.ExecutionType == FilterExecutionType.WithoutFilter)
            {
                return values;
            }
            else
            {
                EventManager.CriteriaEvaluationStarted(processId);

                var filterExpression = ParseCriteriaExpression(filterDataDTO.Conditions, values);
                var results = values.Provider.CreateQuery<FieldDTO>(filterExpression);

                return results;
            }
        }

        private Expression ParseCriteriaExpression(
            IEnumerable<FilterConditionDTO> conditions,
            IQueryable<FieldDTO> queryableData)
        {
            var curType = typeof(FieldDTO);

            Expression criteriaExpression = null;
            var pe = Expression.Parameter(curType, "p");

            foreach (var condition in conditions)
            {
                var namePropInfo = curType.GetProperty("Key");
                var valuePropInfo = curType.GetProperty("Value");

                var nameLeftExpr = Expression.Property(pe, namePropInfo);
                var nameRightExpr = Expression.Constant(condition.Field);
                var nameExpression = Expression.Equal(nameLeftExpr, nameRightExpr);

                var valueLeftExpr = Expression.Invoke(TryMakeDecimalExpression.Value, Expression.Property(pe, valuePropInfo));
                var valueRightExpr = Expression.Invoke(TryMakeDecimalExpression.Value, Expression.Constant(condition.Value));
                var comparisionExpr = Expression.Call(valueLeftExpr, "CompareTo", null, valueRightExpr);
                var zero = Expression.Constant(0);

                var op = condition.Operator;
                Expression criterionExpression;

                switch (op)
                {
                    case "eq":
                        criterionExpression = Expression.Equal(comparisionExpr, zero);
                        break;
                    case "neq":
                        criterionExpression = Expression.NotEqual(comparisionExpr, zero);
                        break;
                    case "gt":
                        criterionExpression = Expression.GreaterThan(comparisionExpr, zero);
                        break;
                    case "gte":
                        criterionExpression = Expression.GreaterThanOrEqual(comparisionExpr, zero);
                        break;
                    case "lt":
                        criterionExpression = Expression.LessThan(comparisionExpr, zero);
                        break;
                    case "lte":
                        criterionExpression = Expression.LessThanOrEqual(comparisionExpr, zero);
                        break;
                    default:
                        throw new NotSupportedException(string.Format("Not supported operator: {0}", op));
                }

                if (criteriaExpression == null)
                {
                    criteriaExpression = Expression.And(nameExpression, criterionExpression);
                }
                else
                {
                    criteriaExpression = Expression.AndAlso(criteriaExpression, Expression.And(nameExpression, criterionExpression));
                }
            }

            if (criteriaExpression == null)
            {
                criteriaExpression = Expression.Constant(true);
            }

            var whereCallExpression = Expression.Call(
                typeof(Queryable),
                "Where",
                new[] { curType },
                queryableData.Expression,
                Expression.Lambda<Func<FieldDTO, bool>>(criteriaExpression, new[] { pe })
            );

            return whereCallExpression;
        }

        private static readonly Lazy<Expression<Func<string, IComparable>>> TryMakeDecimalExpression =
            new Lazy<Expression<Func<string, IComparable>>>(() =>
            {
                var value = Expression.Parameter(typeof(string), "value");
                var returnValue = Expression.Variable(typeof(IComparable), "result");
                var decimalValue = Expression.Variable(typeof(decimal), "decimalResult");
                var ifExpression = Expression.IfThenElse(
                    Expression.Call(typeof(decimal), "TryParse", null,
                            Expression.TypeAs(value, typeof(string)), decimalValue),
                    Expression.Assign(returnValue, Expression.TypeAs(decimalValue, typeof(IComparable))),
                    Expression.Assign(returnValue, Expression.TypeAs(value, typeof(IComparable))));
                var func = Expression.Block(
                    new[] { returnValue, decimalValue },
                    ifExpression,
                    returnValue);
                return Expression.Lambda<Func<string, IComparable>>(func, "TryMakeDecimal", new[] { value });
            });

        /// <summary>
        /// Configure infrastructure.
        /// </summary>
        public override async Task<ActionDTO> Configure(ActionDTO curActionDataPackageDTO)
        {
            return await ProcessConfigurationRequest(curActionDataPackageDTO, ConfigurationEvaluator);
        }

        private Crate CreateControlsCrate()
        {
            var fieldFilterPane = new FilterPaneControlDefinitionDTO()
            {
                Label = "Execute Actions If:",
                Name = "Selected_Filter",
                Required = true,
                Source = new FieldSourceDTO
                {
                    Label = "Queryable Criteria",
                    ManifestType = CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME
                }
            };

            return PackControlsCrate(fieldFilterPane);
        }

        /// <summary>
        /// Looks for first Create with Id == "Standard Design-Time" among all upcoming Actions.
        /// </summary>
        protected override async Task<ActionDTO> InitialConfigurationResponse(ActionDTO curActionDTO)
        {
            if (curActionDTO.Id > 0)
            {
                //this conversion from actiondto to Action should be moved back to the controller edge
                var curUpstreamFields =
                    (await GetDesignTimeFields(curActionDTO.Id, GetCrateDirection.Upstream))
                    .Fields
                    .ToArray();

                //2) Pack the merged fields into a new crate that can be used to populate the dropdownlistbox
                var queryFieldsCrate = Crate.CreateDesignTimeFieldsCrate("Queryable Criteria", curUpstreamFields);

                //build a controls crate to render the pane
                var configurationControlsCrate = CreateControlsCrate();

                using (var updater = Crate.UpdateStorage(() => curActionDTO.CrateStorage))
                {
                    updater.CrateStorage = AssembleCrateStorage(queryFieldsCrate, configurationControlsCrate);
            }
            }
            else
            {
                throw new ArgumentException(
                    "Configuration requires the submission of an Action that has a real ActionId");
            }

            return curActionDTO;
        }

        protected override async Task<CrateDTO> ValidateAction(ActionDTO curActionDTO)
        {
            return await ValidateByStandartDesignTimeFields(curActionDTO, Crate.GetStorage(curActionDTO).FirstCrate<StandardDesignTimeFieldsCM>(x => x.Label == "Queryable Criteria").Content);
        }

        private ConfigurationRequestType ConfigurationEvaluator(ActionDTO curActionDataPackageDTO)
        {
            if (Crate.IsEmptyStorage(curActionDataPackageDTO.CrateStorage))
            {
                return ConfigurationRequestType.Initial;
            }

            var hasControlsCrate = GetCratesByManifestType(curActionDataPackageDTO, CrateManifests.STANDARD_CONF_CONTROLS_MANIFEST_NAME) != null;

            var hasQueryFieldsCrate = GetCratesByManifestType(curActionDataPackageDTO, CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME) != null;

            if (hasControlsCrate && hasQueryFieldsCrate)
            {
                
                return ConfigurationRequestType.Followup;
            }
            else
            {
                return ConfigurationRequestType.Initial;
            }
        }



        /// <summary>
        ///  Returns true, if at least one row has been fully configured.
        /// </summary>
        /// <param name="curActionDataPackageDTO"></param>
        /// <returns></returns>
//        private bool HasValidConfiguration(ActionDTO curActionDataPackageDTO)
//        {
//            // STANDARD_CONF_CONTROLS_NANIFEST_NAME can't be deseralized to RadioButtonOption.

////            var crateDTO = GetCratesByManifestType(curActionDataPackageDTO, CrateManifests.STANDARD_CONF_CONTROLS_NANIFEST_NAME);
////
////            if (crateDTO != null)
////            {
////                RadioButtonOption radioButtonOption = JsonConvert.DeserializeObject<RadioButtonOption>(crateDTO.Contents);
////                if (radioButtonOption != null)
////                {
////                    foreach (ControlDefinitionDTO controlDefinitionDTO in radioButtonOption.Controls)
////                    {
////                        if (!string.IsNullOrEmpty(controlDefinitionDTO.Value))
////                        {
////                            FilterDataDTO filterDataDTO = JsonConvert.DeserializeObject<FilterDataDTO>(controlDefinitionDTO.Value);
////                            return filterDataDTO.Conditions.Any(x =>
////                                  x.Field != null && x.Field != "" &&
////                                  x.Operator != null && x.Operator != "" &&
////                                  x.Value != null && x.Value != "");
////                        }
////                    }
////                }
////
////            }
//            return false;
//        }

        private Crate GetCratesByManifestType(ActionDTO curActionDataPackageDTO, string curManifestType)
        {
            string curLabel = string.Empty;
            switch (curManifestType)
            {
                case CrateManifests.STANDARD_CONF_CONTROLS_MANIFEST_NAME:
                    curLabel = "Configuration_Controls";
                    break;
                case CrateManifests.DESIGNTIME_FIELDS_MANIFEST_NAME:
                    curLabel = "Queryable Criteria";
                    break;
            }

            return Crate.FromDto(curActionDataPackageDTO.CrateStorage).FirstOrDefault(x =>
                x.ManifestType.Type == curManifestType
                && x.Label == curLabel);

        }
    }
}
