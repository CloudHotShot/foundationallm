﻿using FoundationaLLM.Common.Authentication;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.ResourceProviders.DataPipeline;
using FoundationaLLM.DataPipelineEngine.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FoundationaLLM.DataPipeline.API.Controllers
{
    /// <summary>
    /// Provides the routes for the Data Pipelines API data pipelines controller.
    /// </summary>
    /// <param name="dataPipelineService">The service used to manage data pipeline runs.</param>
    /// <param name="orchestrationContext">The orchestration context that provides FoundationaLLM context information</param>
    /// <param name="logger">The <see cref="ILogger"/> used for logging.</param>
    [ApiController]
    [APIKeyAuthentication]
    [Route("instances/{instanceId}")]
    public class DataPipelineRunsController(
        IDataPipelineService dataPipelineService,
        IOrchestrationContext orchestrationContext,
        ILogger<DataPipelineRunsController> logger): ControllerBase
    {
        private readonly IDataPipelineService _dataPipelineService = dataPipelineService;
        private readonly IOrchestrationContext _orchestrationContext = orchestrationContext;
        private readonly ILogger<DataPipelineRunsController> _logger = logger;

        /// <summary>
        /// Retrieves a list of data pipeline runs filtered by the provided filter criteria.
        /// </summary>
        /// <param name="instanceId">The FoundationaLLM instance identifier.</param>
        /// <param name="dataPipelineRunFilter">The definition of the filter criteria.</param>
        /// <returns>The requested list of data pipeline run objects.</returns>
        [HttpPost("datapipelineruns/filter")]
        public async Task<IActionResult> GetDataPipelineRuns(
            string instanceId,
            DataPipelineRunFilter dataPipelineRunFilter)
        {
            var dataPipelineRuns = await _dataPipelineService.GetDataPipelineRuns(
                instanceId,
                dataPipelineRunFilter,
                _orchestrationContext.CurrentUserIdentity!);

            return Ok(dataPipelineRuns);
        }

        /// <summary>
        /// Retrieves a data pipeline run by its name.
        /// </summary>
        /// <param name="instanceId">The FoundationaLLM instance identifier.</param>
        /// <param name="dataPipelineName">The name of the data pipeline.</param>
        /// <param name="runId">The identifier of the data pipeline run.</param>
        /// <returns>The data pipeline run identified by the provided identifier.</returns>
        [HttpGet("datapipelines/{dataPipelineName}/datapipelineruns/{runId}")]
        public async Task<IActionResult> GetDataPipelineRun(
            string instanceId,
            string dataPipelineName,
            string runId)
        {
            var dataPipelineRun = await _dataPipelineService.GetDataPipelineRun(
                instanceId,
                dataPipelineName,
                runId,
                _orchestrationContext.CurrentUserIdentity!);

            return Ok(dataPipelineRun);
        }

        /// <summary>
        /// Creates a new data pipeline run.
        /// </summary>
        /// <param name="instanceId">The FoundationaLLM instance identifier.</param>
        /// <param name="dataPipelineRun">The object with the properties of the new data pipeline run.</param>
        /// <returns>The newly created data pipeline run.</returns>
        [HttpPost("datapipelineruns")]
        public async Task<IActionResult> CreateDataPipelineRun(
            string instanceId,
            [FromBody] DataPipelineRun dataPipelineRun)
        {
            var updatedDataPipelineRun = await _dataPipelineService.CreateDataPipelineRun(
                instanceId,
                dataPipelineRun,
                _orchestrationContext.CurrentUserIdentity!);

            return Ok(updatedDataPipelineRun);
        }
    }
}
