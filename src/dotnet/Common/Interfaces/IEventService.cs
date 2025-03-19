﻿using Azure.Messaging;
using FoundationaLLM.Common.Models.Events;

namespace FoundationaLLM.Common.Interfaces
{
    /// <summary>
    /// Provides services to interact with an eventing engine.
    /// </summary>
    public interface IEventService
    {
        /// <summary>
        /// The name of the service instance.
        /// </summary>
        string ServiceInstanceName { get; }

        /// <summary>
        /// Gets the <see cref="TaskCompletionSource{T}"/> (<c>TResult</c> of type <see cref="bool"/>)
        /// that signals the completion of the initialization task.
        /// </summary>
        /// <remarks>
        /// The result of the task indicates whether initialization completed successfully or not.
        /// </remarks>
        Task<bool> InitializationTask { get; }

        /// <summary>
        /// Starts the event service, allowing it to initialize.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> signaling cancellation.</param>
        /// <returns></returns>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Stops the event service, allowing it to cleanup.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> signaling cancellation.</param>
        /// <returns></returns>
        Task StopAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Executes the event service until cancellation is signaled.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> signaling cancellation.</param>
        /// <returns></returns>
        Task ExecuteAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Adds an event set event delgate to the list of event handlers for a specified event set namespace.
        /// </summary>
        /// <param name="eventType">The event type to subscribe to.</param>
        /// <param name="eventHandler">The function to be invoked during event handling.</param>
        void SubscribeToEventTypeEvent(string eventType, EventTypeEventDelegate eventHandler);

        /// <summary>
        /// Removes an event set event delegate from the list of event handlers for a specified event set namespace.
        /// </summary>
        /// <param name="eventType">The event type to subscribe to.</param>
        /// <param name="eventHandler">The function to be invoked during event handling.</param>
        void UnsubscribeFromEventTypeEvent(string eventType, EventTypeEventDelegate eventHandler);

        /// <summary>
        /// Sends an event to the event service.
        /// </summary>
        /// <param name="topicName">The name of the topic where the event should be sent.</param>
        /// <param name="cloudEvent">The <see cref="CloudEvent"/> object containing the details of the event.</param>
        /// <returns></returns>
        Task SendEvent(string topicName, CloudEvent cloudEvent);
    }
}
