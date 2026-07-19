# Space Engineers API Ecosystem Vision

## Status

This document describes a longer-term ecosystem built around reusable API protocol libraries and one optional in-game API manager mod.

It is a design direction, not the initial implementation scope. The first priority remains a small, reliable provider/consumer protocol that lets mods discover each other regardless of load order.

## Goals

The ecosystem should make cross-mod APIs:

- independent of mod load order;
- discoverable at runtime;
- explicit about identity and versions;
- usable as either required or optional integrations;
- diagnosable by mod authors and players;
- tolerant of compatible older library copies;
- observable through an optional manager mod;
- safe by default, with overrides clearly marked as risky.

The protocol libraries are copied or vendored into mods. They are not separate runtime dependencies that players must install.

## Core loading problem

Two mods may depend on each other in either direction:

- Mod A may consume an API from Mod B.
- Mod B may consume an API from Mod A.
- Either mod may load first.
- One or both integrations may be optional.
- A provider may become available after a consumer has already initialized.

The protocol therefore cannot rely on one startup-time announcement alone.

## Lazy discovery model

Consumers should be able to ask for an API whenever they need it.

A consumer lifecycle should support:

1. Registering an announcement listener.
2. Sending a discovery request for a specific API ID.
3. Receiving zero or more provider announcements.
4. Evaluating each provider against the consumer's supported version range.
5. Accepting one compatible provider.
6. Ignoring duplicate announcements from the same provider.
7. Repeating discovery later when no provider was initially available.
8. Detecting provider disappearance or session unload when supported.
9. Reconnecting when a compatible provider becomes available again.

Providers should support:

1. Registering a discovery-request listener.
2. Responding to matching requests.
3. Broadcasting an unsolicited announcement when they become ready.
4. Responding repeatedly without producing duplicate consumer connections.
5. Unregistering all handlers during unload.

This allows either side to load first:

- Provider first: it broadcasts; later consumers can also request.
- Consumer first: it requests; a later provider broadcasts when ready.
- Both already loaded: the consumer can request again lazily.

## Discovery semantics

A discovery request should contain:

- protocol marker and protocol wire version;
- requested API ID;
- request correlation ID;
- optional consumer identity in a later protocol revision;
- optional dependency intent in a later protocol revision.

A provider announcement should contain:

- protocol marker and protocol wire version;
- API ID;
- provider API version;
- correlation ID, or an empty value for an unsolicited announcement;
- endpoint dictionary;
- provider identity in a later protocol revision;
- capability and metadata fields in later protocol revisions.

The initial wire format should use only runtime types shared safely across separately compiled mod assemblies, such as:

- `string`;
- `Guid`;
- arrays;
- dictionaries;
- delegates;
- primitive numeric values.

Library-defined message classes should remain internal parsed models and should not themselves be sent through mod messages.

## Required and optional dependencies

A consuming mod should be able to describe each dependency as one of:

### Required

The mod cannot provide its intended functionality without the API.

Possible manager behavior:

- show an error when no compatible provider exists;
- identify the missing API and expected version range;
- identify the consuming mod;
- optionally expose troubleshooting details.

The protocol library itself should not automatically unload or disable a mod. The consuming mod decides what failure means.

### Optional

The mod works without the API but enables an integration when a compatible provider is available.

Possible manager behavior:

- show the integration as unavailable rather than broken;
- explain which feature is disabled;
- update the status if a provider appears later.

### Development-only or diagnostic

A mod may use an API only for debugging, profiling, editor tooling, or administrative commands.

This distinction may be added later if it proves useful.

## API identity

API IDs should be globally distinctive and stable.

Recommended form:

```text
AuthorOrOrganization.ApiName
```

Example:

```text
Mz.CommandAPI
```

API IDs should be compared using ordinal, case-sensitive comparison.

The API ID identifies the contract, not a workshop item or mod package. A provider mod may expose multiple API IDs, and multiple mods could theoretically implement the same API contract.

## Version model

Provider APIs expose a semantic version:

```text
major.minor.patch
```

Consumers declare a supported range using a minimum-inclusive and maximum-exclusive interval:

```text
[1.2.0, 2.0.0)
```

This means:

- `1.2.0` is supported;
- `1.9.9` is supported;
- `2.0.0` is not supported.

Compatibility status should remain explicit:

- compatible;
- different API;
- provider too old;
- provider too new.

The protocol should not silently treat every newer version as compatible.

## Multiple embedded library versions

