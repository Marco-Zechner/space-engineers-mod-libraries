# Mz.Networking

`Mz.Networking` provides trusted client/server message routing for Space
Engineers multiplayer mods.

The package contains:

- `Mz.Networking.Core`  -  transport-independent envelopes, handlers, sender
  validation, and relay decisions.
- `Mz.Networking.SpaceEngineers`  -  secure-message serialization, transport,
  and session lifecycle integration.

`Mz.SemanticVersioning` is an exact package dependency shared by all
library packages in this repository.

For complete server and client session components, see the
[copy-paste example](Guide.md).

## Install

### Install with SELibs

[SELibs](https://github.com/Marco-Zechner/selibs) is a source-library manager
for Space Engineers mods. Its repository contains the installer, command
reference, and project setup instructions.

After installing SELibs, run these commands from the root of the mod project:

```shell
    selibs init
    selibs add Mz.Networking@0.1.2
```

Skip `selibs init` when the project already contains `selibs.json`.

SELibs installs both Networking source components and the exact
`Mz.SemanticVersioning` dependency. It records their checksums and selected
versions. Inspect that state with:

```shell
    selibs status
```

### Install manually

To install without SELibs, use the source from the matching release tag in this
repository and copy these complete folders:

```text
    src/Mz.SemanticVersioning
    src/Mz.Networking.Core
    src/Mz.Networking.SpaceEngineers
```

Place them as sibling folders under the mod's script library directory:

```text
    Data/Scripts/ExampleMod/Libraries/Mz.SemanticVersioning
    Data/Scripts/ExampleMod/Libraries/Mz.Networking.Core
    Data/Scripts/ExampleMod/Libraries/Mz.Networking.SpaceEngineers
```

Compile all contained `.cs` files as part of the mod. Networking `0.1.2`
requires `Mz.SemanticVersioning` `0.1.1`. The Space Engineers component also
depends on the Core component, so use the exact dependency versions declared
by the selected package manifest.

## Create a Space Engineers network session

Each session owns one secure-message channel. The same channel ID must be used
by every peer running the mod, and it should not collide with another protocol.

Create the session during the active mod lifecycle and dispose it during
unload. Pass the same stable network ID on every peer. The explicit-ID overload
uses a versioned wire header and can distinguish foreign channel traffic from
another Mz.Networking application identity. The older overload remains
unframed for compatibility with existing deployments.

```csharp
    using System.Text;
    using Mz.Networking;
    using Mz.Networking.SpaceEngineers;

    private SpaceEngineersNetworkSession _network;
    private NetworkMessageSubscription _chatSubscription;

    public override void LoadData()
    {
        _network = new SpaceEngineersNetworkSession(
            45123,
            "example.chat.network",
            failure =>
            {
                MyLog.Default.WriteLineAndConsole(
                    "Example networking failure: "
                    + failure.Exception
                );
            }
        );

        _chatSubscription = _network.Endpoint.RegisterHandler(
            "example.chat",
            context =>
            {
                string text = Encoding.UTF8.GetString(
                    context.Envelope.Payload
                );

                HandleChat(
                    context.Envelope.OriginalSenderId,
                    text
                );

                if (context.IsServer)
                    context.RelayMode = NetworkRelayMode.ToOthers;
            }
        );
    }

    protected override void UnloadData()
    {
        _chatSubscription?.Dispose();
        _chatSubscription = null;

        _network?.Dispose();
        _network = null;
    }
```

The receive-failure callback is required. It receives the channel, raw packet,
transport sender information, failure kind, conflict flag, available network
IDs, and exception when decoding or processing fails. Only foreign wire data
and another network ID are marked as channel conflicts.

## Send messages

Application payloads are byte arrays. Serialize application data before
sending it.

A client sends to the authoritative server with:

```csharp
    byte[] payload = Encoding.UTF8.GetBytes("Hello");

    _network.Endpoint.SendToServer(
        "example.chat",
        payload
    );
```

When the local peer is the server, `SendToServer` dispatches locally instead
of performing a network round trip.

Only the server may send directly to one player:

```csharp
    _network.Endpoint.SendToPlayer(
        "example.notice",
        payload,
        targetPeerId
    );
```

Calling `SendToPlayer` from a client throws.

## Register handlers

Each message type may have one handler per endpoint:

```csharp
    NetworkMessageSubscription subscription =
        _network.Endpoint.RegisterHandler(
            "example.request",
            context =>
            {
                // Process context.Envelope.Payload.
            }
        );
```

Dispose the returned subscription to unregister the exact handler.

Message types are case-sensitive after surrounding whitespace is removed.

## Trusted sender information

Do not trust sender information serialized by a client.

On server receipt, the library validates the envelope against the immediate
transport sender. A forged original-sender ID is replaced with the trusted
transport sender, and a client-forged relay flag is removed.

Handlers receive both the corrected envelope and diagnostics:

- `Envelope.OriginalSenderId`
- `TransportSenderId`
- `TransportSenderIsServer`
- `OriginalSenderWasCorrected`
- `RelayFlagWasCorrected`
- `RequiresSerialization`

Clients reject messages whose immediate transport sender is not identified as
the authoritative server.

## Relay from the server

A receive handler running on the server may select one relay mode:

- `None`  -  do not relay.
- `ToOthers`  -  send to every client except the original sender.
- `ToEveryone`  -  send to every client.
- `ReturnToSender`  -  send only to the original sender.

Example:

```csharp
    if (context.IsServer)
        context.RelayMode = NetworkRelayMode.ReturnToSender;
```

The server applies the relay after the handler returns. Relayed envelopes are
marked as relays by the trusted server.

## Latest-only unreliable state

Unreliable packets may be dropped or arrive out of order. Applications sending
frequent replaceable state can include their own `ushort` sequence in the
payload and reject older updates with:

    if (!NetworkSequence.IsNewer(candidateSequence, currentSequence))
        return;

`NetworkSequence.IsNewer` uses modular unsigned 16-bit comparison. A forward
difference from 1 through 32767 is newer. Equal values are duplicates, and a
difference of exactly 32768 is ambiguous and therefore rejected.

`NetworkSequenceTracker` provides the same rule with first-value and reset
handling:

    private readonly NetworkSequenceTracker _stateSequence =
        new NetworkSequenceTracker();

    private void ApplyState(ushort sequence, byte[] state)
    {
        if (!_stateSequence.TryAccept(sequence))
            return;

        ApplyLatestState(state);
    }

Keep one tracker per independent sender, entity, and application stream. Reset
or recreate that tracker whenever the stream, entity, or multiplayer session is
recreated. Sequence metadata remains application-level and is not added to
every `NetworkEnvelope`.

## Payload ownership

`NetworkEnvelope` copies payload arrays when an envelope is created and when
its `Payload` property is read. Mutating the original array or a returned copy
does not mutate the stored envelope.

## Lower-level integration

`NetworkEndpoint` can be used without Space Engineers by implementing
`INetworkTransport`.

Use `SpaceEngineersNetworkSession` for the normal ModAPI integration because it
owns registration and unregistration of the exact secure-message handler.

## Package version

The released package version is available through:

```csharp
    string packageVersion = Mz.Networking.LibraryVersionFile.VersionString;
```
