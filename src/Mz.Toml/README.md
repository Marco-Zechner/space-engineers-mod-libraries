# Mz.Toml

`Mz.Toml` is a strict TOML 1.0 parser, document model, and deterministic
writer designed for source-copy use in Space Engineers mods and ordinary
.NET projects.

The package has an exact dependency on `Mz.SemanticVersioning` `0.1.1`,
matching the shared version and changelog model used by the other libraries in
this repository.

The library supports:

- TOML 1.0 keys, tables, dotted keys, and arrays of tables;
- basic, literal, multiline basic, and multiline literal strings;
- integers, floating-point values, Booleans, arrays, and inline tables;
- offset date-times, local date-times, local dates, and local times;
- strict UTF-8 byte parsing with BOM handling;
- deterministic canonical writing;
- line and column diagnostics for parse failures.

The writer intentionally produces canonical TOML rather than preserving the
input's comments, whitespace, or original layout.

## Install

### Install with SELibs

[SELibs](https://github.com/Marco-Zechner/selibs) is a source-library manager
for Space Engineers mods.

From the root of a mod project:

```shell
selibs init
selibs add Mz.Toml@0.1.0
```

Skip `selibs init` when the project already contains `selibs.json`.

SELibs installs `Mz.Toml` and its exact `Mz.SemanticVersioning` dependency.
It records the installed versions and managed file checksums.

Inspect installed state with:

```shell
selibs status
```

### Install manually

Use source from the matching release tags and copy both complete folders:

```text
src/Mz.SemanticVersioning
src/Mz.Toml
```

Place them as sibling folders under the mod's script library directory:

```text
Data/Scripts/ExampleMod/Libraries/Mz.SemanticVersioning
Data/Scripts/ExampleMod/Libraries/Mz.Toml
```

For `Mz.Toml` `0.1.0`, use `Mz.SemanticVersioning` `0.1.1`.

Keep both folder structures intact and compile every contained `.cs` file as
part of the mod. Do not combine source files from different release versions.

## Parse TOML

Use `Toml.Parse` when invalid TOML should throw:

```csharp
using Mz.Toml;

TomlDocument document = Toml.Parse(
    "title = \"Example\"\n" +
    "enabled = true\n"
);

string title =
    ((TomlValue)document.Root["title"]).AsString();

bool enabled =
    ((TomlValue)document.Root["enabled"]).AsBoolean();
```

Use `Toml.TryParse` when diagnostics should be handled without an exception:

```csharp
TomlParseResult result = Toml.TryParse(text);

if (!result.IsSuccess)
{
    TomlDiagnostic diagnostic = result.Diagnostics[0];

    Log(
        diagnostic.Line
        + ":"
        + diagnostic.Column
        + " "
        + diagnostic.Message
    );

    return;
}

TomlDocument document = result.Document;
```

Diagnostics include a stable diagnostic code plus one-based line and column
information.

## Parse strict UTF-8 bytes

The byte API validates the original UTF-8 encoding before parsing TOML:

```csharp
byte[] bytes = LoadTomlBytes();

TomlParseResult result = Toml.TryParse(bytes);

if (!result.IsSuccess)
{
    HandleTomlFailure(result.Diagnostics[0]);
    return;
}

TomlDocument document = result.Document;
```

Malformed UTF-8 produces `TomlDiagnosticCode.InvalidEncoding`.

One UTF-8 BOM is accepted only at the beginning of the input. The string API
is intentionally an already-decoded Unicode API and therefore cannot detect
decoding errors that occurred before the string reached `Mz.Toml`.

## Build a document programmatically

Create nodes directly when TOML is being generated rather than parsed:

```csharp
var document = new TomlDocument();

document.Root.Set(
    "title",
    TomlValue.FromString("Example")
);

document.Root.Set(
    "enabled",
    TomlValue.FromBoolean(true)
);

var ports = new TomlArray();

ports.Add(TomlValue.FromInteger(8000));
ports.Add(TomlValue.FromInteger(8001));

document.Root.Set("ports", ports);

string text = Toml.Write(document);
```

Tables preserve insertion order. Arrays may contain heterogeneous TOML value
kinds, as permitted by TOML 1.0.

## Read values

Every table entry is a `TomlNode`.

Inspect `TomlNode.Kind` or cast a node to its concrete type:

```csharp
TomlNode node = document.Root["title"];

if (node.Kind == TomlNodeKind.Value)
{
    TomlValue value = (TomlValue)node;

    if (value.ValueKind == TomlValueKind.String)
    {
        string title = value.AsString();
    }
}
```

`TomlValue` supplies typed accessors for strings, integers, floats, Booleans,
offset date-times, local date-times, local dates, and local times.

Calling an accessor for the wrong `TomlValueKind` throws
`InvalidOperationException`.

## Write canonical TOML

`Toml.Write` produces deterministic TOML:

```csharp
string canonicalToml = Toml.Write(document);
```

The output is intended for configuration persistence and generated documents.
It does not preserve comments, whitespace choices, quoting choices, or other
presentation details from parsed input.

Cyclic programmatic node graphs are rejected. Reusing the same node in multiple
acyclic locations is supported.

## TOML behavior

`Mz.Toml` targets TOML 1.0.

Notable format rules include:

- TOML has no `null` value;
- arrays may contain heterogeneous values;
- inline tables and arrays are represented through the same public node model;
- arrays of tables are exposed as arrays containing tables;
- temporal values use dedicated `Mz.Toml` value types so TOML edge cases are
  not lost through `System.DateTime`.

During development, the library was checked against the complete pinned TOML
1.0 `toml-test` fixture manifest:

- 210 valid fixtures accepted;
- 501 invalid fixtures rejected.

## Space Engineers compatibility

Production source targets C# 6 and is permanently source-copy validated against
the Space Engineers mod analyzer environment.

Neither `Mz.Toml` nor `Mz.SemanticVersioning` depends on Space Engineers APIs.
Both can therefore be used by ordinary .NET code and copied directly into a
mod's script source tree.