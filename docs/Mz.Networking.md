# Mz.Networking

`Mz.Networking` provides trusted, message-type-based multiplayer routing for
Space Engineers mods.

The library is split into two source-copy-safe layers:

- `Mz.Networking.Core` contains envelopes, handler registration, sender
  validation, routing decisions, and the transport-independent endpoint.
- `Mz.Networking.SpaceEngineers` connects the core to the Space Engineers
  secure multiplayer message API and binary serializer.

The current adapter owns one caller-selected `ushort` channel. Dynamic channel
allocation and coordinator leasing are intentionally outside this first
transport slice.

## Basic lifecycle

Create one `SpaceEngineersNetworkSession` while the Space Engineers session is
active. Dispose every handler subscription and then dispose the network
session during unload.

    using Mz.Networking;
    using Mz.Networking.SpaceEngineers;

    private SpaceEngineersNetworkSession _network;
    private NetworkMessageSubscription _commandSubscription;

    public void LoadNetworking()
    {
        _network = new SpaceEngineersNetworkSession(
            41000,
            "Mz.Command.Network",
            OnNetworkFailure
        );

        _commandSubscription =
            _network.Endpoint.RegisterHandler(
                "Mz.Command.Execute",
                OnCommandReceived
            );
    }

    public void UnloadNetworking()
    {
        if (_commandSubscription != null)
        {
            _commandSubscription.Dispose();
            _commandSubscription = null;
        }

        if (_network != null)
        {
            _network.Dispose();
            _network = null;
        }
    }

The session registers one exact secure-message callback and removes that same
callback when disposed. Disposal is idempotent.

The explicit network ID is trimmed, case-sensitive, and limited to 256 UTF-8
bytes. It is written to a versioned Mz.Networking wire header so another
application identity on the same channel can be classified as a conflict. The
legacy constructor without a network ID remains available for byte-for-byte
compatibility with existing unframed Mz.Networking traffic.

## Sending messages

Application payloads are opaque `byte[]` values. Application code owns their
format and versioning.

A client sends a message to the authoritative server through the endpoint:

    _network.Endpoint.SendToServer(
        "Mz.Command.Execute",
        serializedCommand
    );

When called by the server, `SendToServer` dispatches locally without sending a
multiplayer packet.

The server can send directly to one player:

    _network.Endpoint.SendToPlayer(
        "Mz.Command.Result",
        serializedResult,
        targetSteamId
    );

Calling `SendToPlayer` from a client throws `InvalidOperationException`.

## Receiving and relaying

Register one handler per message type:

    private void OnCommandReceived(
        NetworkReceiveContext context
    )
    {
        byte[] payload = context.Envelope.Payload;

        if (!context.IsServer)
        {
            ApplyCommandResult(payload);
            return;
        }

        ExecuteValidatedCommand(
            context.Envelope.OriginalSenderId,
            payload
        );

        context.RelayMode = NetworkRelayMode.ToOthers;
    }

Message types use ordinal, case-sensitive comparison after surrounding
whitespace is removed. Duplicate registrations for the same normalized type
are rejected.

A server handler may select one relay mode:

- `None`: do not relay.
- `ToOthers`: send to every connected client except the original sender.
- `ToEveryone`: send to every connected client, including the original sender.
- `ReturnToSender`: send only to the original sender.

The Space Engineers transport enumerates connected players for server
broadcasts. It never sends a relay packet back to the server's own local peer.

## Sender trust guarantees

The secure Space Engineers callback supplies both the immediate sender ID and
whether that sender is the server. The adapter passes both values explicitly
into the core.

Before application code receives a message:

- the server replaces a client-claimed original sender ID with the trusted
  callback sender ID;
- the server removes a relay flag forged by a client;
- a client rejects any packet whose secure callback does not identify the
  sender as the authoritative server;
- client sender trust is checked before message-type lookup, including for
  unknown message types.

The validated values are exposed through `NetworkReceiveContext`:

- `Envelope.OriginalSenderId`
- `TransportSenderId`
- `IsServer`
- `TransportSenderIsServer`
- `OriginalSenderWasCorrected`
- `RelayFlagWasCorrected`

`NetworkEnvelope` copies payload arrays when constructed and when payload data
is read.

## Receive failures

`SpaceEngineersNetworkSession` reports deserialization, trust, dispatch, and
handler failures through the callback supplied to its constructor:

    private void OnNetworkFailure(
        SpaceEngineersNetworkReceiveFailure failure
    )
    {
        Log(
            "Network packet failed on channel "
            + failure.ChannelId
            + " from peer "
            + failure.SenderPeerId
            + ": "
            + failure.Exception
        );
    }

The failure object contains:

- channel ID;
- immediate sender peer ID;
- whether the sender was identified as the server;
- failure `Kind`;
- `IsChannelConflict`;
- expected and observed network IDs when available;
- the exception;
- a copy of the serialized packet.

Only packets without Mz.Networking magic and packets carrying another network
ID are channel-conflict evidence. Unsupported versions, malformed recognized
Mz.Networking framing, malformed own envelopes, processing failures, and
application-handler failures are reported without marking a channel conflict.

## Source-copy use

Space Engineers mods can source-copy all `.cs` files from:

- `src/Mz.Networking.Core`
- `src/Mz.Networking.SpaceEngineers`

The Space Engineers layer requires the game's `ProtoBuf.Net.Core`,
`Sandbox.Common`, and `VRage.Game` assemblies.

`validation/Mz.Networking.SourceCopyValidation` compiles both source folders as
linked files under `Mal.Mdk2.ModAnalyzers`. The adapter project references this
validation project as a build dependency, so normal solution verification also
checks the mod whitelist.

## Current scope

This implementation provides:

- fixed-channel secure-message lifecycle;
- optional versioned wire identity with conflict classification;
- legacy unframed-wire compatibility;
- binary envelope serialization;
- trusted original-sender correction;
- forged-relay correction;
- message-type handler ownership;
- client-to-server sends;
- server-to-player sends;
- server relay decisions;
- transport-independent core tests;
- Space Engineers adapter tests;
- MDK source-copy validation.

It does not yet provide:

- dynamic channel leasing;
- a known control channel;
- coordinator or ModManager integration;
- automatic application-payload serialization;
- request/response correlation;
- retries or delivery guarantees.
