﻿using Data.States;
using Data.States.Templates;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.Entities
{
    public class AuthorizationTokenDO : BaseDO
    {
        public AuthorizationTokenDO()
        {
            Id = Guid.NewGuid();
            
            // Do not initialize navigation properties like this.
            // It breaks entity update process.
            // When record is retrieved from DB, new instance of dynamic proxy is created derived from AuthorizationTokenDO.
            // If we manually set navigation property like this, EF will not be able to handle dynamic proxy navigation properties properly!.
            // It is ok to initialize collections, though.
            // Commented out by yakov.gnusin.
            // Terminal = new TerminalDO() { Name = "", Version = "1", TerminalStatus = TerminalStatus.Active };
        }

        public Guid Id { get; set; }
        public String Token { get; set; }
        public String RedirectURL { get; set; }
        public String SegmentTrackingEventName { get; set; }
        public String SegmentTrackingProperties { get; set; }
        public DateTime ExpiresAt { get; set; }

        public String ExternalAccountId { get; set; }

        /// <summary>
        /// State-token parameter, that is sent to exteral auth service,
        /// and returned back when auth is completed.
        /// </summary>
        public String ExternalStateToken { get; set; }

        [ForeignKey("UserDO")]
        public String UserID { get; set; }
        public virtual Fr8AccountDO UserDO { get; set; }

        [ForeignKey("Terminal")]
        public int TerminalID { get; set; }
        public virtual TerminalDO Terminal { get; set; }

        [ForeignKey("AuthorizationTokenStateTemplate")]
        public int? AuthorizationTokenState { get; set; }

        public virtual _AuthorizationTokenStateTemplate AuthorizationTokenStateTemplate { get; set; }

        //Additional Attributes such as version number ,  instance url etc..
        public String AdditionalAttributes { get; set; }

        public bool IsMain { get; set; }
    }
}
