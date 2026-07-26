using System;
using System.Text;
using Mz.Networking;
using Mz.Networking.SpaceEngineers;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Example.NetworkingMod
{
    /// <summary>
    /// Complete authoritative-server half of the networking example.
    ///
    /// Copy this file and ExampleNetworkClientSession.cs into the same mod.
    /// Install Mz.Networking in that mod before compiling it.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public sealed class ExampleNetworkServerSession :
        MySessionComponentBase
    {
        // Pick one channel that is unique to the mod. The client file must use
        // exactly the same value.
        private const ushort ChannelId = 45123;

        // Message types are case-sensitive and must match the client file.
        private const string PingRequestMessage =
            "example.networking.ping.request";

        private const string PingResponseMessage =
            "example.networking.ping.response";

        private SpaceEngineersNetworkSession _network;
        private NetworkMessageSubscription _pingSubscription;

        public override void BeforeStart()
        {
            if (
                MyAPIGateway.Multiplayer == null
                || !MyAPIGateway.Multiplayer.IsServer
            ) {
                return;
            }

            _network = new SpaceEngineersNetworkSession(
                ChannelId,
                OnReceiveFailure
            );

            _pingSubscription =
                _network.Endpoint.RegisterHandler(
                    PingRequestMessage,
                    OnPingRequest
                );

            ShowMessage(
                "Server handler registered on channel "
                + ChannelId
                + "."
            );
        }

        protected override void UnloadData()
        {
            if (_pingSubscription != null)
            {
                _pingSubscription.Dispose();
                _pingSubscription = null;
            }

            if (_network != null)
            {
                _network.Dispose();
                _network = null;
            }
        }

        private void OnPingRequest(
            NetworkReceiveContext context
        )
        {
            string requestText = Encoding.UTF8.GetString(
                context.Envelope.Payload
            );

            // The server has already validated this ID against the trusted
            // transport sender.
            ulong requestingPeerId =
                context.Envelope.OriginalSenderId;

            string responseText =
                "Pong from the authoritative server. You sent: "
                + requestText;

            _network.Endpoint.SendToPlayer(
                PingResponseMessage,
                Encoding.UTF8.GetBytes(responseText),
                requestingPeerId
            );

            ShowMessage(
                "Answered peer "
                + requestingPeerId
                + ": "
                + requestText
            );
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
            // Replace this with the mod's normal logger for production use.
            if (MyAPIGateway.Utilities == null)
                return;

            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            MyAPIGateway.Utilities.ShowMessage(
                "Network Server",
                message
            );
        }
    }
}
