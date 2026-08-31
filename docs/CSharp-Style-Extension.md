# C# Compact Layout Extension

This file supplements the project's general C# engineering and code-style
guidelines.

For layout decisions in this repository, this document is more specific and
takes precedence where the general guideline leaves room for interpretation.

## Core rule: prefer horizontal code

Compact horizontal code is the default.

A line break must improve readability or expose meaningful structure. Do not
introduce vertical layout merely because a construct has multiple arguments,
contains a nested call, or could technically be spread over several lines.

Moderately long but straightforward lines are preferred over mechanically
expanded code.

As a practical line-length target, keep normal code at roughly 150 columns or
fewer. Lines from 151 through 160 columns are acceptable when they remain
clearer than the wrapped alternative. Treat 160 columns as the practical upper
limit rather than allowing horizontal layout to grow without bound.

## Line length and wrap level

Compact does not mean forcing every expression onto one line.

When a line needs to wrap, break at the highest available syntactic level. For
a call or constructor, this normally means breaking between outer arguments
before breaking inside one of those arguments.

Prefer:

```csharp
Call(arg1, arg2,
    new Args(arg3, arg4));
```

over breaking the nested constructor first:

```csharp
Call(arg1, arg2, new Args(arg3,
    arg4));
```

Likewise, prefer keeping a nested call intact:

```csharp
return new TemplateArgumentValue(kind, source.Substring(start, length),
    source.Substring(contentStart, contentLength), span, contentSpan);
```

rather than breaking inside `Substring(...)` while an outer argument boundary
is available.

When several equivalent break points exist, prefer a tapered layout where the
upper line is longer than the continuation line. This is a readability
preference rather than a reason to break semantic grouping.

The intended progression is therefore:

1. Keep the complete expression horizontal while it remains comfortably
   readable.
2. At roughly 150 columns, consider a high-level wrap.
3. Between 151 and 160 columns, keep the line if wrapping would be worse.
4. Above roughly 160 columns, wrap at the highest useful argument or expression
   boundary.
5. Only break inside a nested expression when its containing expression cannot
   be wrapped cleanly at a higher level.

## Assignments stay with their expression

Do not normally break immediately after `=`.

Prefer:

```csharp
var result = TemplateParser.Parse(source);
var diagnostic = Assert.Single(result.Diagnostics);
const string source = "{{tab 4 wrap=1}}";
var tag = Assert.IsType<TemplateTagNode>(Assert.Single(result.Document.Nodes));
```

Avoid:

```csharp
var result =
    TemplateParser.Parse(source);

var diagnostic =
    Assert.Single(result.Diagnostics);

const string source =
    "{{tab 4 wrap=1}}";
```

This applies equally to locals, fields, properties, and assignment statements.

A break after `=` is acceptable when the right-hand side is itself deliberately
multiline and the break exposes real structure, for example a multiline
collection expression:

```csharp
TemplateArgumentValueKind[] kinds =
[
    TemplateArgumentValueKind.Number,
    TemplateArgumentValueKind.String
];
```

Do not use that exception for an ordinary constructor, property access, method
call, constant, or other simple expression.

## Calls and constructors stay horizontal when reasonable

Keep an invocation or constructor on one line when its arguments are reasonably
readable there.

Prefer:

```csharp
Assert.Equal("[4..7)", span.ToString());
var positional = new TemplatePositionalArgumentDefinition(true, kinds);
var contract = new TemplateArgumentContract([], []);
TemplateLanguageAnalyzer.Analyze(parse, language);
```

Avoid mechanically expanding the same calls:

```csharp
Assert.Equal(
    "[4..7)",
    span.ToString()
);

var positional =
    new TemplatePositionalArgumentDefinition(
        true,
        kinds
    );
```

Nested calls are not a reason by themselves to expand vertically.

Prefer:

```csharp
var block = Assert.IsType<TemplateBlockNode>(Assert.Single(result.Document.Nodes));
```

over expanding every nested invocation onto another level.

## Multiline calls must earn the extra lines

Use multiline argument layout when the arguments themselves contain meaningful
multiline structure or the complete invocation becomes genuinely difficult to
scan.

For example:

```csharp
var language = new TemplateLanguageDefinition(
    [
        new TemplateTagDefinition("name", TemplateTagRole.Value),
        new TemplateTagDefinition("tab", TemplateTagRole.Command)
    ],
    []
);
```

Likewise, keeping a simple nested expression intact is preferable even when the
outer call must wrap:

```csharp
var result = TemplateLanguageAnalyzer.Analyze(
    TemplateParser.Parse("{{#first}}{{name}}{{/first}}"),
    language
);
```

