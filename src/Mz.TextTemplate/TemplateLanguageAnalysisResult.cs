using System;
using System.Collections.Generic;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Contains parser output enriched with host-language diagnostics and
    /// semantic editor classifications.
    /// </summary>
    public sealed class TemplateLanguageAnalysisResult
    {
        private readonly TemplateDiagnostic[] _diagnostics;
        private readonly TemplateSyntaxSpan[] _syntaxSpans;

        internal TemplateLanguageAnalysisResult(
            TemplateParseResult parseResult,
            IList<TemplateDiagnostic> diagnostics,
            IList<TemplateSyntaxSpan> syntaxSpans
        )
        {
            if (parseResult == null)
                throw new ArgumentNullException(nameof(parseResult));

            if (diagnostics == null)
                throw new ArgumentNullException(nameof(diagnostics));

            if (syntaxSpans == null)
                throw new ArgumentNullException(nameof(syntaxSpans));

            ParseResult = parseResult;

            _diagnostics =
                new TemplateDiagnostic[diagnostics.Count];

            for (
                int index = 0;
                index < diagnostics.Count;
                index++
            )
            {
                _diagnostics[index] =
                    diagnostics[index];
            }

            _syntaxSpans =
                new TemplateSyntaxSpan[syntaxSpans.Count];

            for (
                int index = 0;
                index < syntaxSpans.Count;
                index++
            )
            {
                _syntaxSpans[index] =
                    syntaxSpans[index];
            }
        }

        /// <summary>
        /// Gets the original syntactic parse result.
        /// </summary>
        public TemplateParseResult ParseResult { get; private set; }

        /// <summary>
        /// Gets the exact template source supplied to the parser.
        /// </summary>
        public string Source
        {
            get { return ParseResult.Source; }
        }

        /// <summary>
        /// Gets the parsed template document.
        /// </summary>
        public TemplateDocument Document
        {
            get { return ParseResult.Document; }
        }

        /// <summary>
        /// Gets parser and host-language diagnostics in stable source order.
        /// </summary>
        public TemplateDiagnostic[] Diagnostics
        {
            get
            {
                var copy =
                    new TemplateDiagnostic[_diagnostics.Length];

                Array.Copy(
                    _diagnostics,
                    copy,
                    _diagnostics.Length
                );

                return copy;
            }
        }

        /// <summary>
        /// Gets syntax spans with known normal-tag names semantically
        /// classified as values or commands.
        /// </summary>
        public TemplateSyntaxSpan[] SyntaxSpans
        {
            get
            {
                var copy =
                    new TemplateSyntaxSpan[_syntaxSpans.Length];

                Array.Copy(
                    _syntaxSpans,
                    copy,
                    _syntaxSpans.Length
                );

                return copy;
            }
        }

        /// <summary>
        /// Gets whether parser or language analysis produced an error.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                for (
                    int index = 0;
                    index < _diagnostics.Length;
                    index++
                )
                {
                    if (
                        _diagnostics[index].Severity
                        == TemplateDiagnosticSeverity.Error
                    )
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
