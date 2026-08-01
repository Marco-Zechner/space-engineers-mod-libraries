# SELibs package releases

This repository publishes source-copy libraries in the format consumed by
SELibs.

## Packages

| Package | Owned source folders | Exact dependencies |
| --- | --- | --- |
| `Mz.SemanticVersioning` | `Mz.SemanticVersioning` | None |
| `Mz.ApiProtocol` | `Mz.ApiProtocol.Core`, `Mz.ApiProtocol.SpaceEngineers` | `Mz.SemanticVersioning` `0.1.1` |
| `Mz.Logging` | `Mz.Logging.Core`, `Mz.Logging.SpaceEngineers` | `Mz.SemanticVersioning` `0.1.1` |
| `Mz.Networking` | `Mz.Networking.Core`, `Mz.Networking.SpaceEngineers` | `Mz.ApiProtocol` `0.2.5`, `Mz.SemanticVersioning` `0.1.1` |

Each package archive contains only its own folders. Dependencies are separate
SELibs packages and are not duplicated inside dependent archives.

## Release tags

Tags use the namespace and numeric version declared by the selected
`LibraryVersionFile.cs`:

    release/<namespace>/<major.minor.patch>

Examples:

    release/Mz.SemanticVersioning/0.1.1
    release/Mz.ApiProtocol/0.2.2
    release/Mz.Logging/0.1.1
    release/Mz.Networking/0.2.0

Both values are matched exactly. Namespace casing must be identical to the
version-file namespace, and the tag version must equal `Major.Minor.Patch`.

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

## Automatic package discovery

Release tooling does not maintain a central list of package names or source
folders.

Every `LibraryVersionFile.cs` beneath `src` defines one package root:

- the file namespace is the SELibs package ID;
- `Major`, `Minor`, and `Patch` define the package version;
- the single project beside the version file is the package root;
- projects without their own version file join a package when their root
  namespace matches that package and they reference one of its projects;
- portable release tests are discovered from test projects that directly
  reference the package's versioned root project.

The complete version-file namespace is the release identity. For example,
namespace `Mz.ApiProtocol` version `0.2.2` uses the exact tag
`release/Mz.ApiProtocol/0.2.2`. No separate slug is derived or configured.

Normal project references to another discovered package become exact package
dependencies. Evaluated source files outside the package's owned source
folders are matched against folder ownership in `selibs.lock.json`.

Only the releasing package's owned source folders are copied into its component
archive. Dependency source is never embedded. Unmapped external source causes
publishing to fail rather than guessing a package identity.

Adding another library therefore requires a project with an adjacent
`LibraryVersionFile.cs`; no release configuration entry or workflow tag prefix
is added manually.
