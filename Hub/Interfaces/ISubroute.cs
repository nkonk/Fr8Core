﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;

namespace Hub.Interfaces
{
    /// <summary>
    /// Subroute service.
    /// </summary>
    public interface ISubroute
    {
        void Update(IUnitOfWork uow, SubrouteDO subroute);
        void Delete(IUnitOfWork uow, Guid id);
        /// <summary>
        /// Backups current action and calls configure on downstream actions
        /// if there are validation errors restores current action and returns false
        /// </summary>
        /// <param name="userId">Current user id</param>
        /// <param name="activityId">Action to delete</param>
        /// <param name="confirmed">Forces deletion of current action even when there are validation errors</param>
        /// <returns>Deletion status of action</returns>
        Task<bool> DeleteActivity(string userId, Guid activityId, bool confirmed);
    }
}
