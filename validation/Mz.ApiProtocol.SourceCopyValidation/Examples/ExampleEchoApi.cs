using System;
using Mz.ApiProtocol;
using Mz.ApiProtocol.SpaceEngineers;
using Mz.SemanticVersioning;

namespace Example.EchoApi
{
    /// <summary>
    /// Consumer-side wrapper distributed by the Echo API author.
    ///
    /// A consuming mod copies this file unchanged and uses the public methods
    /// below. All ApiProtocol discovery and endpoint details stay private.
    ///
    /// This is an ordinary static class, not a session component. The
    /// consuming mod calls Init during its own startup and Close during unload.
    /// </summary>
    public static class ExampleEchoApi
    {
        private const string ApiId = "Example.EchoApi";
        private const string EchoEndpoint = "Echo";
        private const string AddEndpoint = "Add";

        private static readonly ApiEndpointContract EndpointContract =
            new ApiEndpointContract(
                new[]
                {
                    new ApiEndpointRequirement(
                        EchoEndpoint,
                        typeof(Func<string, string>)
                    ),
                    new ApiEndpointRequirement(
                        AddEndpoint,
                        typeof(Func<int, int, int>)
                    )
                }
            );

        private static ApiDiscoveryConsumer _consumer;
        private static Func<string, string> _echo;
        private static Func<int, int, int> _add;

        /// <summary>
        /// Raised whenever a compatible provider has connected and all
        /// required endpoint delegates are ready to call.
        /// </summary>
        public static event Action Ready;

        /// <summary>
        /// Raised when a ready provider disconnects or the discovered provider
        /// cannot satisfy this wrapper's endpoint contract.
        /// </summary>
        public static event Action Unavailable;

        /// <summary>
        /// Gets whether Init has created the underlying discovery consumer.
        /// </summary>
        public static bool IsInitialized
        {
            get { return _consumer != null; }
        }

        /// <summary>
        /// Gets whether all public API methods are currently callable.
        /// </summary>
        public static bool IsReady
        {
            get
            {
                return _echo != null
                    && _add != null;
            }
        }

        /// <summary>
        /// Gets the latest human-readable connection or contract error.
        /// </summary>
        public static string LastError { get; private set; }

