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
| `Mz.TextTemplate` | `Mz.TextTemplate` | `Mz.SemanticVersioning` `0.1.1` |
| `Mz.Toml` | `Mz.Toml` | `Mz.SemanticVersioning` `0.1.1` |

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
    release/Mz.TextTemplate/0.1.0
    release/Mz.Toml/0.2.0

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
- `Dependencies` declares every exact SELibs package dependency as a package ID and numeric version string;
- the single project beside the version file is the package root;
- projects without their own version file join a package when their root
  namespace matches that package and they reference one of its projects;
- portable release tests are discovered from test projects that directly
  reference the package's versioned root project.

The complete version-file namespace is the release identity. For example,
namespace `Mz.ApiProtocol` version `0.2.2` uses the exact tag
`release/Mz.ApiProtocol/0.2.2`. No separate slug is derived or configured.

Package dependencies are declared explicitly in `LibraryVersionFile.cs`. For
example:

    public static LibraryDependency[] Dependencies { get; } = {
        new LibraryDependency("Mz.SemanticVersioning", "0.1.1")
    };

A package without dependencies declares `new LibraryDependency[0]`. These
declarations are authoritative and are written unchanged to the released SELibs
package manifest.

Release validation independently discovers dependency usage from normal project
references and evaluated source files outside the package's owned source folders.
External source ownership and versions are resolved through `selibs.lock.json`.
Publishing fails when discovered usage is missing from `Dependencies`, when an
exact declared version differs from discovered usage, when a declaration is no
longer used, or when external source cannot be mapped to a package.

Only the releasing package's owned source folders are copied into its component
archive. Dependency source is never embedded.

Adding another library therefore requires a project with an adjacent
`LibraryVersionFile.cs`; no release configuration entry or workflow tag prefix
is added manually.