Do not expand the inner `TemplateParser.Parse(...)` merely because the outer
call is multiline.

## Method and constructor declarations

Keep parameter lists on one line whenever that remains reasonably readable.

Prefer:

```csharp
public void Parse_InvalidNamedArgumentName_ReportsOffendingCharacter(string name, int expectedOffset)
```

over:

```csharp
public void Parse_InvalidNamedArgumentName_ReportsOffendingCharacter(
    string name,
    int expectedOffset
)
```

Use a multiline declaration only when the signature is genuinely difficult to
read horizontally.

Do not use one-parameter-per-line formatting as a default.

The same rule applies to constructors and private/internal helpers.

## Assertions

Simple assertions should normally be one line.

Prefer:

```csharp
Assert.Equal(4, span.Start);
Assert.Equal(parameter, exception.ParamName);
Assert.True(contract.PositionalArguments[0].Required);
Assert.Contains(syntax, item => item.Kind == TemplateSyntaxKind.NumberValue);
```

A multiline assertion is appropriate when one or more arguments are themselves
substantial expressions and the split exposes useful structure.

Do not make every `Assert.Equal`, `Assert.True`, `Assert.IsType`, or
`Assert.Throws` vertical by default.

## Lambdas

Keep short lambdas inline.

Prefer:

```csharp
var exception = Assert.Throws<ArgumentException>(() => new SourceSpan(start, length));
Assert.Contains(syntax, item => item.Kind == TemplateSyntaxKind.StringValue);
```

Avoid moving a short lambda body onto additional lines solely because it is
nested inside another call.

Multiline lambdas remain appropriate when the lambda contains multiple
statements or genuinely complex logic.

## Conditions and loop headers

Keep short conditions and loop headers horizontal.

Prefer:

```csharp
for (var index = 1; index < syntax.Length; index++)
```

and:

```csharp
if (t.Kind == TemplateSyntaxKind.ValueName)
    valueFound = true;
```

Avoid splitting each operand or each `for` clause onto its own line unless the
condition is genuinely complex.

Prefer `foreach` when an index exists only to retrieve the current element, but
do not rewrite unrelated control flow merely to reduce line count.

## Object construction and collection syntax

Use the shortest clear representation supported by the project's language
version.

The test project uses modern C# and may prefer collection expressions:

```csharp
var contract = new TemplateArgumentContract([], []);
var tags = [new TemplateTagDefinition("name", TemplateTagRole.Value)];
```

The reusable `Mz.TextTemplate` source targets C# 6. Do not copy C# 12-only
syntax such as collection expressions into `src/Mz.TextTemplate`.

Compact layout still applies there using C# 6-compatible syntax.

## `var`

Prefer `var` when the right-hand side already makes the type obvious:

```csharp
var result = TemplateParser.Parse(source);
var valueFound = false;
```

Use an explicit type when it communicates useful information that is not
obvious from the initializer or when the syntax requires it.

Do not change types mechanically just for brevity.

## Constructor initializers, returns, and simple expressions

Do not vertically expand trivial constructor initializers, return expressions,
ternaries, or property accesses.

Prefer:

```csharp
: base(TemplateNodeKind.Text, span)
```

and:

```csharp
return new SourceSpan(start, length);
```

when they are readable that way.

As elsewhere, use multiline layout when the expression itself contains
meaningful complex structure.

## Blank lines

Use blank lines to separate logical steps, not every individual statement.

Several closely related setup statements may remain together. A blank line
should indicate a real conceptual boundary in the method.

Do not compress unrelated operations together solely to minimize line count.

## Formatting must not change behavior

A style pass should remain a style pass.

Do not rewrite algorithms, recovery behavior, data structures, exception
semantics, ordering, or public API merely to make code shorter.

Small control-flow simplifications are appropriate only when they are obviously
equivalent and are part of the currently edited slice.

## Practical wrapping test

Before introducing a line break, ask:

1. Is the horizontal form already easy to understand?
2. Does the break expose meaningful structure?
3. Is the break necessary because an argument or expression is itself
   multiline?
4. Would the vertical form merely put one argument, token, or nested call on
   each line?

If the horizontal form is readable and the extra lines do not expose meaningful
structure, keep it horizontal.

In particular, treat these patterns as warning signs during review:

```text
=
    simpleExpression

Method(
    simpleArg1,
    simpleArg2
)

new Type(
    simpleArg1,
    simpleArg2
)

return
    simpleExpression
```

They are not absolutely forbidden, but they should be unusual and justified by
the surrounding expression rather than used as a default formatting style.
