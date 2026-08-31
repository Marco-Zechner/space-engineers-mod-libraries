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

TemplateArgumentContract tabArguments =
    new TemplateArgumentContract(
        new[]
        {
            new TemplatePositionalArgumentDefinition(
                true,
                new[]
                {
                    TemplateArgumentValueKind.Number
                }
            )
        },
        new[]
        {
            new TemplateNamedArgumentDefinition(
                "wrap",
                false,
                new[]
                {
                    TemplateArgumentValueKind.Number
                }
            )
        }
    );

TemplateLanguageDefinition language =
    new TemplateLanguageDefinition(
        new[]
        {
            new TemplateTagDefinition(
                "name",
                TemplateTagRole.Value
            ),
            new TemplateTagDefinition(
                "message",
                TemplateTagRole.Value
            ),
            new TemplateTagDefinition(
                "tab",
                TemplateTagRole.Command,
                tabArguments
            )
        },
        new[]
        {
            new TemplateBlockDefinition(
                "first"
            )
        }
    );
```

The short tag and block constructors accept no arguments. Supplying arguments
to those constructs is therefore an analysis error.

A construct that accepts arguments must receive an explicit
`TemplateArgumentContract`.

## Parse and analyze

```csharp
string source =
    "{{#first}}{{name}}: {{/first}}"
    + "{{tab 4 wrap=1}}"
    + "{{message}}";

TemplateParseResult parseResult =
    TemplateParser.Parse(source);

TemplateLanguageAnalysisResult analysis =
    TemplateLanguageAnalyzer.Analyze(
        parseResult,
        language
    );
```

`analysis.ParseResult` retains the original syntactic result.
`analysis.Document` exposes the parsed syntax tree, while
`analysis.Diagnostics` combines parser and host-language diagnostics in stable
source order.

```csharp
TemplateDiagnostic[] diagnostics =
    analysis.Diagnostics;

for (int index = 0; index < diagnostics.Length; index++)
{
    TemplateDiagnostic diagnostic =
        diagnostics[index];

    // Use diagnostic.Code, diagnostic.Severity,
    // diagnostic.Message, and diagnostic.Span.
}
```

`analysis.HasErrors` reports whether either parsing or language analysis
produced an error.

## Positional arguments

Positional definitions are matched in order:

```csharp
TemplateArgumentContract coordinates =
    new TemplateArgumentContract(
        new[]
        {
            new TemplatePositionalArgumentDefinition(
                true,
                new[]
                {
                    TemplateArgumentValueKind.Number
                }
            ),
            new TemplatePositionalArgumentDefinition(
                false,
                new[]
                {
                    TemplateArgumentValueKind.Number
                }
            )
        },
        new TemplateNamedArgumentDefinition[0]
    );
```

Required positional arguments must come before optional positional arguments in
the contract.

## Named arguments

Named arguments are matched by exact ordinal name:

```csharp
TemplateArgumentContract displayArguments =
    new TemplateArgumentContract(
        new TemplatePositionalArgumentDefinition[0],
        new[]
        {
            new TemplateNamedArgumentDefinition(
                "limit",
                false,
                new[]
                {
                    TemplateArgumentValueKind.Number
                }
            ),
            new TemplateNamedArgumentDefinition(
                "ellipsis",
                false,
                new[]
                {
                    TemplateArgumentValueKind.Boolean
                }
            )
        }
    );
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