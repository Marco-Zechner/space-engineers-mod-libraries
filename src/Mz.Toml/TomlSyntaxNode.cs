namespace Mz.Toml
{
    /// <summary>
    /// Represents one ordered source-preserving TOML syntax range.
    /// </summary>
    public sealed class TomlSyntaxNode
    {
        internal TomlSyntaxNode(TomlSyntaxNodeKind kind, TomlSourceSpan span)
        {
            Kind = kind;
            Span = span;
        }

        /// <summary>
        /// Gets the syntax node kind.
        /// </summary>
        public TomlSyntaxNodeKind Kind { get; }

        /// <summary>
        /// Gets the exact source range occupied by this node.
        /// </summary>
        public TomlSourceSpan Span { get; }
    }
}