using System;
using System.Collections.Generic;
using Mz.ApiProtocol;

namespace Mz.ApiProtocol.SpaceEngineers
{
    /// <summary>
    /// Exposes one API through the Space Engineers mod-message discovery
    /// protocol.
    /// </summary>
    /// <remarks>
    /// The provider registers when <see cref="Start"/> is called, broadcasts
    /// an unsolicited announcement, and responds to later matching discovery
    /// requests. The instance must be stopped or disposed during mod unload.
    /// </remarks>
    public sealed class ApiDiscoveryProvider : IDisposable
    {
        private readonly IModMessageBus _messageBus;
        private readonly Dictionary<string, Delegate> _endpoints;

        private ModMessageSubscription _subscription;
        private bool _isDisposed;

        /// <summary>
        /// Gets the mod-message channel used for discovery.
        /// </summary>
        public long ChannelId { get; }

        /// <summary>
        /// Gets the API identity and version exposed by this provider.
        /// </summary>
        public ApiDescriptor Descriptor { get; }

        /// <summary>
        /// Gets whether the provider is currently listening for requests.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets the last unexpected error caught while processing a discovery
        /// request.
        /// </summary>
        /// <remarks>
        /// Malformed and unrelated messages are ignored and do not populate
        /// this property.
        /// </remarks>
        public Exception LastError { get; private set; }

        /// <summary>
        /// Creates an API discovery provider.
        /// </summary>
        /// <param name="messageBus">
        /// The mod-message transport used for discovery.
        /// </param>
        /// <param name="channelId">
        /// The shared discovery channel known by providers and consumers of
        /// this API.
        /// </param>
        /// <param name="descriptor">
        /// The API identity and version exposed by the provider.
        /// </param>
        /// <param name="endpoints">
        /// The endpoint delegates exposed to compatible consumers.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an endpoint name or delegate is invalid.
        /// </exception>
        public ApiDiscoveryProvider(
            IModMessageBus messageBus,
            long channelId,
            ApiDescriptor descriptor,
            IDictionary<string, Delegate> endpoints
        )
        {
            if (messageBus == null)
            {
                throw new ArgumentNullException(
                    nameof(messageBus)
                );
            }

            var validated = new ApiAnnouncement(
                descriptor,
                Guid.Empty,
                endpoints
            );

            _messageBus = messageBus;
            ChannelId = channelId;
            Descriptor = validated.Descriptor;

            _endpoints = new Dictionary<string, Delegate>(
                StringComparer.Ordinal
            );

            foreach (
                KeyValuePair<string, Delegate> pair
                in validated.Endpoints
            )
            {
                _endpoints.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Registers the discovery request handler and broadcasts an
        /// unsolicited provider announcement.
        /// </summary>
        /// <remarks>
        /// Repeated calls while already started have no effect.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the provider has been disposed.
        /// </exception>
        public void Start()
        {
            ThrowIfDisposed();

            if (IsStarted)
                return;

            var subscription = new ModMessageSubscription(
                _messageBus,
                ChannelId,
                HandleMessage
            );

            _subscription = subscription;
            IsStarted = true;

            try
            {
                Announce();
            }
            catch
            {
                IsStarted = false;
                _subscription = null;
                subscription.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Broadcasts an unsolicited provider announcement.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when the provider has been disposed.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the provider has not been started.
        /// </exception>
        public void Announce()
        {
            ThrowIfDisposed();

            if (!IsStarted)
            {
                throw new InvalidOperationException(
                    "The API discovery provider has not been started."
                );
            }

            object payload =
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    Descriptor,
                    Guid.Empty,
                    _endpoints
                );

            _messageBus.Send(ChannelId, payload);
            LastError = null;
        }

        /// <summary>
        /// Unregisters the discovery request handler.
        /// </summary>
        /// <remarks>
        /// Repeated calls while already stopped have no effect. The provider
        /// may be started again after being stopped.
        /// </remarks>
        public void Stop()
        {
            if (_isDisposed || !IsStarted)
                return;

            _subscription.Dispose();

            _subscription = null;
            IsStarted = false;
        }

        /// <summary>
        /// Stops the provider and permanently releases its registration.
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
                ApiDiscoveryRequest request;

                if (!ApiDiscoveryWireProtocol.TryParseRequest(
                    payload,
                    out request
                ))
                {
                    return;
                }

                if (!string.Equals(
                    Descriptor.ApiId,
                    request.ApiId,
                    StringComparison.Ordinal
                ))
                {
                    return;
                }

                object response =
                    ApiDiscoveryWireProtocol.CreateAnnouncement(
                        Descriptor,
                        request.CorrelationId,
                        _endpoints
                    );

                _messageBus.Send(ChannelId, response);
                LastError = null;
            }
            catch (Exception exception)
            {
                LastError = exception;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(
                    nameof(ApiDiscoveryProvider)
                );
            }
        }
    }
}