Because each mod embeds its own copy of the protocol library, different mods may contain different library revisions.

That is acceptable as long as:

- wire protocol revisions remain backward compatible where intended;
- sent payloads use shared runtime types;
- parsing rejects malformed or unknown messages safely;
- protocol markers include a wire revision;
- public behavior is documented;
- older implementations continue working unless a breaking protocol revision is intentionally introduced.

The manager mod may report embedded protocol-library versions, but outdated alone should not be treated as an error.

## Provider selection

Initially, a consumer may simply accept the first compatible provider.

A more complete selection policy may later consider:

- exact versus broad compatibility;
- provider priority;
- provider implementation identity;
- provider API version;
- provider health;
- user overrides;
- deterministic ordering.

Selection must be idempotent. Receiving the same announcement repeatedly must not reconnect, duplicate callbacks, or leak registrations.

If several compatible providers expose the same API, the manager should show all candidates and identify the active selection.

## Optional API manager mod

A future mod can observe and present the API ecosystem without being required for normal API operation.

Possible name:

```text
API Manager
```

It should consume protocol metadata and diagnostics rather than becoming a mandatory central broker.

Normal provider/consumer communication must continue working when the manager is not installed.

## Manager responsibilities

### Installed API overview

Show:

- provider mods;
- APIs exposed by each provider;
- provider API versions;
- protocol-library versions;
- available endpoint names;
- provider readiness;
- consumers currently connected;
- incompatible consumers;
- optional integrations not currently active.

### Dependency graph

Visualize relationships such as:

```text
Mod A -> requires -> API C
Mod B -> optionally uses -> API C
Mod C -> provides -> API C
```

The graph should distinguish:

- required dependency;
- optional integration;
- compatible connection;
- missing provider;
- provider too old;
- provider too new;
- overridden incompatibility;
- unresolved provider ambiguity.

### Version-conflict detection

Example:

- Mod A supports API C version `[2.0.0, 3.0.0)`.
- Mod B supports API C version `[1.0.0, 2.0.0)`.
- Installed Mod C provides API C version `2.1.0`.

Result:

- Mod A is compatible.
- Mod B reports `ProviderTooNew`.
- The manager shows that Mod B likely needs an update.
- The manager can present the consuming mod and expected range.
- The player can be advised to inform the mod author.

This is more useful than merely saying that one embedded library is old. It identifies an actual contract mismatch.

### Conflict override

The manager may later provide an explicit option to force acceptance of an incompatible provider.

This must be treated as unsafe:

- disabled by default;
- scoped to a specific consumer/provider/API combination;
- persisted per world or per user only when clearly explained;
- visibly marked in the dependency graph;
- easy to revoke;
- never silently enabled.

An override does not make versions compatible. It only instructs a willing consumer to attempt connection despite its declared range.

Consumers must explicitly support override requests. The manager should not mutate arbitrary mods or bypass their validation invisibly.

### Changelogs

Providers may publish:

- mod version;
- API version;
- API changelog;
- migration notes;
- deprecations;
- breaking changes;
- endpoint additions and removals.

The manager could show:

- the installed provider version;
- changes since a consumer's last known compatible version;
- API-specific changes separately from general mod changes;
- links or identifiers for workshop and source pages when available.

Changelog metadata should be optional and bounded. The discovery transport should not broadcast large changelog documents repeatedly.

A later metadata query could retrieve detailed information only when the manager requests it.

### Raw API command console

A manager console could allow advanced users or modders to invoke explicitly exposed diagnostic endpoints, conceptually similar to an HTTP client.

Example:

```text
api call Mz.CommandAPI ListCommands
```

This should not make every delegate arbitrarily invokable.

A safe design should require providers to publish invocation metadata:

- endpoint name;
- purpose;
- parameter schema;
- return schema;
- whether invocation is read-only;
- whether invocation is administrative;
- whether invocation is safe for players;
- whether invocation is intended only for other mods.

The manager must never guess how to invoke an arbitrary delegate.

Raw invocation is best treated as a later diagnostic protocol layered on top of normal typed mod integrations.

## Metadata model

A future provider metadata record may include:

- provider ID;
- provider display name;
- workshop ID;
- source repository;
- API ID;
- API version;
- protocol-library version;
- provider mod version;
- endpoint summaries;
- API changelog reference;
- deprecation notices;
- support contact;
- documentation reference.

A future consumer metadata record may include:

