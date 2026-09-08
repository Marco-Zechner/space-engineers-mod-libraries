# Space Engineers Mod Libraries

Reusable source libraries for Space Engineers mod projects.

Packages are distributed through [SELibs](https://github.com/Marco-Zechner/selibs), which resolves exact dependencies and installs source folders.

> This README is generated from published stable GitHub releases. Do not edit it manually.

## Packages

| Package | Latest stable release | Documentation |
| --- | --- | --- |
| `Mz.ApiProtocol` | [`0.2.5`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.ApiProtocol/0.2.5) | [Guide](src/Mz.ApiProtocol.Core/README.md) |
| `Mz.Logging` | [`0.1.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Logging/0.1.1) | [Guide](src/Mz.Logging.Core/README.md) |
| `Mz.Networking` | [`0.2.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Networking/0.2.0) | [Guide](src/Mz.Networking.Core/README.md) |
| `Mz.SemanticVersioning` | [`0.2.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.SemanticVersioning/0.2.0) | [Guide](src/Mz.SemanticVersioning/README.md) |
| `Mz.TextTemplate` | [`0.1.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.TextTemplate/0.1.0) | [Guide](src/Mz.TextTemplate/README.md) |
| `Mz.Toml` | [`0.2.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Toml/0.2.0) | [Guide](src/Mz.Toml/README.md) |

## Latest changes

### Mz.ApiProtocol

- Latest stable release: [`0.2.5`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.ApiProtocol/0.2.5)
- Published the guide example style improvements after correcting release validation.

### Mz.Logging

- Latest stable release: [`0.1.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Logging/0.1.1)
- Added a complete package usage guide and installation examples.
- Added the shared Mz.SemanticVersioning dependency.
- Adopted the shared SemanticVersioning changelog model.

### Mz.Networking

- Latest stable release: [`0.2.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Networking/0.2.0)
- Added reliable and unreliable delivery selection with Space Engineers size validation.
- Added wrap-aware application sequence comparison and tracking helpers.
- Added versioned network identity and bounded structured receive diagnostics.
- Added optional NetworkManager channel assignment, generation-checked reassignment, and active conflict reporting.
- Added fallback and forced-channel managed-session modes while preserving legacy fixed-channel compatibility.
- Added the exact Mz.ApiProtocol dependency and expanded source-copy validation.

### Mz.SemanticVersioning

- Latest stable release: [`0.2.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.SemanticVersioning/0.2.0)
- Added LibraryDependency for exact SELibs package dependency metadata.
- Added explicit package dependency declarations to LibraryVersionFile.

### Mz.TextTemplate

- Latest stable release: [`0.1.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.TextTemplate/0.1.0)
- Published the initial SELibs package release.
- Added recoverable template parsing with exact source spans, arguments, and nested blocks.
- Added host-defined language analysis, semantic syntax classifications, and strict argument contracts.
- Added package documentation and Space Engineers source-copy validation.

### Mz.Toml

- Latest stable release: [`0.2.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Toml/0.2.0)
- Added exact source-preserving syntax spans, comments, trivia, and objective trivia placement.
- Added the custom '#!' disabled-assignment extension while keeping disabled values out of the semantic document.
- Added source-preserving enable, disable, value-replacement, and validated source-insertion operations.
- Added recoverable syntax information for invalid decoded source, including explicit unparsed ranges.
- Added the composable TomlSourceEditor with refreshed syntax, stale-node rejection, and atomic validated edits.
- Kept strict TOML 1.0 semantic parsing, deterministic canonical writing, and Space Engineers C# 6 source-copy compatibility.

## Release format

Each stable release publishes a SELibs package manifest and a checksum-verified source component archive.

See [SELibs package releases](docs/SELibs-Packages.md) for tag, asset, dependency, and package-discovery details.
