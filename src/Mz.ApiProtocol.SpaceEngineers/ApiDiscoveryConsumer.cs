using System;
using System.Collections.Generic;
using Mz.ApiProtocol;

namespace Mz.ApiProtocol.SpaceEngineers
{
    /// <summary>
    /// Discovers and accepts the first compatible provider for one API
    /// requirement.
    /// </summary>
    /// <remarks>
    /// The consumer listens for unsolicited provider announcements and may
    /// request discovery explicitly through <see cref="RequestDiscovery"/>.
    /// A connected consumer may release its provider with
    /// <see cref="Disconnect"/> or atomically disconnect and request again
    /// with <see cref="Rediscover"/>.
    /// </remarks>
    public sealed class ApiDiscoveryConsumer : IDisposable
    {
        private readonly IModMessageBus _messageBus;

        private ModMessageSubscription _subscription;
        private Guid _pendingCorrelationId;
        private bool _isDisposed;

        /// <summary>
        /// Occurs when a provider for the requested API is observed and its
        /// compatibility has been evaluated.
        /// </summary>
        /// <remarks>
        /// Subscriber failures are captured in <see cref="LastError"/>.
        /// One failing subscriber does not prevent later subscribers from
        /// receiving the event.
        /// </remarks>
        public event EventHandler<ApiProviderObservedEventArgs>
            ProviderObserved;

        /// <summary>
        /// Occurs after a compatible provider connection has been accepted.
        /// </summary>
        /// <remarks>
        /// Subscriber failures are captured in <see cref="LastError"/> and do
        /// not undo the accepted connection.
        /// </remarks>
        public event EventHandler<ApiConnectedEventArgs> Connected;

        /// <summary>
        /// Occurs after an accepted provider connection has been removed.
        /// </summary>
        /// <remarks>
        /// Subscriber failures are captured in <see cref="LastError"/> and do
        /// not restore the removed connection.
        /// </remarks>
        public event EventHandler<ApiDisconnectedEventArgs> Disconnected;

        /// <summary>
        /// Gets the mod-message channel used for discovery.
        /// </summary>
        public long ChannelId { get; }

        /// <summary>
        /// Gets the API identity and provider versions accepted by this
        /// consumer.
        /// </summary>
        public ApiRequirement Requirement { get; }