- consumer ID;
- consumer display name;
- requested API ID;
- supported API range;
- required or optional intent;
- feature enabled by the dependency;
- current connection status;
- last compatibility result.

Metadata should remain separate from the minimal discovery handshake so ordinary mods pay little overhead.

## Observability

Provider and consumer implementations should expose lifecycle events or diagnostic snapshots that a manager can observe.

Potential events:

- discovery request sent;
- provider announcement received;
- incompatible provider observed;
- provider accepted;
- duplicate announcement ignored;
- provider replaced;
- connection lost;
- retry scheduled;
- consumer stopped;
- provider stopped.

Diagnostics should avoid leaking private internal mod state or permitting arbitrary code execution.

## Error handling

Malformed or unrelated mod messages must be ignored safely.

The protocol should:

- never throw from a shared mod-message handler due to a malformed payload;
- validate marker, field count, field types, IDs, versions, and endpoint dictionaries;
- keep parsing separate from business logic;
- make cleanup idempotent;
- avoid reporting a connection before validation and callback completion;
- preserve enough status information for diagnostics.

Provider callbacks and consumer callbacks may throw. The protocol lifecycle must define whether such failures:

- reject the connection;
- preserve the previous connection;
- trigger cleanup;
- become observable diagnostics.

## Security and trust boundaries

All installed mods execute within the same game process and are not strongly isolated from each other. The protocol should still minimize accidental misuse.

Recommended rules:

- only expose delegates intentionally included by the provider;
- validate endpoint names and delegate signatures;
- do not expose arbitrary reflection-based invocation;
- do not allow the manager to force unsupported calls;
- mark global storage as collision-prone;
- keep override actions explicit;
- avoid sending secrets through mod messages;
- treat all received payloads as untrusted input.

## Lifecycle requirements

Every provider and consumer object should have explicit lifecycle ownership.

Provider lifecycle:

1. Construct configuration and endpoint table.
2. Start during the active session lifecycle.
3. Register request handler.
4. Broadcast readiness.
5. Respond to matching requests.
6. Stop during unload.
7. Unregister handlers exactly once.

Consumer lifecycle:

1. Construct requirement and callbacks.
2. Start during the active session lifecycle.
3. Register announcement handler.
4. Request discovery.
5. Accept a compatible provider.
6. Retry lazily when needed.
7. Stop during unload.
8. Unregister handlers and release provider state.

Static initialization should not access `MyAPIGateway`.

## Documentation requirement

All public types and all public members in these libraries should include XML documentation.

This includes:

- classes;
- interfaces;
- enums and enum values;
- constructors;
- properties;
- methods;
- parameters where behavior is not obvious;
- return values;
- exceptions;
- lifecycle expectations;
- unsafe or collision-prone operations.

Warnings from generated XML documentation should remain enabled and treated as errors where practical.

## Proposed implementation phases

### Phase 1: Minimal discovery

- transport-safe request and announcement messages;
- provider request listener;
- provider unsolicited announcement;
- consumer announcement listener;
- consumer request method;
- compatibility evaluation;
- idempotent first-compatible-provider selection;
- explicit disposal.

### Phase 2: Lazy reconnection

- repeated discovery requests;
- retry policy controlled by the consumer;
- provider replacement policy;
- connection-lost state;
- diagnostic events.

### Phase 3: Dependency metadata

- consumer identity;
- provider identity;
- required versus optional intent;
- feature description;
- status snapshots;
- manager-readable registry messages.

### Phase 4: API manager

- provider and consumer overview;
- dependency graph;
- compatibility warnings;
- troubleshooting output;
- library and protocol version reporting.

### Phase 5: Conflict overrides

- explicit override records;
- opt-in consumer support;
- visible unsafe status;
- revocation and persistence rules.

### Phase 6: API documentation and diagnostic invocation

- endpoint metadata;
- API changelogs;
- migration notes;
- safe diagnostic endpoints;
- optional raw-call console for explicitly supported operations.

## Near-term next step

The next implementation slice should build provider and consumer discovery lifecycles on top of:

- `IModMessageBus`;
- `ModMessageSubscription`;
- `ApiDiscoveryWireProtocol`;
- `ApiDescriptor`;
- `ApiRequirement`.

The initial consumer should expose an explicit `RequestDiscovery()` method so mods can ask lazily at any point during their active session.

Automatic timed retries should not be built into the first slice. Consumers can decide when retries make sense, and a reusable retry policy can be added after the basic lifecycle is proven.