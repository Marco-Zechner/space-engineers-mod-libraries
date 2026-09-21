# Mz.Storage

`Mz.Storage` provides indexed text-file storage for Space Engineers mods.
It maintains a persisted logical-name index because the ModAPI can probe known
filenames but cannot enumerate all files in a storage directory.

The package contains:

- `Mz.Storage.Core` - portable indexed storage behavior over `IStorageBackend`.
- `Mz.Storage.SpaceEngineers` - Local, World, and safely namespaced Global adapters.

## Install

### Install with SELibs

After installing SELibs, run these commands from the root of the mod project:

    selibs init
    selibs add Mz.Storage@0.1.0

Skip `selibs init` when the project already contains `selibs.json`.

### Install manually

Use the source from the matching release tag and copy these folders:

    src/Mz.SemanticVersioning
    src/Mz.Storage.Core
    src/Mz.Storage.SpaceEngineers

as sibling folders under the mod's script library directory. Compile all
contained `.cs` files as part of the mod and keep package versions aligned with
the exact dependencies declared by `LibraryVersionFile`.

## Create Space Engineers storage

Create storage after `MyAPIGateway.Utilities` becomes available in the mod lifecycle:

    using Mz.Storage;
    using Mz.Storage.SpaceEngineers;

    IndexedStorage local = SpaceEngineersStorage.CreateLocal(typeof(MySession));
    IndexedStorage world = SpaceEngineersStorage.CreateWorld(typeof(MySession));
    IndexedStorage global = SpaceEngineersStorage.CreateGlobal("MyMod");

Local storage is scoped by the calling type's assembly. World storage uses the
same assembly scope inside the active save. Global storage is shared by all mods,
so `CreateGlobal` requires a stable owner prefix and applies it to every physical
data filename and to the private index filename.

For example, owner prefix `MyMod` and logical name `config.toml` use a physical
global filename beginning with `MyMod.` while callers continue to work only with
the logical name `config.toml`.

## Save, load, probe, and list

    world.Save("server.toml", "enabled = true");

    bool exists = world.Exists("server.toml");
    string text = world.Load("server.toml");
    string[] known = world.ListKnown();

`Save` writes the file and records its logical name in the persisted index.
`Load` throws `FileNotFoundException` when the physical file is missing. A
successful load of a deterministic filename that was not already indexed adds
that logical name to the index, which supports discovery of pre-existing files.

`Exists` probes the deterministic physical filename without changing the index.
`ListKnown` returns logical names in ordinal order and removes stale entries whose
physical files disappeared. Duplicate, blank, and reserved index entries are also
repaired when the index is read.

## Logical names and the private index

Logical names must be nonblank, cannot contain line breaks, and cannot equal
`IndexedStorage.IndexFileName`. The reserved index filename is an implementation
detail and is never returned by `ListKnown`.

Local and World storage expose one indexed logical namespace per assembly storage
scope. Global storage isolates that namespace by the owner prefix supplied to
`CreateGlobal`.

## Use the portable core

Implement `IStorageBackend` when storage comes from another deterministic text-file
transport:

    public interface IStorageBackend
    {
        bool Exists(string fileName);
        string Read(string fileName);
        void Write(string fileName, string content);
    }

Then construct `IndexedStorage` directly. The optional physical prefix constructor
can namespace all data files and the private index while preserving logical names.

## Package version

The released package version is available through:

    string packageVersion = Mz.Storage.LibraryVersionFile.VersionString;
