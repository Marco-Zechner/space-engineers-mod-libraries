# Space Engineers Mod Libraries

Reusable source libraries for Space Engineers mod projects.

Packages are distributed through [SELibs](https://github.com/Marco-Zechner/selibs), which resolves exact dependencies and installs source folders.

> This README is generated from published stable GitHub releases. Do not edit it manually.

## Packages

| Package | Latest stable release | Documentation |
| --- | --- | --- |
| `Mz.ApiProtocol` | [`0.3.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.ApiProtocol/0.3.0) | [Guide](src/Mz.ApiProtocol.Core/README.md) |
| `Mz.Logging` | [`0.1.2`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Logging/0.1.2) | [Guide](src/Mz.Logging.Core/README.md) |
| `Mz.Networking` | [`0.2.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Networking/0.2.1) | [Guide](src/Mz.Networking.Core/README.md) |
| `Mz.SemanticVersioning` | [`0.2.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.SemanticVersioning/0.2.0) | [Guide](src/Mz.SemanticVersioning/README.md) |
| `Mz.TextTemplate` | [`0.1.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.TextTemplate/0.1.1) | [Guide](src/Mz.TextTemplate/README.md) |
| `Mz.Toml` | [`0.2.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Toml/0.2.1) | [Guide](src/Mz.Toml/README.md) |

## Latest changes

### Mz.ApiProtocol

- Latest stable release: [`0.3.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.ApiProtocol/0.3.0)
- Added a params ApiEndpointContract constructor for concise endpoint declarations.
- Declared the exact Mz.SemanticVersioning 0.2.0 SELibs dependency.

### Mz.Logging

- Latest stable release: [`0.1.2`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Logging/0.1.2)
- Declared the exact Mz.SemanticVersioning 0.2.0 SELibs dependency.

### Mz.Networking

- Latest stable release: [`0.2.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Networking/0.2.1)
- Improved internal variable declarations and formatting.
- Improved receive classification for packets belonging to another network.
- Declared exact dependencies on Mz.ApiProtocol 0.3.0 and Mz.SemanticVersioning 0.2.0.

### Mz.SemanticVersioning

- Latest stable release: [`0.2.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.SemanticVersioning/0.2.0)
- Added LibraryDependency for exact SELibs package dependency metadata.
- Added explicit package dependency declarations to LibraryVersionFile.

### Mz.TextTemplate

- Latest stable release: [`0.1.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.TextTemplate/0.1.1)
- Declared the exact Mz.SemanticVersioning 0.2.0 SELibs dependency.

### Mz.Toml

- Latest stable release: [`0.2.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Toml/0.2.1)
- Applied internal naming and code-style cleanup.
- Declared the exact Mz.SemanticVersioning 0.2.0 SELibs dependency.

## Release format

Each stable release publishes a SELibs package manifest and a checksum-verified source component archive.

See [SELibs package releases](docs/SELibs-Packages.md) for tag, asset, dependency, and package-discovery details.
