using System;
using System.Text;
using Mz.Networking;
using Mz.Networking.SpaceEngineers;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Example.NetworkingMod
{
    /// <summary>
    /// Complete remote-client half of the networking example.
    ///
    /// Copy this file and ExampleNetworkServerSession.cs into the same mod.
    /// Join a multiplayer server with a remote client and type /netping.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class ExampleNetworkClientSession :
        MySessionComponentBase
    {
        // These values must match ExampleNetworkServerSession exactly.
        private const ushort ChannelId = 45123;

        private const string PingRequestMessage =
            "example.networking.ping.request";

        private const string PingResponseMessage =
            "example.networking.ping.response";

        private const string ChatCommand = "/netping";

        private SpaceEngineersNetworkSession _network;
        private NetworkMessageSubscription _responseSubscription;
        private bool _chatHandlerRegistered;

        public override void BeforeStart()
        {
            // The host runs the server component. This component is only for a
            // remote, non-dedicated multiplayer client.
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            if (
                MyAPIGateway.Multiplayer == null
                || MyAPIGateway.Multiplayer.IsServer
            ) {
                return;
            }

            _network = new SpaceEngineersNetworkSession(
                ChannelId,
                OnReceiveFailure
            );

            _responseSubscription =
                _network.Endpoint.RegisterHandler(
                    PingResponseMessage,
                    OnPingResponse
                );

            MyAPIGateway.Utilities.MessageEntered +=
                OnMessageEntered;

            _chatHandlerRegistered = true;

            ShowMessage(
                "Type "
                + ChatCommand
                + " followed by optional text."
            );
        }

        protected override void UnloadData()
        {
            if (
                _chatHandlerRegistered
                && MyAPIGateway.Utilities != null
            ) {
                MyAPIGateway.Utilities.MessageEntered -=
                    OnMessageEntered;
            }

            _chatHandlerRegistered = false;

            if (_responseSubscription != null)
            {
                _responseSubscription.Dispose();
                _responseSubscription = null;
            }

            if (_network != null)
            {
                _network.Dispose();
                _network = null;
            }
        }

        private void OnMessageEntered(
            string message,
            ref bool sendToOthers
        )
        {
            if (message == null)
                return;

            bool exactCommand = string.Equals(
                message,
                ChatCommand,
                StringComparison.OrdinalIgnoreCase
            );

            bool commandWithText = message.StartsWith(
                ChatCommand + " ",
                StringComparison.OrdinalIgnoreCase
            );

            if (!exactCommand && !commandWithText)
                return;

            // Prevent the test command from being sent to normal chat.
            sendToOthers = false;

            string text = exactCommand
                ? "Hello from the remote client"
                : message.Substring(ChatCommand.Length).Trim();

            if (text.Length == 0)
                text = "Hello from the remote client";

            _network.Endpoint.SendToServer(
                PingRequestMessage,
                Encoding.UTF8.GetBytes(text)
            );

            ShowMessage("Ping sent: " + text);
        }

        private static void OnPingResponse(
            NetworkReceiveContext context
        )
        {
            // Clients only accept packets whose immediate transport sender was
            // identified as the authoritative server.
            string responseText = Encoding.UTF8.GetString(
                context.Envelope.Payload
            );

            ShowMessage(responseText);
        }

        private static void OnReceiveFailure(
            SpaceEngineersNetworkReceiveFailure failure
        )
        {
            ShowMessage(
                "Rejected packet on channel "
                + failure.ChannelId
                + " from peer "
                + failure.SenderPeerId
                + ": "
                + failure.Exception.Message
            );
        }

        private static void ShowMessage(string message)
        {
            if (MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.ShowMessage(
                "Network Client",
                message
            );
        }
    }
}