        /// <summary>
        /// Starts discovery for the API.
        ///
        /// The identity arguments describe the consuming mod, not the API
        /// provider. Subscribe to Ready and Unavailable before calling Init,
        /// because an already-running provider may connect synchronously.
        /// </summary>
        public static void Init(
            string consumerModId,
            string consumerDisplayName,
            int consumerVersionMajor,
            int consumerVersionMinor,
            int consumerVersionPatch
        )
        {
            if (_consumer != null)
                return;

            var consumerIdentity = new ApiModIdentity(
                consumerModId,
                consumerDisplayName,
                new SemanticVersion(
                    consumerVersionMajor,
                    consumerVersionMinor,
                    consumerVersionPatch
                )
            );

            var requirement = new ApiRequirement(
                ApiId,
                new ApiVersionRange(
                    new SemanticVersion(1, 0, 0),
                    new SemanticVersion(2, 0, 0)
                )
            );

            var dependency = new ApiDependencyDescriptor(
                consumerIdentity,
                requirement,
                ApiDependencyKind.Required,
                "Uses the Example Echo API"
            );

            var consumer = new ApiDiscoveryConsumer(
                new SpaceEngineersModMessageBus(),
                dependency
            );

            consumer.Connected += OnConnected;
            consumer.Disconnected += OnDisconnected;
            consumer.WireIncompatibilityObserved +=
                OnWireIncompatibilityObserved;

            _consumer = consumer;
            LastError = null;

            try
            {
                consumer.Start();
                consumer.RequestDiscovery();
            }
            catch
            {
                consumer.Connected -= OnConnected;
                consumer.Disconnected -= OnDisconnected;
                consumer.WireIncompatibilityObserved -=
                    OnWireIncompatibilityObserved;

                _consumer = null;
                ClearEndpoints();
                consumer.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Releases the discovery listener and all endpoint delegates.
        /// Call this from the consuming mod's unload lifecycle.
        /// </summary>
        public static void Close()
        {
            var consumer = _consumer;

            _consumer = null;
            ClearEndpoints();
            LastError = null;

            if (consumer != null)
            {
                consumer.Connected -= OnConnected;
                consumer.Disconnected -= OnDisconnected;
                consumer.WireIncompatibilityObserved -=
                    OnWireIncompatibilityObserved;

                consumer.Dispose();
            }

            Ready = null;
            Unavailable = null;
        }

        /// <summary>
        /// Disconnects the current provider, if any, and requests discovery
        /// again.
        /// </summary>
        public static void Refresh()
        {
            EnsureInitialized();
            ClearEndpoints();
            LastError = null;
            _consumer.Rediscover();
        }

        /// <summary>
        /// Calls the provider's Echo endpoint.
        /// </summary>
        public static string Echo(string value)
        {
            EnsureReady();
            return _echo(value);
        }

        /// <summary>
        /// Calls the provider's Add endpoint.
        /// </summary>
        public static int Add(int left, int right)
        {
            EnsureReady();
            return _add(left, right);
        }

        private static void OnConnected(
            ApiConnectedEventArgs eventArgs
        )
        {
            try
            {
                EndpointContract.EnsureCompatible(
                    eventArgs.Connection
                );

                Func<string, string> echo;
                Func<int, int, int> add;

                if (
                    !eventArgs.Connection.TryGetEndpoint(
                        EchoEndpoint,
                        out echo
                    )
                ) {
                    throw new InvalidOperationException(
                        "The Echo endpoint could not be loaded."
                    );
                }

                if (
                    !eventArgs.Connection.TryGetEndpoint(
                        AddEndpoint,
                        out add
                    )
                ) {
                    throw new InvalidOperationException(
                        "The Add endpoint could not be loaded."
                    );
                }

                _echo = echo;
                _add = add;
                LastError = null;

                RaiseEvent(Ready);
            }
            catch (Exception exception)
            {
                ClearEndpoints();

                LastError =
                    "The connected provider has an invalid endpoint "
                    + "contract: "
                    + exception.Message;

                RaiseEvent(Unavailable);
            }
        }

        private static void OnDisconnected(
            ApiDisconnectedEventArgs eventArgs
        )
        {
            bool wasReady = IsReady;

            ClearEndpoints();

            LastError =
                "The API provider disconnected: "
                + eventArgs.Reason
                + ".";

            if (wasReady)
                RaiseEvent(Unavailable);
        }

        private static void OnWireIncompatibilityObserved(
            ApiWireIncompatibilityEventArgs eventArgs
        )
        {
            LastError =
                "The provider uses an incompatible discovery protocol: "
                + eventArgs.CompatibilityStatus
                + ".";

            RaiseEvent(Unavailable);
        }

        private static void ClearEndpoints()
        {
            _echo = null;
            _add = null;
        }

        private static void EnsureInitialized()
        {
            if (_consumer == null)
            {
                throw new InvalidOperationException(
                    "Call ExampleEchoApi.Init before using the API."
                );
            }
        }

        private static void EnsureReady()
        {
            EnsureInitialized();

            if (!IsReady)
            {
                throw new InvalidOperationException(
                    "The Example Echo API is not ready. "
                    + (LastError ?? "No compatible provider is connected.")
                );
            }
        }

        private static void RaiseEvent(Action callback)
        {
            if (callback == null)
                return;

            Delegate[] handlers = callback.GetInvocationList();

            for (int index = 0; index < handlers.Length; index++)
            {
                try
                {
                    ((Action)handlers[index])();
                }
                catch (Exception exception)
                {
                    LastError =
                        "An API wrapper event subscriber failed: "
                        + exception.Message;
                }
            }
        }
    }
}
