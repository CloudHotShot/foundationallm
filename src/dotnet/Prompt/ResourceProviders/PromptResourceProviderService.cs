﻿using FoundationaLLM.Common.Constants.Configuration;
using FoundationaLLM.Common.Constants.Events;
using FoundationaLLM.Common.Constants.ResourceProviders;
using FoundationaLLM.Common.Exceptions;
using FoundationaLLM.Common.Interfaces;
using FoundationaLLM.Common.Models.Authentication;
using FoundationaLLM.Common.Models.Authorization;
using FoundationaLLM.Common.Models.Configuration.Instance;
using FoundationaLLM.Common.Models.Configuration.ResourceProviders;
using FoundationaLLM.Common.Models.ResourceProviders;
using FoundationaLLM.Common.Models.ResourceProviders.Agent;
using FoundationaLLM.Common.Models.ResourceProviders.Prompt;
using FoundationaLLM.Common.Services.ResourceProviders;
using FoundationaLLM.Prompt.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace FoundationaLLM.Prompt.ResourceProviders
{
    /// <summary>
    /// Implements the FoundationaLLM.Prompt resource provider.
    /// </summary>
    /// <param name="instanceOptions">The options providing the <see cref="InstanceSettings"/> with instance settings.</param>
    /// <param name="cacheOptions">The options providing the <see cref="ResourceProviderCacheSettings"/> with settings for the resource provider cache.</param>
    /// <param name="authorizationService">The <see cref="IAuthorizationServiceClient"/> providing authorization services.</param>
    /// <param name="storageService">The <see cref="IStorageService"/> providing storage services.</param>
    /// <param name="eventService">The <see cref="IEventService"/> providing event services.</param>
    /// <param name="resourceValidatorFactory">The <see cref="IResourceValidatorFactory"/> providing the factory to create resource validators.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> of the main dependency injection container.</param>
    /// <param name="logger">The <see cref="ILogger"/> used for logging.</param>
    /// <param name="proxyMode">Indicates whether the resource provider is running in proxy mode.</param>
    public class PromptResourceProviderService(
        IOptions<InstanceSettings> instanceOptions,
        IOptions<ResourceProviderCacheSettings> cacheOptions,
        IAuthorizationServiceClient authorizationService,
        [FromKeyedServices(DependencyInjectionKeys.FoundationaLLM_ResourceProviders_Prompt)] IStorageService storageService,
        IEventService eventService,
        IResourceValidatorFactory resourceValidatorFactory,
        IServiceProvider serviceProvider,
        ILogger<PromptResourceProviderService> logger,
        bool proxyMode = false)
        : ResourceProviderServiceBase<PromptReference>(
            instanceOptions.Value,
            cacheOptions.Value,
            authorizationService,
            storageService,
            eventService,
            resourceValidatorFactory,
            serviceProvider,
            logger,
            [
                EventTypes.FoundationaLLM_ResourceProvider_Cache_ResetCommand
            ],
            useInternalReferencesStore: true,
            proxyMode: proxyMode)
    {
        /// <inheritdoc/>
        protected override Dictionary<string, ResourceTypeDescriptor> GetResourceTypes() =>
            PromptResourceProviderMetadata.AllowedResourceTypes;

        /// <inheritdoc/>
        protected override string _name => ResourceProviderNames.FoundationaLLM_Prompt;

        /// <inheritdoc/>
        protected override async Task InitializeInternal() =>
            await Task.CompletedTask;

        #region Resource provider support for Management API

        /// <inheritdoc/>
        protected override async Task<object> GetResourcesAsync(
            ResourcePath resourcePath,
            ResourcePathAuthorizationResult authorizationResult,
            UnifiedUserIdentity userIdentity,
            ResourceProviderGetOptions? options = null) =>
            resourcePath.MainResourceTypeName switch
            {
                PromptResourceTypeNames.Prompts => await LoadResources<PromptBase>(
                    resourcePath.ResourceTypeInstances[0],
                    authorizationResult,
                    options ?? new ResourceProviderGetOptions
                    {
                        IncludeRoles = resourcePath.IsResourceTypePath
                    }),
                _ => throw new ResourceProviderException(
                    $"The resource type {resourcePath.MainResourceTypeName} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest)
            };

        /// <inheritdoc/>
        protected override async Task<object> UpsertResourceAsync(
            ResourcePath resourcePath,
            string? serializedResource,
            ResourceProviderFormFile? formFile,
            ResourcePathAuthorizationResult authorizationResult,
            UnifiedUserIdentity userIdentity,
            Func<object, bool>? requestPayloadValidator = null) =>

            resourcePath.MainResourceTypeName switch
            {
                PromptResourceTypeNames.Prompts => await UpdatePrompt(resourcePath, serializedResource!, userIdentity),
                _ => throw new ResourceProviderException(
                        $"The resource type {resourcePath.MainResourceTypeName} is not supported by the {_name} resource provider.",
                        StatusCodes.Status400BadRequest),
            };

        /// <inheritdoc/>
        protected override async Task<object> ExecuteActionAsync(
            ResourcePath resourcePath,
            ResourcePathAuthorizationResult authorizationResult,
            string serializedAction,
            UnifiedUserIdentity userIdentity,
            Func<object, bool>? requestPayloadValidator = null) =>
            resourcePath.ResourceTypeName switch
            {
                SharedResourceTypeNames.Management => resourcePath.Action switch
                {
                    ResourceProviderActions.TriggerCommand => await ExecuteManagementAction(
                        resourcePath,
                        authorizationResult,
                        serializedAction),
                    _ => throw new ResourceProviderException($"The action {resourcePath.Action} is not supported by the {_name} resource provider.",
                        StatusCodes.Status400BadRequest)
                },
                PromptResourceTypeNames.Prompts => resourcePath.Action switch
                {
                    ResourceProviderActions.CheckName => await CheckPromptName(authorizationResult, serializedAction),
                    ResourceProviderActions.Purge => await PurgeResource<PromptBase>(resourcePath),
                    _ => throw new ResourceProviderException(
                            $"The action {resourcePath.Action} is not supported by the {_name} resource provider.",
                            StatusCodes.Status400BadRequest)
                },
                _ => throw new ResourceProviderException()
            };

        /// <inheritdoc/>
        protected override async Task DeleteResourceAsync(ResourcePath resourcePath, UnifiedUserIdentity userIdentity)
        {
            switch (resourcePath.ResourceTypeName)
            {
                case PromptResourceTypeNames.Prompts:
                    await DeleteResource<PromptBase>(resourcePath);
                    break;
                default:
                    throw new ResourceProviderException($"The resource type {resourcePath.ResourceTypeName} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest);
            };
            await SendResourceProviderEvent(EventTypes.FoundationaLLM_ResourceProvider_Cache_ResetCommand);
        }

        #endregion

        #region Resource provider strongly typed operations

        /// <inheritdoc/>
        protected override async Task<T> GetResourceAsyncInternal<T>(ResourcePath resourcePath, ResourcePathAuthorizationResult authorizationResult, UnifiedUserIdentity userIdentity, ResourceProviderGetOptions? options = null) =>
            (await LoadResource<T>(resourcePath.ResourceId!))!;

        /// <inheritdoc/>
        protected override async Task<TResult> UpsertResourceAsyncInternal<T, TResult>(
            ResourcePath resourcePath,
            ResourcePathAuthorizationResult authorizationResult,
            T resource, UnifiedUserIdentity userIdentity,
            ResourceProviderUpsertOptions? options = null) =>
            resource.GetType() switch
            {
                Type t when t == typeof(MultipartPrompt) =>
                    ((await UpdatePrompt(
                        resourcePath,
                        (resource as PromptBase)!,
                        userIdentity)).ToResourceProviderUpsertResult<PromptBase>() as TResult)!,
                _ => throw new ResourceProviderException($"The resource type {resource.GetType().Name} is not supported by the {_name} resource provider.",
                    StatusCodes.Status400BadRequest)
            };

        #endregion

        #region Resource management

        private async Task<ResourceNameCheckResult> CheckPromptName(
            ResourcePathAuthorizationResult authorizationResult,
            string serializedAction)
        {
            if (!authorizationResult.Authorized
                && !authorizationResult.HasRequiredRole)
                throw new ResourceProviderException("Access is not authorized.", StatusCodes.Status403Forbidden);

            return await CheckResourceName<PromptBase>(
                JsonSerializer.Deserialize<ResourceName>(serializedAction)!);
        }

        private async Task<ResourceProviderUpsertResult> UpdatePrompt(ResourcePath resourcePath, string serializedPrompt, UnifiedUserIdentity userIdentity)
        {
            var prompt = JsonSerializer.Deserialize<PromptBase>(serializedPrompt)
                ?? throw new ResourceProviderException("The object definition is invalid.",
                    StatusCodes.Status400BadRequest);

            return await UpdatePrompt(resourcePath, prompt, userIdentity);
        }
        private async Task<ResourceProviderUpsertResult> UpdatePrompt(
            ResourcePath resourcePath,
            PromptBase prompt,
            UnifiedUserIdentity userIdentity)
        {
            var existingPromptReference = await _resourceReferenceStore!.GetResourceReference(prompt.Name);

            if (resourcePath.ResourceTypeInstances[0].ResourceId != prompt.Name)
                throw new ResourceProviderException("The resource path does not match the object definition (name mismatch).",
                    StatusCodes.Status400BadRequest);

            var promptReference = new PromptReference
            {
                Name = prompt.Name!,
                Type = prompt.Type!,
                Filename = $"/{_name}/{prompt.Name}.json",
                Deleted = false
            };

            // TODO: Add validation for the prompt object.

            prompt.ObjectId = resourcePath.GetObjectId(_instanceSettings.Id, _name);

            UpdateBaseProperties(prompt, userIdentity, isNew: existingPromptReference is null);
            if (existingPromptReference is null)
                await CreateResource<PromptBase>(promptReference, prompt);
            else
                await SaveResource<PromptBase>(existingPromptReference, prompt);

            return new ResourceProviderUpsertResult
            {
                ObjectId = prompt!.ObjectId,
                ResourceExists = existingPromptReference is not null
            };
        }


        #endregion
    }
}