        /// <summary>
        /// Gets whether the consumer is currently listening for provider
        /// announcements.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets whether a compatible provider has been accepted.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return Connection != null;
            }
        }

        /// <summary>
        /// Gets the accepted provider connection, or null when no compatible
        /// provider has been accepted.
        /// </summary>
        public ApiConnection Connection { get; private set; }

        /// <summary>
        /// Gets the correlation identifier of the most recent unresolved
        /// discovery request.
        /// </summary>
        /// <remarks>
        /// <see cref="Guid.Empty"/> means there is no unresolved request.
        /// An incompatible response does not resolve the request because
        /// another provider may still respond compatibly.
        /// </remarks>
        public Guid PendingCorrelationId
        {
            get
            {
                return _pendingCorrelationId;
            }
        }

        /// <summary>
        /// Gets the most recently observed provider descriptor for the
        /// requested API.
        /// </summary>
        public ApiDescriptor LastObservedProvider { get; private set; }

        /// <summary>
        /// Gets the compatibility result for the most recently observed
        /// provider for the requested API.
        /// </summary>
        public ApiCompatibilityStatus? LastCompatibilityStatus
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the last unexpected processing, transport, or subscriber
        /// error.
        /// </summary>
        public Exception LastError { get; private set; }

        /// <summary>
        /// Creates an API discovery consumer.
        /// </summary>
        /// <param name="messageBus">
        /// The mod-message transport used for discovery.
        /// </param>
        /// <param name="channelId">
        /// The shared discovery channel known by providers and consumers of
        /// this API.
        /// </param>
        /// <param name="requirement">
        /// The API identity and supported provider versions.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="messageBus"/> or
        /// <paramref name="requirement"/> is null.
        /// </exception>
        public ApiDiscoveryConsumer(
            IModMessageBus messageBus,
            long channelId,
            ApiRequirement requirement
        )
        {
            if (messageBus == null)
            {
                throw new ArgumentNullException(
                    nameof(messageBus)
                );
            }

            if (requirement == null)
            {
                throw new ArgumentNullException(
                    nameof(requirement)
                );
            }

            _messageBus = messageBus;
            ChannelId = channelId;
            Requirement = requirement;
        }

        /// <summary>
        /// Registers the provider announcement handler.
        /// </summary>
        /// <remarks>
        /// This method does not automatically send a request. Repeated calls
        /// while already started have no effect.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the consumer has been disposed.
        /// </exception>
        public void Start()
        {
            ThrowIfDisposed();

            if (IsStarted)
                return;

            _subscription = new ModMessageSubscription(
                _messageBus,
                ChannelId,
                HandleMessage
            );

            IsStarted = true;
        }

        /// <summary>
        /// Sends a discovery request for the configured API.
        /// </summary>
        /// <returns>
        /// The non-empty correlation identifier assigned to the request.
        /// </returns>
        /// <remarks>
        /// Requests may be sent repeatedly while disconnected. A newer
        /// request replaces the previous unresolved correlation identifier.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the consumer has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the consumer has not been started or already has an
        /// accepted connection.
        /// </exception>
        public Guid RequestDiscovery()
        {
            ThrowIfDisposed();
            EnsureStarted();

            if (IsConnected)
            {
                throw new InvalidOperationException(
                    "The API discovery consumer is already connected. "
                    + "Call Rediscover to replace the current connection."
                );
            }

            var correlationId = Guid.NewGuid();
            _pendingCorrelationId = correlationId;
            LastError = null;

            var payload =
                ApiDiscoveryWireProtocol.CreateRequest(
                    Requirement.ApiId,
                    correlationId
                );

            try
            {
                _messageBus.Send(ChannelId, payload);
            }
            catch (Exception exception)
            {
                if (_pendingCorrelationId == correlationId)
                    _pendingCorrelationId = Guid.Empty;

                LastError = exception;
                throw;
            }

            return correlationId;
        }

        /// <summary>
        /// Removes the accepted provider connection.
        /// </summary>
        /// <returns>
        /// True when a connection was removed; false when the consumer was
        /// already disconnected.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the consumer has been disposed.
        /// </exception>
        public bool Disconnect()
        {
            ThrowIfDisposed();

            return DisconnectInternal(
                ApiDisconnectReason.ConsumerRequested
            );
        }

        /// <summary>
        /// Removes the current provider connection and immediately sends a
        /// new discovery request.
        /// </summary>
        /// <returns>
        /// The correlation identifier assigned to the new request.
        /// </returns>
        /// <remarks>
        /// The method also works while currently disconnected. In that case
        /// it behaves like <see cref="RequestDiscovery"/>.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the consumer has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the consumer has not been started.
        /// </exception>
        public Guid Rediscover()
        {
            ThrowIfDisposed();
            EnsureStarted();

            DisconnectInternal(
                ApiDisconnectReason.RediscoveryRequested
            );

            return RequestDiscovery();
        }

        /// <summary>
        /// Stops listening and clears the current connection and pending
        /// request.
        /// </summary>
        /// <remarks>
        /// Repeated calls while already stopped have no effect. The consumer
        /// may be started again after being stopped.
        /// </remarks>
        public void Stop()
        {
            if (_isDisposed || !IsStarted)
                return;

            _subscription.Dispose();

            _subscription = null;
            IsStarted = false;
            _pendingCorrelationId = Guid.Empty;

            DisconnectInternal(
                ApiDisconnectReason.ConsumerStopped
            );

            LastObservedProvider = null;
            LastCompatibilityStatus = null;
        }

        /// <summary>
        /// Stops the consumer and permanently releases its registration.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            Stop();
            _isDisposed = true;
        }

        private void HandleMessage(object payload)
        {
            if (!IsStarted || _isDisposed)
                return;

            try
            {
                ApiProviderWithdrawal withdrawal;

                if (ApiDiscoveryWireProtocol.TryParseWithdrawal(
                        payload,
                        out withdrawal
                    ))
                {
                    HandleWithdrawal(withdrawal);
                    return;
                }

                if (IsConnected)
                    return;

                ApiAnnouncement announcement;

                if (!ApiDiscoveryWireProtocol.TryParseAnnouncement(
                        payload,
                        out announcement
                    ))
                {
                    return;
                }

                // Keep the remainder of the existing announcement logic.

                if (!string.Equals(
                    Requirement.ApiId,
                    announcement.Descriptor.ApiId,
                    StringComparison.Ordinal
                ))
                {
                    return;
                }

                if (announcement.CorrelationId != Guid.Empty
                    && announcement.CorrelationId
                    != _pendingCorrelationId)
                {
                    return;
                }

                var compatibility =
                    Requirement.Evaluate(
                        announcement.Descriptor
                    );

                LastObservedProvider = announcement.Descriptor;
                LastCompatibilityStatus = compatibility;

                var observedError = RaiseEvent(
                    ProviderObserved,
                    new ApiProviderObservedEventArgs(
                        announcement.Descriptor,
                        compatibility,
                        announcement.CorrelationId
                    )
                );

                if (compatibility
                    != ApiCompatibilityStatus.Compatible)
                {
                    LastError = observedError;
                    return;
                }

                var endpointCopy =
                    new Dictionary<string, Delegate>(
                        StringComparer.Ordinal
                    );

                foreach (
                    var pair
                    in announcement.Endpoints
                )
                {
                    endpointCopy.Add(pair.Key, pair.Value);
                }

                var connection = new ApiConnection(
                    announcement.Descriptor,
                    announcement.ProviderInstanceId,
                    endpointCopy
                );

                Connection = connection;
                _pendingCorrelationId = Guid.Empty;

                var connectedError = RaiseEvent(
                    Connected,
                    new ApiConnectedEventArgs(
                        connection,
                        announcement.CorrelationId
                    )
                );

                LastError = observedError ?? connectedError;
            }
            catch (Exception exception)
            {
                LastError = exception;
            }
        }
        
        private void HandleWithdrawal(
            ApiProviderWithdrawal withdrawal
        )
        {
            if (!string.Equals(
                    Requirement.ApiId,
                    withdrawal.ApiId,
                    StringComparison.Ordinal
                ))
            {
                return;
            }

            ApiConnection connection = Connection;

            if (connection == null)
                return;

            if (connection.ProviderInstanceId == Guid.Empty)
                return;

            if (connection.ProviderInstanceId
                != withdrawal.ProviderInstanceId)
            {
                return;
            }

            DisconnectInternal(
                ApiDisconnectReason.ProviderWithdrawn
            );
        }

        private bool DisconnectInternal(
            ApiDisconnectReason reason
        )
        {
            var previousConnection = Connection;

            if (previousConnection == null)
                return false;

            Connection = null;
            _pendingCorrelationId = Guid.Empty;

            LastError = RaiseEvent(
                Disconnected,
                new ApiDisconnectedEventArgs(
                    previousConnection,
                    reason
                )
            );

            return true;
        }

        private Exception RaiseEvent<TEventArgs>(
            EventHandler<TEventArgs> handler,
            TEventArgs eventArgs
        )
            where TEventArgs : EventArgs
        {
            if (handler == null)
                return null;

            Exception firstError = null;
            var subscribers = handler.GetInvocationList();

            for (var index = 0; index < subscribers.Length; index++)
            {
                try
                {
                    var subscriber =
                        (EventHandler<TEventArgs>)subscribers[index];

                    subscriber(this, eventArgs);
                }
                catch (Exception exception)
                {
                    if (firstError == null)
                        firstError = exception;
                }
            }

            return firstError;
        }

        private void EnsureStarted()
        {
            if (!IsStarted)
            {
                throw new InvalidOperationException(
                    "The API discovery consumer has not been started."
                );
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(ApiDiscoveryConsumer)
                );
            }
        }
    }
}