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

            if (
                language.ContainsBlock(
                    block.Name
                )
            )
            {
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