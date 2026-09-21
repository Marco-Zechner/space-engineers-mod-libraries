# Space Engineers Mod Libraries

Reusable source libraries for Space Engineers mod projects.

Packages are distributed through [SELibs](https://github.com/Marco-Zechner/selibs), which resolves exact dependencies and installs source folders.

> This README is generated from published stable GitHub releases. Do not edit it manually.

## Packages

| Package | Latest stable release | Documentation |
| --- | --- | --- |
| `Mz.ApiProtocol` | [`0.3.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.ApiProtocol/0.3.1) | [Guide](src/Mz.ApiProtocol.Core/README.md) |
| `Mz.Collections` | [`0.1.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Collections/0.1.0) | [Guide](src/Mz.Collections/README.md) |
| `Mz.Logging` | [`0.1.2`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Logging/0.1.2) | [Guide](src/Mz.Logging.Core/README.md) |
| `Mz.Networking` | [`0.2.2`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Networking/0.2.2) | [Guide](src/Mz.Networking.Core/README.md) |
| `Mz.SemanticVersioning` | [`0.2.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.SemanticVersioning/0.2.0) | [Guide](src/Mz.SemanticVersioning/README.md) |
| `Mz.Storage` | [`0.1.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Storage/0.1.0) | [Guide](src/Mz.Storage.Core/README.md) |
| `Mz.TextTemplate` | [`0.1.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.TextTemplate/0.1.1) | [Guide](src/Mz.TextTemplate/README.md) |
| `Mz.Toml` | [`0.2.2`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Toml/0.2.2) | [Guide](src/Mz.Toml/README.md) |

## Latest changes

### Mz.ApiProtocol

- Latest stable release: [`0.3.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.ApiProtocol/0.3.1)
- Replaced the duplicated internal read-only collection wrappers with Mz.Collections 0.1.0.
- Declared the exact Mz.Collections 0.1.0 package dependency.

### Mz.Collections

- Latest stable release: [`0.1.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Collections/0.1.0)
- Published reusable live read-only list and dictionary views.
- Added the exact Mz.SemanticVersioning 0.2.0 SELibs dependency.

### Mz.Logging

- Latest stable release: [`0.1.2`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Logging/0.1.2)
- Declared the exact Mz.SemanticVersioning 0.2.0 SELibs dependency.

### Mz.Networking

- Latest stable release: [`0.2.2`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Networking/0.2.2)
- Updated the exact Mz.ApiProtocol dependency to 0.3.1.
- Included the transitive Mz.Collections sources in Space Engineers source-copy validation.

### Mz.SemanticVersioning

- Latest stable release: [`0.2.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.SemanticVersioning/0.2.0)
- Added LibraryDependency for exact SELibs package dependency metadata.
- Added explicit package dependency declarations to LibraryVersionFile.

### Mz.Storage

- Latest stable release: [`0.1.0`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Storage/0.1.0)
- Published indexed logical-name storage with persisted discovery and stale-entry repair.
- Added local and world Space Engineers storage adapters using assembly-scoped ModAPI storage.
- Added safely namespaced global storage through a caller-owned physical filename prefix.
- Uses .index for Local and World indexes and .<owner>.index for Global indexes.
- Added the exact Mz.SemanticVersioning 0.2.0 SELibs dependency.

### Mz.TextTemplate

- Latest stable release: [`0.1.1`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.TextTemplate/0.1.1)
- Declared the exact Mz.SemanticVersioning 0.2.0 SELibs dependency.

### Mz.Toml

- Latest stable release: [`0.2.2`](https://github.com/Marco-Zechner/space-engineers-mod-libraries/releases/tag/release/Mz.Toml/0.2.2)
- Replaced the duplicated internal read-only list wrapper with Mz.Collections 0.1.0.
- Declared the exact Mz.Collections 0.1.0 package dependency.

## Release format

Each stable release publishes a SELibs package manifest and a checksum-verified source component archive.

See [SELibs package releases](docs/SELibs-Packages.md) for tag, asset, dependency, and package-discovery details.
