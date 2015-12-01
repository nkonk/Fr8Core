﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Data.Control;
using Data.Crates;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;
using Hub.Managers;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using TerminalSqlUtilities;
using Utilities.Configuration.Azure;
using terminalFr8Core.Infrastructure;

namespace terminalFr8Core.Actions
{
    public class FindObjects_Solution_v1 : BaseTerminalAction
    {
        public FindObjectHelper FindObjectHelper { get; set; }

        public FindObjects_Solution_v1()
        {
            FindObjectHelper = new FindObjectHelper();
        }

        #region Configration.

        public override ConfigurationRequestType ConfigurationEvaluator(ActionDO curActionDO)
        {
            if (Crate.IsStorageEmpty(curActionDO))
            {
                return ConfigurationRequestType.Initial;
            }

            return ConfigurationRequestType.Followup;
        }

        protected override Task<ActionDO> InitialConfigurationResponse(
            ActionDO actionDO, AuthorizationTokenDO authTokenDO)
        {
            var connectionString = GetConnectionString();

            using (var updater = Crate.UpdateStorage(actionDO))
            {
                updater.CrateStorage.Clear();

                AddSelectObjectDdl(updater);
                AddAvailableObjects(updater, connectionString);

                UpdatePrevSelectedObject(updater);
            }

            return Task.FromResult(actionDO);
        }

        protected override Task<ActionDO> FollowupConfigurationResponse(
            ActionDO actionDO, AuthorizationTokenDO authTokenDO)
        {
            using (var updater = Crate.UpdateStorage(actionDO))
            {
                var crateStorage = updater.CrateStorage;

                if (NeedsRemoveQueryBuilder(updater))
                {
                    RemoveQueryBuilder(updater);
                    RemoveQueryableCriteriaCrate(updater);
                }

                if (NeedsCreateQueryBuilder(updater))
                {
                    AddQueryBuilder(updater);
                    AddQueryableCriteriaCrate(updater);
                }

                UpdatePrevSelectedObject(updater);
            }

            return Task.FromResult(actionDO);
        }

        private bool NeedsCreateQueryBuilder(ICrateStorageUpdater updater)
        {
            var selectObjectDdl = FindControl(updater.CrateStorage, "SelectObjectDdl") as DropDownList;
            if (selectObjectDdl == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(selectObjectDdl.Value))
            {
                if (FindControl(updater.CrateStorage, "QueryBuilder") == null)
                {
                    return true;
                }
            }

            return false;
        }

        private bool NeedsRemoveQueryBuilder(ICrateStorageUpdater updater)
        {
            var selectObjectDdl = FindControl(updater.CrateStorage, "SelectObjectDdl") as DropDownList;
            if (selectObjectDdl == null)
            {
                return false;
            }

            var prevSelectedValue = "";

            var prevSelectedObjectFields = updater.CrateStorage
                .CrateContentsOfType<StandardDesignTimeFieldsCM>(x => x.Label == "PrevSelectedObject")
                .FirstOrDefault();

            if (prevSelectedObjectFields != null)
            {
                var prevSelectedObjectField = prevSelectedObjectFields.Fields
                    .FirstOrDefault(x => x.Key == "PrevSelectedObject");

                if (prevSelectedObjectField != null)
                {
                    prevSelectedValue = prevSelectedObjectField.Value;
                }
            }

            if (selectObjectDdl.Value != prevSelectedValue)
            {
                if (FindControl(updater.CrateStorage, "QueryBuilder") != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddSelectObjectDdl(ICrateStorageUpdater updater)
        {
            AddControl(
                updater.CrateStorage,
                new DropDownList()
                {
                    Name = "SelectObjectDdl",
                    Label = "Search for",
                    Source = new FieldSourceDTO
                    {
                        Label = "AvailableObjects",
                        ManifestType = CrateManifestTypes.StandardDesignTimeFields
                    },
                    Events = new List<ControlEvent> { new ControlEvent("onChange", "requestConfig") }
                }
            );
        }

        private void AddAvailableObjects(ICrateStorageUpdater updater, string connectionString)
        {
            var tableDefinitions = FindObjectHelper.RetrieveTableDefinitions(connectionString);
            var tableDefinitionCrate =
                Crate.CreateDesignTimeFieldsCrate(
                    "AvailableObjects",
                    tableDefinitions.ToArray()
                );

            updater.CrateStorage.Add(tableDefinitionCrate);
        }

        private void UpdatePrevSelectedObject(ICrateStorageUpdater updater)
        {
            var selectedObjectValue = "";

            var selectObjectDdl = FindControl(updater.CrateStorage, "SelectObjectDdl") as DropDownList;
            if (selectObjectDdl != null)
            {
                selectedObjectValue = selectObjectDdl.Value;
            }

            UpdateDesignTimeCrateValue(
                updater.CrateStorage,
                "PrevSelectedObject",
                new FieldDTO("PrevSelectedObject", selectedObjectValue)
            );
        }

        private void AddQueryBuilder(ICrateStorageUpdater updater)
        {
            AddControl(
                updater.CrateStorage,
                new QueryBuilder()
                {
                    Name = "QueryBuilder",
                    Label = "Query",
                    Required = true,
                    Source = new FieldSourceDTO
                    {
                        Label = "QueryableCriteria",
                        ManifestType = CrateManifestTypes.StandardDesignTimeFields
                    }
                }
            );
        }

        private void RemoveQueryBuilder(ICrateStorageUpdater updater)
        {
            RemoveControl(updater.CrateStorage, "QueryBuilder");
        }

        private void AddQueryableCriteriaCrate(ICrateStorageUpdater updater)
        {

        }

        private void RemoveQueryableCriteriaCrate(ICrateStorageUpdater updater)
        {

        }

        private string GetConnectionString()
        {
            return ConfigurationManager.ConnectionStrings["DockyardDB"].ConnectionString;
        }

        #endregion Configration.
    }
}