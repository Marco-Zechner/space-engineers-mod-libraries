using System;
using System.Collections.Generic;
using Mz.Toml.Internal;

namespace Mz.Toml
{
    /// <summary>
    /// Preserves the exact TOML source string and its ordered top-level syntax ranges.
    /// </summary>
    public sealed class TomlSyntaxDocument
    {
        internal TomlSyntaxDocument(
            string source,
            IEnumerable<TomlSyntaxNode> nodes,
            IEnumerable<TomlSyntaxTrivia> trivia)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (nodes == null)
                throw new ArgumentNullException(nameof(nodes));
            if (trivia == null)
                throw new ArgumentNullException(nameof(trivia));

            Source = source;
            Nodes = new TomlReadOnlyList<TomlSyntaxNode>(new List<TomlSyntaxNode>(nodes));
            Trivia = new TomlReadOnlyList<TomlSyntaxTrivia>(new List<TomlSyntaxTrivia>(trivia));
        }

        /// <summary>
        /// Gets the exact source string supplied to the parser.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Gets source-preserving syntax nodes in source order.
        /// </summary>
        public IReadOnlyList<TomlSyntaxNode> Nodes { get; }

        /// <summary>
        /// Gets whitespace, newline, and comment ranges in exact source order.
        /// Trivia may lie inside a larger syntax node such as a multiline array assignment.
        /// </summary>
        public IReadOnlyList<TomlSyntaxTrivia> Trivia { get; }
    }
}
