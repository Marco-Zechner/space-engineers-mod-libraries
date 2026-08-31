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
        TagName = 2,

        /// <summary>
        /// Name identifying a named argument.
        /// </summary>
        ArgumentName = 3,

        /// <summary>
        /// Equals sign joining a named argument to its value.
        /// </summary>
        AssignmentOperator = 4,

        /// <summary>
        /// Unquoted value with no more specific lexical classification.
        /// </summary>
        BareValue = 5,

        /// <summary>
        /// Numeric-looking unquoted argument value.
        /// </summary>
        NumberValue = 6,

        /// <summary>
        /// Boolean argument literal.
        /// </summary>
        BooleanValue = 7,

        /// <summary>
        /// Double-quoted string argument value.
        /// </summary>
        StringValue = 8,

        /// <summary>
        /// Structural block marker "#" or "/".
        /// </summary>
        BlockMarker = 9,

        /// <summary>
        /// Name identifying an opening or closing block.
        /// </summary>
        BlockName = 10,

        /// <summary>
        /// Name of a tag defined by the host as a value.
        /// </summary>
        ValueName = 11,

        /// <summary>
        /// Name of a tag defined by the host as a command.
        /// </summary>
        CommandName = 12
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