﻿namespace FoundationaLLM.Common.Constants.ResourceProviders
{
    /// <summary>
    /// The names of the actions implemented by most of the FoundationaLLM resource providers.
    /// </summary>
    public static class ResourceProviderActions
    {
        /// <summary>
        /// Check the validity of a resource name.
        /// </summary>
        public const string CheckName = "checkname";

        /// <summary>
        /// Purges a soft-deleted resource.
        /// </summary>
        public const string Purge = "purge";

        /// <summary>
        /// Filter resources.
        /// </summary>
        public const string Filter = "filter";

        /// <summary>
        /// Load the content of a file.
        /// </summary>
        public const string LoadFileContent = "load-file-content";

        /// <summary>
        /// Set file tool associations.
        /// </summary>
        public const string UpdateFileToolAssociations = "update-file-tool-associations";

        /// <summary>
        /// Validate resources.
        /// </summary>
        public const string Validate = "validate";

        /// <summary>
        /// Set the resource as the default.
        /// </summary>
        public const string SetDefault = "set-default";
    }
}
