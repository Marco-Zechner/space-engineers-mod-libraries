namespace Mz.TextTemplate
{
    /// <summary>
    /// Identifies the semantic role of a template source range for editor
    /// presentation such as syntax highlighting.
    /// </summary>
    public enum TemplateSyntaxKind
    {
        /// <summary>
        /// Literal text outside template tags.
        /// </summary>
        LiteralText = 0,

        /// <summary>
        /// Structural template delimiter such as "{{" or "}}".
        /// </summary>
        Delimiter = 1,

        /// <summary>
        /// Name identifying a template tag.
        /// </summary>
        TagName = 2
    }

    /// <summary>
    /// Associates a semantic syntax kind with an exact source range.
    /// </summary>
    public sealed class TemplateSyntaxSpan
    {
        /// <summary>
        /// Creates a semantic syntax span.
        /// </summary>
        public TemplateSyntaxSpan(
            TemplateSyntaxKind kind,
            SourceSpan span
        )
        {
            Kind = kind;
            Span = span;
        }

        /// <summary>
        /// Gets the semantic syntax kind.
        /// </summary>
        public TemplateSyntaxKind Kind { get; private set; }

        /// <summary>
        /// Gets the exact source range represented by this syntax span.
        /// </summary>
        public SourceSpan Span { get; private set; }
    }
}