using System;
using System.Collections.Generic;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Contains a parsed template document together with editor-oriented
    /// syntax spans and recoverable diagnostics.
    /// </summary>
    public sealed class TemplateParseResult
    {
        private readonly TemplateDiagnostic[] _diagnostics;
        private readonly TemplateSyntaxSpan[] _syntaxSpans;

        internal TemplateParseResult(
            string source,
            TemplateDocument document,
            IList<TemplateDiagnostic> diagnostics,
            IList<TemplateSyntaxSpan> syntaxSpans
        )
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (document == null)
                throw new ArgumentNullException("document");

            if (diagnostics == null)
                throw new ArgumentNullException("diagnostics");

            if (syntaxSpans == null)
                throw new ArgumentNullException("syntaxSpans");

            Source = source;
            Document = document;

            _diagnostics = new TemplateDiagnostic[diagnostics.Count];

            for (int index = 0; index < diagnostics.Count; index++)
                _diagnostics[index] = diagnostics[index];

            _syntaxSpans = new TemplateSyntaxSpan[syntaxSpans.Count];

            for (int index = 0; index < syntaxSpans.Count; index++)
                _syntaxSpans[index] = syntaxSpans[index];
        }

        /// <summary>
        /// Gets the exact source string supplied to the parser.
        /// </summary>
        public string Source { get; private set; }

        /// <summary>
        /// Gets the parsed template syntax tree.
        /// </summary>
        public TemplateDocument Document { get; private set; }

        /// <summary>
        /// Gets a defensive copy of diagnostics in stable source order.
        /// </summary>
        public TemplateDiagnostic[] Diagnostics
        {
            get
            {
                var copy = new TemplateDiagnostic[_diagnostics.Length];
                Array.Copy(_diagnostics, copy, _diagnostics.Length);
                return copy;
            }
        }

        /// <summary>
        /// Gets a defensive copy of syntax spans in stable source order.
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
        /// Gets whether at least one error diagnostic was produced.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                for (int index = 0; index < _diagnostics.Length; index++)
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