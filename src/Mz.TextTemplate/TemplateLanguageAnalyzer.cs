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
        public const string UnexpectedPositionalArgumentDiagnosticCode = "MZTT2003";

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
        public const string MissingRequiredPositionalArgumentDiagnosticCode = "MZTT2006";

        /// <summary>
        /// Diagnostic code reported when a required named argument is omitted.
        /// </summary>
        public const string MissingRequiredNamedArgumentDiagnosticCode = "MZTT2007";

        /// <summary>
        /// Diagnostic code reported when an argument's lexical value kind is
        /// not accepted by its definition.
        /// </summary>
        public const string InvalidArgumentValueKindDiagnosticCode = "MZTT2008";

        /// <summary>
        /// Applies a host language definition to an existing parse result.
        /// Parser diagnostics are preserved and combined with language
        /// diagnostics in stable source order.
        /// </summary>
        public static TemplateLanguageAnalysisResult Analyze(TemplateParseResult parseResult, TemplateLanguageDefinition language)
        {
            if (parseResult == null)
                throw new ArgumentNullException(nameof(parseResult));

            if (language == null)
                throw new ArgumentNullException(nameof(language));

            var parserDiagnostics = parseResult.Diagnostics;
            var diagnostics = new List<TemplateDiagnostic>(parserDiagnostics.Length);

            foreach (var diagnostic in parserDiagnostics)
                diagnostics.Add(diagnostic);

            var classifications = new List<NameClassification>();

            AnalyzeNodes(parseResult.Document.Nodes, parserDiagnostics, language, diagnostics, classifications);

            TemplateDiagnosticUtilities.SortBySource(diagnostics);

            var parserSyntax = parseResult.SyntaxSpans;
            var syntaxSpans = new List<TemplateSyntaxSpan>(parserSyntax.Length);

            foreach (var syntax in parserSyntax)
            {
                if (syntax.Kind == TemplateSyntaxKind.TagName)
                {
                    TemplateSyntaxKind semanticKind;

                    if (TryGetClassification(classifications, syntax.Span, out semanticKind))
                    {
                        syntaxSpans.Add(new TemplateSyntaxSpan(semanticKind, syntax.Span));
                        continue;
                    }
                }

                syntaxSpans.Add(syntax);
            }

            return new TemplateLanguageAnalysisResult(parseResult, diagnostics, syntaxSpans);
        }

        private sealed class NameClassification
        {
            public NameClassification(SourceSpan span, TemplateSyntaxKind kind)
            {
                Span = span;
                Kind = kind;
            }

            public readonly SourceSpan Span;
            public readonly TemplateSyntaxKind Kind;
        }

        private static void AnalyzeNodes(TemplateNode[] nodes, TemplateDiagnostic[] parserDiagnostics, TemplateLanguageDefinition language,
            IList<TemplateDiagnostic> diagnostics, IList<NameClassification> classifications)
        {
            foreach (var node in nodes)
            {
                var tag = node as TemplateTagNode;

                if (tag != null)
                {
                    AnalyzeTag(tag, parserDiagnostics, language, diagnostics, classifications);
                    continue;
                }

                var block = node as TemplateBlockNode;

                if (block == null)
                    continue;

                AnalyzeBlock(block, parserDiagnostics, language, diagnostics);
                AnalyzeNodes(block.Children, parserDiagnostics, language, diagnostics, classifications);
            }
        }

        private static void AnalyzeTag(TemplateTagNode tag, TemplateDiagnostic[] parserDiagnostics, TemplateLanguageDefinition language,
            IList<TemplateDiagnostic> diagnostics, IList<NameClassification> classifications)
        {
            if (tag.Name.Length == 0 ||
                TemplateDiagnosticUtilities.HasCodeWithinSpan(parserDiagnostics, TemplateParser.InvalidTagNameDiagnosticCode, tag.NameSpan))
                return;

            TemplateTagDefinition definition;

            if (language.TryGetTag(tag.Name, out definition))
            {
                classifications.Add(new NameClassification(tag.NameSpan,
                    definition.Role == TemplateTagRole.Value
                        ? TemplateSyntaxKind.ValueName
                        : TemplateSyntaxKind.CommandName)
                );

                TemplateArgumentAnalyzer.Analyze(tag.Arguments, tag.NameSpan, definition.ArgumentContract, parserDiagnostics, diagnostics);

                return;
            }

            diagnostics.Add(new TemplateDiagnostic(UnknownTagDiagnosticCode, TemplateDiagnosticSeverity.Error,
                "Unknown template tag '" + tag.Name + "'.", tag.NameSpan)
            );
        }

        private static void AnalyzeBlock(TemplateBlockNode block, TemplateDiagnostic[] parserDiagnostics, TemplateLanguageDefinition language,
            IList<TemplateDiagnostic> diagnostics)
        {
            if (block.Name.Length == 0 ||
                TemplateDiagnosticUtilities.HasCodeWithinSpan(parserDiagnostics, TemplateParser.InvalidBlockNameDiagnosticCode, block.OpenNameSpan))
                return;

            TemplateBlockDefinition definition;

            if (language.TryGetBlock(block.Name, out definition))
            {
                TemplateArgumentAnalyzer.Analyze(block.Arguments, block.OpenNameSpan, definition.ArgumentContract, parserDiagnostics, diagnostics);
                return;
            }

            diagnostics.Add(new TemplateDiagnostic(UnknownBlockDiagnosticCode, TemplateDiagnosticSeverity.Error,
                "Unknown template block '" + block.Name + "'.", block.OpenNameSpan)
            );
        }

        private static bool TryGetClassification(IList<NameClassification> classifications, SourceSpan span, out TemplateSyntaxKind kind)
        {
            foreach (var classification in classifications)
            {
                if (classification.Span != span) continue;

                kind = classification.Kind;
                return true;
            }

            kind = TemplateSyntaxKind.TagName;
            return false;
        }
    }
}
