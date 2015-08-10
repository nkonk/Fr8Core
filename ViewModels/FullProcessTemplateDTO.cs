﻿using System.Collections.Generic;

namespace Web.ViewModels
{
    /// <summary>
    /// ProcessTemplate DTO that contains full graph of other DTO objects
    /// Specifically used in Workflow Designer to draw entire process.
    /// </summary>
    public class FullProcessTemplateDTO
    {
        /// <summary>
        /// DTO for ProcessTemplate entity.
        /// </summary>
        public ProcessTemplateDTO ProcessTemplate { get; set; }

        /// <summary>
        /// List of ProcessNodeTemplate DTOs.
        /// </summary>
        public IEnumerable<FullProcessNodeTemplateDTO> ProcessNodeTemplates { get; set; }
    }

    /// <summary>
    /// ProcessNodeTemplate DTO that contains full graph of other DTO objects.
    /// </summary>
    public class FullProcessNodeTemplateDTO
    {
        /// <summary>
        /// DTO for ProcessNodeTemplate entity.
        /// </summary>
        public ProcessNodeTemplateDTO ProcessNodeTemplate { get; set; }

        /// <summary>
        /// DTO for Criteria entity.
        /// </summary>
        public CriteriaDTO Criteria { get; set; }
    }
}