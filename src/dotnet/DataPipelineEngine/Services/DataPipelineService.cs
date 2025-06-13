﻿using FoundationaLLM.Common.Constants.ResourceProviders;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.Authentication;
using FoundationaLLM.Common.Models.ResourceProviders;
using FoundationaLLM.Common.Models.ResourceProviders.DataPipeline;
using FoundationaLLM.DataPipelineEngine.Interfaces;
using FoundationaLLM.DataPipelineEngine.Models.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FoundationaLLM.DataPipelineEngine.Services
{
    /// <summary>
    /// Provides services for managing data pipelines.
    /// </summary>
    /// <param name="resourceProviders">The FoundationaLLM resource providers.</param>
    /// <param name="settings">The settings for the service.</param>
    /// <param name="logger">The logger used for logging.</param>
    public class DataPipelineService(
        IEnumerable<IResourceProviderService> resourceProviders,
        DataPipelineServiceSettings settings,
        ILogger<DataPipelineService> logger) : IDataPipelineService
    {
        private readonly IResourceProviderService _dataPipelineResourceProvider =
            resourceProviders.Single(rp => rp.Name == ResourceProviderNames.FoundationaLLM_DataPipeline);
        private readonly DataPipelineServiceSettings _settings = settings;
        private readonly ILogger<DataPipelineService> _logger = logger;

        /// <inheritdoc/>
        public async Task<DataPipelineRun> CreateDataPipelineRun(
            string instanceId,
            DataPipelineRun dataPipelineRun,
            UnifiedUserIdentity userIdentity)
        {
            var upsertResult = await _dataPipelineResourceProvider.UpsertResourceAsync<DataPipelineRun, ResourceProviderUpsertResult<DataPipelineRun>>(
                instanceId, dataPipelineRun, userIdentity);

            return upsertResult.Resource!;
        }

        /// <inheritdoc/>
        public async Task<DataPipelineRun> GetDataPipelineRun(
            string instanceId,
            string dataPipelineName,
            string runId,
            UnifiedUserIdentity userIdentity) =>
            await _dataPipelineResourceProvider.GetResourceAsync<DataPipelineRun>(
                ResourcePath.GetObjectId(
                    instanceId,
                    ResourceProviderNames.FoundationaLLM_DataPipeline,
                    DataPipelineResourceTypeNames.DataPipelines,
                    dataPipelineName,
                    DataPipelineResourceTypeNames.DataPipelineRuns,
                    runId),
                userIdentity);

        /// <inheritdoc/>
        public async Task<List<DataPipelineRun>> GetDataPipelineRuns(
            string instanceId,
            DataPipelineRunFilter dataPipelineRunFilter,
            UnifiedUserIdentity userIdentity) =>
            (await _dataPipelineResourceProvider.ExecuteResourceActionAsync<
                DataPipelineDefinition, DataPipelineRun, DataPipelineRunFilter, ResourceProviderActionResult<ResourceCollection<DataPipelineRun>>>(
                    instanceId,
                    dataPipelineRunFilter.DataPipelineName!,
                    null!,
                    ResourceProviderActions.Filter,
                    dataPipelineRunFilter,
                    userIdentity)).Resource?.Resources.ToList() ?? [];
    }
}
