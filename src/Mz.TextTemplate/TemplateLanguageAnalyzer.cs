using System;
using System.Collections.Generic;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Applies a host-provided language definition to syntactically parsed
    /// template source without introducing host-specific behavior.
    /// </summary>
    public static class TemplateLanguageAnalyzer
    {
        /// <summary>
        /// Diagnostic code reported for a syntactically valid but unknown
        /// normal template tag.
        /// </summary>
        public const string UnknownTagDiagnosticCode = "MZTT2001";

        /// <summary>
        /// Diagnostic code reported for a syntactically valid but unknown
        /// template block.
        /// </summary>
        public const string UnknownBlockDiagnosticCode = "MZTT2002";

        /// <summary>
        /// Diagnostic code reported for a positional argument beyond the
        /// construct's declared positional argument count.
        /// </summary>
        public const string UnexpectedPositionalArgumentDiagnosticCode =
            "MZTT2003";

        /// <summary>
        /// Diagnostic code reported for an undeclared named argument.
        /// </summary>
        public const string UnknownNamedArgumentDiagnosticCode = "MZTT2004";

        /// <summary>
        /// Diagnostic code reported when a named argument is supplied more
        /// than once.
        /// </summary>
        public const string DuplicateNamedArgumentDiagnosticCode = "MZTT2005";

        /// <summary>
        /// Diagnostic code reported when a required positional argument is
        /// omitted.
        /// </summary>
        public const string MissingRequiredPositionalArgumentDiagnosticCode =
            "MZTT2006";

        /// <summary>
        /// Diagnostic code reported when a required named argument is omitted.
        /// </summary>
        public const string MissingRequiredNamedArgumentDiagnosticCode =
            "MZTT2007";

        /// <summary>
        /// Diagnostic code reported when an argument's lexical value kind is
        /// not accepted by its definition.
        /// </summary>
        public const string InvalidArgumentValueKindDiagnosticCode =
            "MZTT2008";

        /// <summary>
        /// Applies a host language definition to an existing parse result.
        /// Parser diagnostics are preserved and combined with language
        /// diagnostics in stable source order.
        /// </summary>
        public static TemplateLanguageAnalysisResult Analyze(
            TemplateParseResult parseResult,
            TemplateLanguageDefinition language
        )
        {
            if (parseResult == null)
                throw new ArgumentNullException("parseResult");

            if (language == null)
                throw new ArgumentNullException("language");

            TemplateDiagnostic[] parserDiagnostics =
                parseResult.Diagnostics;

            var diagnostics =
                new List<TemplateDiagnostic>(
                    parserDiagnostics.Length
                );

            for (
                int index = 0;
                index < parserDiagnostics.Length;
                index++
            )
            {
                diagnostics.Add(
                    parserDiagnostics[index]
                );
            }

            var classifications =
                new List<NameClassification>();

            AnalyzeNodes(
                parseResult.Document.Nodes,
                parserDiagnostics,
                language,
                diagnostics,
                classifications
            );

            SortDiagnosticsBySource(
                diagnostics
            );

            TemplateSyntaxSpan[] parserSyntax =
                parseResult.SyntaxSpans;

            var syntaxSpans =
                new List<TemplateSyntaxSpan>(
                    parserSyntax.Length
                );

            for (
                int index = 0;
                index < parserSyntax.Length;
                index++
            )
            {
                TemplateSyntaxSpan syntax =
                    parserSyntax[index];

                if (
                    syntax.Kind
                    == TemplateSyntaxKind.TagName
                )
                {
                    TemplateSyntaxKind semanticKind;

                    if (
                        TryGetClassification(
                            classifications,
                            syntax.Span,
                            out semanticKind
                        )
                    )
                    {
                        syntaxSpans.Add(
                            new TemplateSyntaxSpan(
                                semanticKind,
                                syntax.Span
                            )
                        );

                        continue;
                    }
                }

                syntaxSpans.Add(syntax);
            }

            return
                new TemplateLanguageAnalysisResult(
                    parseResult,
                    diagnostics,
                    syntaxSpans
                );
        }

        private sealed class NameClassification
        {
            public NameClassification(
                SourceSpan span,
                TemplateSyntaxKind kind
            )
            {
                Span = span;
                Kind = kind;
            }

            public SourceSpan Span;
            public TemplateSyntaxKind Kind;
        }

        private static void AnalyzeNodes(
            TemplateNode[] nodes,
            TemplateDiagnostic[] parserDiagnostics,
            TemplateLanguageDefinition language,
            IList<TemplateDiagnostic> diagnostics,
            IList<NameClassification> classifications
        )
        {
            for (
                int index = 0;
                index < nodes.Length;
                index++
            )
            {
                TemplateTagNode tag =
                    nodes[index] as TemplateTagNode;

                if (tag != null)
                {
                    AnalyzeTag(
                        tag,
                        parserDiagnostics,
                        language,
                        diagnostics,
                        classifications
                    );

                    continue;
                }

                TemplateBlockNode block =
                    nodes[index] as TemplateBlockNode;

                if (block == null)
                    continue;

                AnalyzeBlock(
                    block,
                    parserDiagnostics,
                    language,
                    diagnostics
                );

                AnalyzeNodes(
                    block.Children,
                    parserDiagnostics,
                    language,
                    diagnostics,
                    classifications
                );
            }
        }

        private static void AnalyzeTag(
            TemplateTagNode tag,
            TemplateDiagnostic[] parserDiagnostics,
            TemplateLanguageDefinition language,
            IList<TemplateDiagnostic> diagnostics,
            IList<NameClassification> classifications
        )
        {
            if (
                tag.Name.Length == 0
                || HasParserNameDiagnostic(
                    parserDiagnostics,
                    TemplateParser.InvalidTagNameDiagnosticCode,
                    tag.NameSpan
                )
            )
            {
                return;
            }

            TemplateTagDefinition definition;

            if (
                language.TryGetTag(
                    tag.Name,
                    out definition
                )
            )
            {
                classifications.Add(
                    new NameClassification(
                        tag.NameSpan,
                        definition.Role
                        == TemplateTagRole.Value
                            ? TemplateSyntaxKind.ValueName
                            : TemplateSyntaxKind.CommandName
                    )
                );

                AnalyzeArguments(
                    tag.Arguments,
                    tag.NameSpan,
                    definition.ArgumentContract,
                    parserDiagnostics,
                    diagnostics
                );

                return;
            }

            diagnostics.Add(
                new TemplateDiagnostic(
                    UnknownTagDiagnosticCode,
                    TemplateDiagnosticSeverity.Error,
                    "Unknown template tag '" + tag.Name + "'.",
                    tag.NameSpan
                )
            );
        }

        private static void AnalyzeBlock(
            TemplateBlockNode block,
            TemplateDiagnostic[] parserDiagnostics,
            TemplateLanguageDefinition language,
            IList<TemplateDiagnostic> diagnostics
        )
        {
            if (
                block.Name.Length == 0
                || HasParserNameDiagnostic(
                    parserDiagnostics,
                    TemplateParser.InvalidBlockNameDiagnosticCode,
                    block.OpenNameSpan
                )
            )
            {
                return;
            }

            TemplateBlockDefinition definition;

            if (
                language.TryGetBlock(
                    block.Name,
                    out definition
                )
            )
            {
                AnalyzeArguments(
                    block.Arguments,
                    block.OpenNameSpan,
                    definition.ArgumentContract,
                    parserDiagnostics,
                    diagnostics
                );

                return;
            }

            diagnostics.Add(
                new TemplateDiagnostic(
                    UnknownBlockDiagnosticCode,
                    TemplateDiagnosticSeverity.Error,
                    "Unknown template block '" + block.Name + "'.",
                    block.OpenNameSpan
                )
            );
        }

        private static void AnalyzeArguments(
            TemplateArgument[] arguments,
            SourceSpan constructNameSpan,
            TemplateArgumentContract contract,
            TemplateDiagnostic[] parserDiagnostics,
            IList<TemplateDiagnostic> diagnostics
        )
        {
            int positionalIndex = 0;

            var seenNamedArguments =
                new Dictionary<string, bool>(
                    StringComparer.Ordinal
                );

            for (
                int index = 0;
                index < arguments.Length;
                index++
            )
            {
                TemplatePositionalArgument positional =
                    arguments[index]
                    as TemplatePositionalArgument;

                if (positional != null)
                {
                    if (
                        positionalIndex
                        >= contract.PositionalArgumentCount
                    )
                    {
                        diagnostics.Add(
                            new TemplateDiagnostic(
                                UnexpectedPositionalArgumentDiagnosticCode,
                                TemplateDiagnosticSeverity.Error,
                                "Unexpected positional argument.",
                                positional.Span
                            )
                        );
                    }
                    else
                    {
                        TemplatePositionalArgumentDefinition definition =
                            contract.GetPositionalArgument(
                                positionalIndex
                            );

                        if (
                            positional.Value.Kind
                            != TemplateArgumentValueKind.Missing
                            && !definition.Allows(
                                positional.Value.Kind
                            )
                        )
                        {
                            diagnostics.Add(
                                new TemplateDiagnostic(
                                    InvalidArgumentValueKindDiagnosticCode,
                                    TemplateDiagnosticSeverity.Error,
                                    "Positional argument uses an unsupported value kind.",
                                    positional.Value.Span
                                )
                            );
                        }
                    }

                    positionalIndex++;
                    continue;
                }

                TemplateNamedArgument named =
                    arguments[index]
                    as TemplateNamedArgument;

                if (named == null)
                    continue;

                if (
                    named.Name.Length == 0
                    || HasParserNameDiagnostic(
                        parserDiagnostics,
                        TemplateParser.InvalidArgumentNameDiagnosticCode,
                        named.NameSpan
                    )
                )
                {
                    continue;
                }

                TemplateNamedArgumentDefinition namedDefinition;

                if (
                    !contract.TryGetNamedArgument(
                        named.Name,
                        out namedDefinition
                    )
                )
                {
                    diagnostics.Add(
                        new TemplateDiagnostic(
                            UnknownNamedArgumentDiagnosticCode,
                            TemplateDiagnosticSeverity.Error,
                            "Unknown named argument '" + named.Name + "'.",
                            named.NameSpan
                        )
                    );

                    continue;
                }

                if (
                    seenNamedArguments.ContainsKey(
                        named.Name
                    )
                )
                {
                    diagnostics.Add(
                        new TemplateDiagnostic(
                            DuplicateNamedArgumentDiagnosticCode,
                            TemplateDiagnosticSeverity.Error,
                            "Named argument '" + named.Name + "' is supplied more than once.",
                            named.NameSpan
                        )
                    );

                    continue;
                }

                seenNamedArguments.Add(
                    named.Name,
                    true
                );

                if (
                    named.Value.Kind
                    != TemplateArgumentValueKind.Missing
                    && !namedDefinition.Allows(
                        named.Value.Kind
                    )
                )
                {
                    diagnostics.Add(
                        new TemplateDiagnostic(
                            InvalidArgumentValueKindDiagnosticCode,
                            TemplateDiagnosticSeverity.Error,
                            "Named argument '" + named.Name + "' uses an unsupported value kind.",
                            named.Value.Span
                        )
                    );
                }
            }

            for (
                int index = positionalIndex;
                index < contract.PositionalArgumentCount;
                index++
            )
            {
                TemplatePositionalArgumentDefinition definition =
                    contract.GetPositionalArgument(
                        index
                    );

                if (!definition.Required)
                    continue;

                diagnostics.Add(
                    new TemplateDiagnostic(
                        MissingRequiredPositionalArgumentDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Required positional argument "
                        + (index + 1).ToString()
                        + " is missing.",
                        constructNameSpan
                    )
                );
            }

            for (
                int index = 0;
                index < contract.NamedArgumentCount;
                index++
            )
            {
                TemplateNamedArgumentDefinition definition =
                    contract.GetNamedArgument(
                        index
                    );

                if (
                    !definition.Required
                    || seenNamedArguments.ContainsKey(
                        definition.Name
                    )
                )
                {
                    continue;
                }

                diagnostics.Add(
                    new TemplateDiagnostic(
                        MissingRequiredNamedArgumentDiagnosticCode,
                        TemplateDiagnosticSeverity.Error,
                        "Required named argument '" + definition.Name + "' is missing.",
                        constructNameSpan
                    )
                );
            }
        }

        private static bool HasParserNameDiagnostic(
            TemplateDiagnostic[] diagnostics,
            string code,
            SourceSpan nameSpan
        )
        {
            for (
                int index = 0;
                index < diagnostics.Length;
                index++
            )
            {
                TemplateDiagnostic diagnostic =
                    diagnostics[index];

                if (
                    diagnostic.Code == code
                    && diagnostic.Span.Start >= nameSpan.Start
                    && diagnostic.Span.Start < nameSpan.End
                )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetClassification(
            IList<NameClassification> classifications,
            SourceSpan span,
            out TemplateSyntaxKind kind
        )
        {
            for (
                int index = 0;
                index < classifications.Count;
                index++
            )
            {
                NameClassification classification =
                    classifications[index];

                if (
                    classification.Span
                    == span
                )
                {
                    kind = classification.Kind;
                    return true;
                }
            }

            kind = TemplateSyntaxKind.TagName;
            return false;
        }

        private static void SortDiagnosticsBySource(
            IList<TemplateDiagnostic> diagnostics
        )
        {
            for (
                int index = 1;
                index < diagnostics.Count;
                index++
            )
            {
                TemplateDiagnostic current =
                    diagnostics[index];

                int destination =
                    index - 1;

                while (
                    destination >= 0
                    && diagnostics[destination].Span.Start
                    > current.Span.Start
                )
                {
                    diagnostics[destination + 1] =
                        diagnostics[destination];

                    destination--;
                }

                diagnostics[destination + 1] =
                    current;
            }
        }
    }
}