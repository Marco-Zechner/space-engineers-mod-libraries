# Mz.TextTemplate language-definition guide

This guide builds a small host language containing:

- `name` and `message` value tags;
- a `tab` command with one required numeric positional argument;
- optional numeric `wrap`;
- a `first` block with no arguments.

The library parses and validates the language. The application remains
responsible for evaluating tags and rendering output.

## Define the language

```csharp
using Mz.TextTemplate;

// Value kinds describe how argument text was classified by the parser.
// They do not convert the source text into an application value.
var numberKinds = new[] { TemplateArgumentValueKind.Number };

// Positional definitions are matched in order.
// The first bool says whether this position is required.
var tabPositionals = new[]
{
    new TemplatePositionalArgumentDefinition(true, numberKinds)
};

// Named definitions are matched by exact name.
// Here "wrap" is optional and accepts only numeric source values.
var tabNamedArguments = new[]
{
    new TemplateNamedArgumentDefinition("wrap", false, numberKinds)
};

// A contract declares the complete argument shape accepted by a tag or block.
var tabArguments = new TemplateArgumentContract(tabPositionals, tabNamedArguments);

// Tag definitions declare ordinary {{name}} constructs known by the host.
// Value tags represent host-provided values; command tags represent host actions.
var tags = new[]
{
    new TemplateTagDefinition("name", TemplateTagRole.Value),
    new TemplateTagDefinition("message", TemplateTagRole.Value),
    new TemplateTagDefinition("tab", TemplateTagRole.Command, tabArguments)
};

// Block definitions declare structural {{#name}}...{{/name}} constructs.
var blocks = new[] { new TemplateBlockDefinition("first") };

// A language definition is the complete set of tags and blocks accepted by the host.
var language = new TemplateLanguageDefinition(tags, blocks);
```

The short tag and block constructors accept no arguments. Supplying arguments
to those constructs is therefore an analysis error.

A construct that accepts arguments must receive an explicit
`TemplateArgumentContract`.

## Parse and analyze

```csharp
const string source = "{{#first}}{{name}}: {{/first}}{{tab 4 wrap=1}}{{message}}";

// Parsing understands template syntax only and preserves exact source spans.
var parseResult = TemplateParser.Parse(source);

// Analysis applies the host language to the parsed document.
// It adds host-language diagnostics and semantic syntax classifications.
var analysis = TemplateLanguageAnalyzer.Analyze(parseResult, language);
```

`analysis.ParseResult` retains the original syntactic result.
`analysis.Document` exposes the parsed syntax tree, while
`analysis.Diagnostics` combines parser and host-language diagnostics in stable
source order.

```csharp
foreach (var diagnostic in analysis.Diagnostics)
{
    // Code identifies the rule and Severity indicates its importance.
    // Message is human-readable; Span identifies the exact source range.
}
```

`analysis.HasErrors` reports whether either parsing or language analysis
produced an error.

## Positional arguments

Positional definitions are matched in order:

```csharp
var numberKinds = new[] { TemplateArgumentValueKind.Number };

var coordinatePositions = new[]
{
    // First coordinate is required.
    new TemplatePositionalArgumentDefinition(true, numberKinds),

    // Second coordinate is optional.
    new TemplatePositionalArgumentDefinition(false, numberKinds)
};

// This contract accepts only those two positions and no named arguments.
var coordinates = new TemplateArgumentContract(coordinatePositions, new TemplateNamedArgumentDefinition[0]);
```

Required positional arguments must come before optional positional arguments in
the contract.

## Named arguments

Named arguments are matched by exact ordinal name:

```csharp
var numberKinds = new[] { TemplateArgumentValueKind.Number };
var booleanKinds = new[] { TemplateArgumentValueKind.Boolean };

var displayNamedArguments = new[]
{
    // "limit" is optional and accepts a numeric value.
    new TemplateNamedArgumentDefinition("limit", false, numberKinds),

    // "ellipsis" is optional and accepts a Boolean value.
    new TemplateNamedArgumentDefinition("ellipsis", false, booleanKinds)
};

// This contract accepts no positional arguments and the two named arguments above.
var displayArguments = new TemplateArgumentContract(new TemplatePositionalArgumentDefinition[0], displayNamedArguments);
```

The parser only determines lexical value kinds. For example, `12` is
`Number`, `true` is `Boolean`, `"text"` is `String`, and an ordinary unquoted
token is `Bare`.

Hosts remain responsible for converting accepted source text into application
values after successful analysis.

## Blocks

Blocks are ordinary host-defined constructs:

```text
{{#first}}
    {{name}}
{{/first}}
```

`Mz.TextTemplate` validates block syntax and nesting but does not assign
behavior to the name `first`.

Applications can therefore define unrelated block languages without changing
the parser.

## Work with the syntax tree

The root document contains `TemplateNode` values.

A node is one of:

- `TemplateTextNode`;
- `TemplateTagNode`;
- `TemplateBlockNode`.

Tags expose their name, name span, complete span, and parsed arguments.

Blocks expose their opening and closing spans, opening arguments, nested
children, and whether a matching closing tag was present.

Unclosed or otherwise malformed blocks are retained where possible so an editor
can still reason about incomplete input.

## Syntax highlighting

Use `analysis.SyntaxSpans` after language analysis when host-aware highlighting
is desired.

Known normal tag names are refined from the parser's generic `TagName`
classification to either:

- `TemplateSyntaxKind.ValueName`;
- `TemplateSyntaxKind.CommandName`.

Structural and argument classifications retain their exact source positions.

The language analyzer never evaluates a template. A renderer or compiler should
only consume analyzed syntax after applying whatever validity policy the host
requires.
