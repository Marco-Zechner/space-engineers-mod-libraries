# SELibs package releases

This repository publishes source-copy libraries in the format consumed by
SELibs.

## Packages

| Package | Owned source folders | Exact dependencies |
| --- | --- | --- |
| `Mz.SemanticVersioning` | `Mz.SemanticVersioning` | None |
| `Mz.ApiProtocol` | `Mz.ApiProtocol.Core`, `Mz.ApiProtocol.SpaceEngineers` | `Mz.SemanticVersioning` `0.1.0` |
| `Mz.Logging` | `Mz.Logging.Core`, `Mz.Logging.SpaceEngineers` | None |
| `Mz.Networking` | `Mz.Networking.Core`, `Mz.Networking.SpaceEngineers` | None |

Each package archive contains only its own folders. Dependencies are separate
SELibs packages and are not duplicated inside dependent archives.

## Release tags

Tags use:

    <slug>/v<major.minor.patch>

Examples:

    semanticversioning/v0.1.0
    apiprotocol/v0.2.0
    logging/v0.1.0
    networking/v0.1.0

Release revisions are not appended to tags. A published version is immutable;
changed source requires a new package version.

## Release assets

For package `Mz.ApiProtocol` version `0.2.0`, the workflow publishes:

    Mz.ApiProtocol-0.2.0-package.json
    Mz.ApiProtocol-0.2.0-component.zip

The component ZIP has exactly one root:

    Libraries/
      Mz.ApiProtocol.Core/
      Mz.ApiProtocol.SpaceEngineers/

The package manifest records the package ID, numeric version, exact
dependencies, owned folders, component filename, and SHA-256 checksum.
