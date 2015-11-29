﻿using Data.Constants;

namespace Data.Interfaces.Manifests
{
    public class StandardAuthenticationCM : Manifest
    {
        public StandardAuthenticationCM()
            : base(MT.StandardAuthentication)
        {
        }

        public AuthenticationMode Mode { get; set; }
    }

    public enum AuthenticationMode
    {
        /// <summary>
        /// When application shows default credentials window.
        /// </summary>
        InternalMode = 1,

        /// <summary>
        /// When external auth form URL is triggered.
        /// </summary>
        ExternalMode = 2
    }
}
