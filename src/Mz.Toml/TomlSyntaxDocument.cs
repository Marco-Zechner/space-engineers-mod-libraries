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

        /// <summary>
        /// Returns source with the specified active assignment disabled by inserting
        /// the custom '#!' marker at the assignment start.
        /// </summary>
        public string DisableAssignment(TomlSyntaxNode node)
        {
            ValidateOwnedNode(node);

            if (node.Kind != TomlSyntaxNodeKind.Assignment)
                throw new ArgumentException("The syntax node is not an active assignment.", nameof(node));

            return Source.Insert(node.Span.Start, "#!");
        }

        /// <summary>
        /// Returns source with the specified disabled assignment enabled by removing
        /// only its custom '#!' marker.
        /// </summary>
        public string EnableAssignment(TomlSyntaxNode node)
        {
            ValidateOwnedNode(node);

            if (node.Kind != TomlSyntaxNodeKind.DisabledAssignment)
                throw new ArgumentException("The syntax node is not a disabled assignment.", nameof(node));

            if (node.Span.Length < 2 ||
                node.Span.Start + 1 >= Source.Length ||
                Source[node.Span.Start] != '#' ||
                Source[node.Span.Start + 1] != '!')
            {
                throw new InvalidOperationException("The disabled assignment does not contain the expected '#!' marker.");
            }

            return Source.Remove(node.Span.Start, 2);
        }

        /// <summary>
        /// Returns source with only the exact value range of the specified active
        /// or disabled assignment replaced by <paramref name="valueSource"/>.
        /// </summary>
        public string ReplaceAssignmentValue(TomlSyntaxNode node, string valueSource)
        {
            ValidateOwnedNode(node);

            if (node.Kind != TomlSyntaxNodeKind.Assignment &&
                node.Kind != TomlSyntaxNodeKind.DisabledAssignment)
            {
                throw new ArgumentException(
                    "The syntax node is not an active or disabled assignment.",
                    nameof(node));
            }

            if (valueSource == null)
                throw new ArgumentNullException(nameof(valueSource));

            ValidateValueSource(valueSource);

            if (!node.ValueSpan.HasValue)
                throw new InvalidOperationException("The assignment syntax node does not contain a value span.");

            var valueSpan = node.ValueSpan.Value;

            return Source
                .Remove(valueSpan.Start, valueSpan.Length)
                .Insert(valueSpan.Start, valueSource);
        }

        private static void ValidateValueSource(string valueSource)
        {
            const string prefix = "value = ";
            var parsed = Toml.TryParse(prefix + valueSource);

            if (!parsed.IsSuccess ||
                parsed.Syntax == null ||
                parsed.Syntax.Nodes.Count == 0 ||
                parsed.Syntax.Nodes[0].Kind != TomlSyntaxNodeKind.Assignment ||
                !parsed.Syntax.Nodes[0].ValueSpan.HasValue)
            {
                throw new ArgumentException(
                    "Replacement text must be exactly one valid TOML value.",
                    nameof(valueSource));
            }

            var span = parsed.Syntax.Nodes[0].ValueSpan.Value;

            if (span.Start != prefix.Length || span.Length != valueSource.Length)
            {
                throw new ArgumentException(
                    "Replacement text must be exactly one valid TOML value.",
                    nameof(valueSource));
            }
        }

        private void ValidateOwnedNode(TomlSyntaxNode node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            for (var index = 0; index < Nodes.Count; index++)
            {
                if (ReferenceEquals(Nodes[index], node))
                    return;
            }

            throw new ArgumentException("The syntax node does not belong to this syntax document.", nameof(node));
        }
    }
}
