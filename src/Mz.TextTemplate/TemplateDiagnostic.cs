using System;

namespace Mz.TextTemplate
{
    /// <summary>
    /// Describes the severity of a template diagnostic.
    /// </summary>
    public enum TemplateDiagnosticSeverity
    {
        /// <summary>
        /// Informational diagnostic that does not indicate invalid input.
        /// </summary>
        Info = 0,

        /// <summary>
        /// Diagnostic describing suspicious but usable input.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Diagnostic describing invalid template input.
        /// </summary>
        Error = 2
    }

    /// <summary>
    /// Describes one parser or language diagnostic tied to template source.
    /// </summary>
    public sealed class TemplateDiagnostic
    {
        /// <summary>
        /// Creates a diagnostic with a stable code, severity, message, and
        /// exact source span.
        /// </summary>
        internal TemplateDiagnostic(
            string code,
            TemplateDiagnosticSeverity severity,
            string message,
            SourceSpan span
        )
        {
            if (code == null)
                throw new ArgumentNullException("code");

            if (message == null)
                throw new ArgumentNullException("message");

            Code = code;
            Severity = severity;
            Message = message;
            Span = span;
        }

        /// <summary>
        /// Gets the stable machine-readable diagnostic code.
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// Gets the diagnostic severity.
        /// </summary>
        public TemplateDiagnosticSeverity Severity { get; private set; }

        /// <summary>
        /// Gets the human-readable diagnostic message.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Gets the exact source range associated with the diagnostic.
        /// </summary>
        public SourceSpan Span { get; private set; }
    }
}