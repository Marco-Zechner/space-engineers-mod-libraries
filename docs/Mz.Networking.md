# Mz.Networking

`Mz.Networking` provides trusted, message-type-based multiplayer routing for
Space Engineers mods.

The library is split into two source-copy-safe layers:

- `Mz.Networking.Core` contains envelopes, handler registration, sender
  validation, routing decisions, delivery modes, and application sequence
  helpers.
- `Mz.Networking.SpaceEngineers` connects the core to the Space Engineers
  secure multiplayer API, versioned wire framing, diagnostics, and optional
  managed-channel discovery.

The package has exact dependencies on `Mz.ApiProtocol` `0.2.5` and
`Mz.SemanticVersioning` `0.1.1`.

A session may use a legacy unframed fixed channel, a fixed channel with
versioned network identity, or a managed channel that starts on a preferred
fallback and accepts newer NetworkManager assignments.

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

## Managed channels and NetworkManagerMod

`NetworkManagerMod` is an optional provider for the `Mz.NetworkManager` API.
`Mz.Networking.SpaceEngineers` discovers it through `Mz.ApiProtocol`; the
library does not bundle or require the provider mod.

    var configuration =
        new SpaceEngineersManagedNetworkConfiguration(
            "Mz.Command",
            "Command Mod",
            new SemanticVersion(1, 0, 0),
            "Mz.Command.Network",
            "Command network",
            41000,
            null
        );

    _network =
        new SpaceEngineersNetworkSession(
            configuration
        );

The session registers the preferred channel immediately, so networking remains
available while no provider is connected. A compatible provider can return a
channel and provider-scoped generation. Only a strictly newer generation from
the active provider is accepted. Channel changes are deferred through the
Space Engineers game-thread scheduler when available, and
`ChannelAssignmentApplied` is raised after the assignment is active.

Public managed state is available through `ChannelId`,
`AssignmentGeneration`, `IsNetworkManagerConnected`, `NetworkManagerError`,
and `IsForcedChannel`. Supplying a non-null forced channel disables discovery
and reassignment.

NetworkManager API 1.0 exposes `RegisterNetwork`. API 1.1 also exposes
`RegisterNetworkWithConflictReporting`. With API 1.1, Mz.Networking reports one
active `ForeignPacket` or `NetworkMismatch` conflict for the current channel
and generation. NetworkManager validates the report, persists a
per-logical-network channel blacklist, and may issue a replacement assignment.
Duplicate, stale, wrong-channel, disconnected, and unregistered reports are
ignored.

NetworkManager calculates assignments process-locally rather than synchronizing
them from the server. Every peer should load the same logical-network
registration set so deterministic assignments match.

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

Reliable delivery is the default. Use the overload taking
`NetworkDeliveryMode.Unreliable` for frequent replaceable state. Space
Engineers rejects unreliable packets larger than 1024 bytes after envelope
serialization and optional Mz.Networking framing. A relay handler can set
`RelayDeliveryMode` independently from `RelayMode`.

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
handler failures through its `Diagnostic` event. Existing constructor overloads
can also receive the same failure through a callback:

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

Each failure also contains:

- a recommended `Severity`;
- a stable `DiagnosticCode`;
- a deterministic bounded `DiagnosticMessage`;
- the complete packet length;
- a bounded hexadecimal packet preview;
- bounded sanitized text candidates for channel conflicts.

Constructor overloads without a failure callback support event-only use.
Exceptions from `Diagnostic` subscribers are isolated so logging or telemetry
failures do not interrupt packet processing. Existing callback overloads retain
their previous exception behavior.

Mz.Networking does not depend on Mz.Logging. A consuming mod can map the
severity explicitly and pass the prepared message and exception to its logger:

    _network.Diagnostic += failure =>
    {
        _logger.Write(
            ToLogLevel(failure.Severity),
            failure.DiagnosticMessage,
            failure.Exception
        );
    };

The severity names and numeric values intentionally match
`Mz.Logging.LogLevel`, but an explicit mapping function keeps that relationship
visible at the integration boundary.

## Source-copy use

Space Engineers mods can source-copy all `.cs` files from:

- `src/Mz.SemanticVersioning`
- `src/Mz.ApiProtocol.Core`
- `src/Mz.ApiProtocol.SpaceEngineers`
- `src/Mz.Networking.Core`
- `src/Mz.Networking.SpaceEngineers`

The Space Engineers layer requires the game's `ProtoBuf.Net.Core`,
`Sandbox.Common`, and `VRage.Game` assemblies.

`validation/Mz.Networking.SourceCopyValidation` compiles the package and dependency source folders as
linked files under `Mal.Mdk2.ModAnalyzers`. The adapter project references this
validation project as a build dependency, so normal solution verification also
checks the mod whitelist.

## Current scope

This implementation provides:

- reliable and unreliable delivery selection;
- wrap-aware application sequence helpers;
- fixed and forced secure-message channels;
- optional NetworkManager-managed assignment and reassignment;
- provider-generation validation and conflict reporting;
- versioned wire identity with conflict classification;
- legacy unframed-wire compatibility;
- bounded structured receive diagnostics;
- binary envelope serialization;
- trusted original-sender and relay correction;
- message-type handler ownership;
- client-to-server, server-to-player, and server relay sends;
- transport-independent and Space Engineers adapter tests;
- MDK source-copy validation.

It does not provide:

- authoritative cross-peer synchronization of managed registrations;
- automatic application-payload serialization;
- request/response correlation;
- retries beyond the selected transport delivery mode;
- guaranteed delivery for unreliable packets.
