using System;
using System.Collections.Generic;
using Mz.ApiProtocol;

namespace Mz.ApiProtocol.SpaceEngineers
{
    /// <summary>
    /// Exposes one API through the Space Engineers mod-message discovery
    /// protocol.
    /// </summary>
    public sealed class ApiDiscoveryProvider : IDisposable
    {
        private readonly IModMessageBus _messageBus;
        private readonly Dictionary<string, Delegate> _endpoints;

        private ModMessageSubscription _subscription;
        private bool _withdrawalSent;
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
        /// Gets the identity of this provider instance.
        /// </summary>
        public Guid ProviderInstanceId { get; }

        /// <summary>
        /// Gets whether the provider is currently listening for requests.
        /// </summary>
        public bool IsStarted { get; private set; }

        /// <summary>
        /// Gets the last unexpected lifecycle or message-processing error.
        /// </summary>
        public Exception LastError { get; private set; }

        /// <summary>
        /// Creates a provider with a generated provider-instance identity.
        /// </summary>
        /// <param name="messageBus">
        /// The mod-message transport used for discovery.
        /// </param>
        /// <param name="channelId">
        /// The shared API discovery channel.
        /// </param>
        /// <param name="descriptor">
        /// The API identity and version exposed by the provider.
        /// </param>
        /// <param name="endpoints">
        /// The endpoint delegates exposed by the provider.
        /// </param>
        public ApiDiscoveryProvider(
            IModMessageBus messageBus,
            long channelId,
            ApiDescriptor descriptor,
            IDictionary<string, Delegate> endpoints
        )
            : this(
                messageBus,
                channelId,
                descriptor,
                Guid.NewGuid(),
                endpoints
            )
        {
        }

        /// <summary>
        /// Creates a provider with an explicit provider-instance identity.
        /// </summary>
        /// <param name="messageBus">
        /// The mod-message transport used for discovery.
        /// </param>
        /// <param name="channelId">
        /// The shared API discovery channel.
        /// </param>
        /// <param name="descriptor">
        /// The API identity and version exposed by the provider.
        /// </param>
        /// <param name="providerInstanceId">
        /// The non-empty identity of this provider instance.
        /// </param>
        /// <param name="endpoints">
        /// The endpoint delegates exposed by the provider.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required argument is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the provider-instance ID or an endpoint is invalid.
        /// </exception>
        public ApiDiscoveryProvider(
            IModMessageBus messageBus,
            long channelId,
            ApiDescriptor descriptor,
            Guid providerInstanceId,
            IDictionary<string, Delegate> endpoints
        )
        {
            if (messageBus == null)
            {
                throw new ArgumentNullException(
                    nameof(messageBus)
                );
            }

            if (providerInstanceId == Guid.Empty)
            {
                throw new ArgumentException(
                    "A provider requires a non-empty instance identifier.",
                    nameof(providerInstanceId)
                );
            }

            var validated = new ApiAnnouncement(
                descriptor,
                providerInstanceId,
                Guid.Empty,
                endpoints
            );

            _messageBus = messageBus;
            ChannelId = channelId;
            Descriptor = validated.Descriptor;
            ProviderInstanceId = providerInstanceId;

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
        /// Registers the request handler and broadcasts provider readiness.
        /// </summary>
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
            _withdrawalSent = false;
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

            _messageBus.Send(
                ChannelId,
                ApiDiscoveryWireProtocol.CreateAnnouncement(
                    Descriptor,
                    ProviderInstanceId,
                    Guid.Empty,
                    _endpoints
                )
            );

            LastError = null;
        }

        /// <summary>
        /// Announces provider withdrawal and unregisters the request handler.
        /// </summary>
        /// <remarks>
        /// Repeated calls while already stopped have no effect. The provider
        /// may be started again after stopping.
        /// </remarks>
        public void Stop()
        {
            if (_isDisposed || !IsStarted)
                return;

            Exception withdrawalError = null;

            if (!_withdrawalSent)
            {
                try
                {
                    _messageBus.Send(
                        ChannelId,
                        ApiDiscoveryWireProtocol.CreateWithdrawal(
                            Descriptor.ApiId,
                            ProviderInstanceId
                        )
                    );

                    _withdrawalSent = true;
                }
                catch (Exception exception)
                {
                    withdrawalError = exception;
                }
            }

            try
            {
                _subscription.Dispose();
            }
            catch (Exception exception)
            {
                LastError = withdrawalError ?? exception;
                throw;
            }

            _subscription = null;
            IsStarted = false;
            LastError = withdrawalError;

            if (withdrawalError != null)
                throw withdrawalError;
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
                
                if (!ApiProtocolInfo.IsWireProtocolCompatible(
                        request.WireProtocolVersion
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

                _messageBus.Send(
                    ChannelId,
                    ApiDiscoveryWireProtocol.CreateAnnouncement(
                        Descriptor,
                        ProviderInstanceId,
                        request.CorrelationId,
                        _endpoints
                    )
                );

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