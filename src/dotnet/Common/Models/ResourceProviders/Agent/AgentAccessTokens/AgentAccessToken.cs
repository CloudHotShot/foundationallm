﻿namespace FoundationaLLM.Common.Models.ResourceProviders.Agent.AgentAccessTokens
{
    /// <summary>
    /// Agent access token object.
    /// </summary>
    public class AgentAccessToken : ResourceBase
    {
        /// <summary>
        /// The unique identifier.
        /// </summary>
        public required Guid Id { get; set; }

        /// <summary>
        /// A flag denoting if object is active or not.
        /// </summary>
        public required bool Active { get; set; }
    }
}
