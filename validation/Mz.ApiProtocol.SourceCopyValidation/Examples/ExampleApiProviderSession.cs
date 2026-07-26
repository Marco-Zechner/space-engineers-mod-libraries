using System;
using System.Collections.Generic;
using Mz.ApiProtocol;
using Mz.ApiProtocol.SpaceEngineers;
using Mz.SemanticVersioning;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Example.ApiProviderMod
{
    /// <summary>
    /// Complete example mod that exposes a small Echo API.
    ///
    /// Copy this entire file into the provider mod. Install Mz.ApiProtocol in
    /// that mod before compiling it.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class ExampleApiProviderSession :
        MySessionComponentBase
    {
        // These values form the public contract. Consumers must use the same
        // API ID, endpoint names, and exact delegate types.
        public const string ApiId = "Example.EchoApi";
        public const string EchoEndpoint = "Echo";
        public const string AddEndpoint = "Add";

        private ApiDiscoveryProvider _provider;

        public override void BeforeStart()
        {
            var providerIdentity = new ApiModIdentity(
                "example.echo-api-provider",
                "Example Echo API Provider",
                new SemanticVersion(1, 0, 0)
            );

            var apiDescriptor = new ApiDescriptor(
                ApiId,
                new SemanticVersion(1, 0, 0)
            );

            var endpoints =
                new Dictionary<string, Delegate>(
                    StringComparer.Ordinal
                )
                {
                    {
                        EchoEndpoint,
                        new Func<string, string>(Echo)
                    },
                    {
                        AddEndpoint,
                        new Func<int, int, int>(Add)
                    }
                };

            _provider = new ApiDiscoveryProvider(
                new SpaceEngineersModMessageBus(),
                providerIdentity,
                apiDescriptor,
                endpoints
            );

            _provider.ConsumerObserved += OnConsumerObserved;
            _provider.WireIncompatibilityObserved +=
                OnWireIncompatibilityObserved;

            // Start registers the discovery handler and immediately announces
            // that this provider is available.
            _provider.Start();

            ShowMessage(
                "Provider started for " + ApiId + " 1.0.0."
            );
        }

        protected override void UnloadData()
        {
            if (_provider == null)
                return;

            try
            {
                // Dispose sends a withdrawal and unregisters the exact
                // discovery handler.
                _provider.Dispose();
            }
            catch (Exception exception)
            {
                ShowMessage(
                    "Provider shutdown failed: " + exception.Message
                );
            }
            finally
            {
                _provider = null;
            }
        }

        private static string Echo(string value)
        {
            return "Provider echoed: " + (value ?? string.Empty);
        }

        private static int Add(int left, int right)
        {
            return left + right;
        }

        private static void OnConsumerObserved(
            ApiConsumerObservedEventArgs eventArgs
        )
        {
            ShowMessage(
                "Observed consumer "
                + eventArgs.Consumer.DisplayName
                + ". Compatibility: "
                + eventArgs.CompatibilityStatus
                + "."
            );
        }

        private static void OnWireIncompatibilityObserved(
            ApiWireIncompatibilityEventArgs eventArgs
        )
        {
            ShowMessage(
                "Incompatible discovery protocol from "
                + eventArgs.RemoteMod.DisplayName
                + ": "
                + eventArgs.CompatibilityStatus
                + "."
            );
        }

        private static void ShowMessage(string message)
        {
            // Replace this helper with the provider mod's normal logger when
            // moving beyond the example.
            if (MyAPIGateway.Utilities == null)
                return;

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            MyAPIGateway.Utilities.ShowMessage(
                "API Provider",
                message
            );
        }
    }
}
