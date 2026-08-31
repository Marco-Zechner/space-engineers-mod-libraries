# Mz.TextTemplate

`Mz.TextTemplate` provides a small host-defined text-template language with
recoverable parsing and editor-friendly source information.

The library is independent of Space Engineers, RichHudFramework, and any
specific template host. Applications define the names and argument contracts
that are meaningful to them.

Features include:

- literal text and `{{tag}}` syntax;
- positional and named arguments;
- bare, string, number, and boolean lexical value kinds;
- nested `{{#block}}...{{/block}}` structures;
- exact source spans for syntax-tree nodes and editor classifications;
- recoverable parser diagnostics for malformed partial input;
- host-defined value tags, command tags, and blocks;
- strict positional and named argument contracts;
- semantic diagnostics for unknown constructs and invalid argument usage.

`Mz.TextTemplate` has an exact package dependency on
`Mz.SemanticVersioning` `0.1.1`.

For a complete language-definition and analysis example, see
[Guide.md](Guide.md).

## Install

### Install with SELibs

[SELibs](https://github.com/Marco-Zechner/selibs) is a source-library manager
for Space Engineers mods.

From the mod project root:

```shell
selibs init
selibs add Mz.TextTemplate@0.1.0
```

Skip `selibs init` when the project already contains `selibs.json`.

SELibs installs `Mz.TextTemplate` and its exact `Mz.SemanticVersioning`
dependency and records their versions and file checksums.

Inspect the managed state with:

```shell
selibs status
```

### Install manually

Use source from the matching release tags and copy these complete folders:

```text
src/Mz.SemanticVersioning
src/Mz.TextTemplate
```

Place them as sibling folders beneath the mod's script-library directory:

```text
Data/Scripts/ExampleMod/Libraries/Mz.SemanticVersioning
Data/Scripts/ExampleMod/Libraries/Mz.TextTemplate
```

Compile all contained `.cs` files as part of the mod.

`Mz.TextTemplate` `0.1.0` requires `Mz.SemanticVersioning` `0.1.1`.

## Template syntax

A normal tag contains a host-defined name:

```text
{{name}}
```

Tags can have positional and named arguments:

```text
{{tab 4 wrap=1}}
{{time format="HH:mm:ss"}}
```

Blocks contain nested template content:

```text
{{#first}}{{name}}: {{/first}}{{message}}
```

The parser does not assign application meaning to names such as `name`,
`tab`, or `first`. That meaning belongs to the host language definition.

## Parse source

```csharp
using Mz.TextTemplate;

TemplateParseResult parseResult =
    TemplateParser.Parse(
        "{{#first}}{{name}}: {{/first}}{{message}}"
    );

TemplateDiagnostic[] diagnostics =
    parseResult.Diagnostics;

TemplateNode[] nodes =
    parseResult.Document.Nodes;
```

Malformed source still produces a recoverable document, diagnostics, and exact
source spans. This is useful for live editors where incomplete input should
remain inspectable.

## Apply host-language rules

Parsing answers what the source structurally contains. Language analysis answers
whether those constructs are known and valid for one application.

Define normal tags as either values or commands, define blocks, and attach
argument contracts when arguments are accepted. Then pass the parse result to
`TemplateLanguageAnalyzer`.

See [Guide.md](Guide.md) for a complete example.

## Editor integration

Both parser and language analysis expose `TemplateSyntaxSpan` values with exact
`SourceSpan` locations.

Syntax kinds distinguish literal text, delimiters, tag and block structure,
argument names and values, and host-classified value or command names.

Diagnostics also contain exact source spans, stable diagnostic codes, severity,
and messages. Hosts can therefore underline errors, provide hover information,
and continue highlighting malformed partial templates without reparsing source
positions themselves.

## Package version

The package version represented by the source is available through:

```csharp
string packageVersion =
    Mz.TextTemplate.LibraryVersionFile.VersionString;
```