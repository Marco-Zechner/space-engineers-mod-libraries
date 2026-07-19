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
    /// request discovery explicitly at any time through
    /// <see cref="RequestDiscovery"/>. This supports either provider or
    /// consumer loading first.
    /// </remarks>
    public sealed class ApiDiscoveryConsumer : IDisposable
    {
        private readonly IModMessageBus _messageBus;

        private ModMessageSubscription _subscription;
        private Guid _pendingCorrelationId;
        private bool _isDisposed;

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
        /// Gets the last unexpected error caught while processing an
        /// announcement or sending a request.
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
        /// This method does not automatically send a request. Call
        /// <see cref="RequestDiscovery"/> when the consuming mod wants to ask
        /// for a provider. Repeated calls while already started have no
        /// effect.
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
        /// Requests may be sent repeatedly. Only the newest unresolved
        /// correlation identifier is retained. Unsolicited compatible
        /// announcements remain acceptable regardless of correlation.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the consumer has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the consumer has not been started.
        /// </exception>
        public Guid RequestDiscovery()
        {
            ThrowIfDisposed();

            if (!IsStarted)
            {
                throw new InvalidOperationException(
                    "The API discovery consumer has not been started."
                );
            }

            Guid correlationId = Guid.NewGuid();
            _pendingCorrelationId = correlationId;
            LastError = null;

            object payload =
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

            ResetDiscoveryState();
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
            if (!IsStarted || _isDisposed || IsConnected)
                return;

            try
            {
                ApiAnnouncement announcement;

                if (!ApiDiscoveryWireProtocol.TryParseAnnouncement(
                    payload,
                    out announcement
                ))
                {
                    return;
                }

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

                ApiCompatibilityStatus compatibility =
                    Requirement.Evaluate(
                        announcement.Descriptor
                    );

                LastObservedProvider = announcement.Descriptor;
                LastCompatibilityStatus = compatibility;

                if (announcement.CorrelationId
                    == _pendingCorrelationId)
                {
                    _pendingCorrelationId = Guid.Empty;
                }

                if (compatibility
                    != ApiCompatibilityStatus.Compatible)
                {
                    return;
                }

                var endpointCopy =
                    new Dictionary<string, Delegate>(
                        StringComparer.Ordinal
                    );

                foreach (
                    KeyValuePair<string, Delegate> pair
                    in announcement.Endpoints
                )
                {
                    endpointCopy.Add(pair.Key, pair.Value);
                }

                Connection = new ApiConnection(
                    announcement.Descriptor,
                    endpointCopy
                );

                _pendingCorrelationId = Guid.Empty;
                LastError = null;
            }
            catch (Exception exception)
            {
                LastError = exception;
            }
        }

        private void ResetDiscoveryState()
        {
            _pendingCorrelationId = Guid.Empty;
            Connection = null;
            LastObservedProvider = null;
            LastCompatibilityStatus = null;
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