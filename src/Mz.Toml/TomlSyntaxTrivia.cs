namespace Mz.Toml
{
    /// <summary>
    /// Represents one exact whitespace, newline, or comment range in TOML source.
    /// </summary>
    public sealed class TomlSyntaxTrivia
    {
        internal TomlSyntaxTrivia(TomlSyntaxTriviaKind kind, TomlSourceSpan span)
        {
            Kind = kind;
            Span = span;
        }

        /// <summary>
        /// Gets the trivia kind.
        /// </summary>
        public TomlSyntaxTriviaKind Kind { get; }

        /// <summary>
        /// Gets the exact source range occupied by this trivia.
        /// </summary>
        public TomlSourceSpan Span { get; }
    }
}