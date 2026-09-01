using System;

namespace Mz.Toml
{
    /// <summary>
    /// Composes source-preserving TOML edits while refreshing syntax after every
    /// successful change. Each edit is validated by reparsing the complete resulting
    /// source. A failed edit leaves the current source and syntax unchanged.
    /// </summary>
    public sealed class TomlSourceEditor
    {
        private TomlSyntaxDocument _syntax;

        internal TomlSourceEditor(TomlSyntaxDocument syntax)
        {
            if (syntax == null)
                throw new ArgumentNullException(nameof(syntax));

            _syntax = syntax;
        }

        /// <summary>
        /// Gets the exact current TOML source.
        /// </summary>
        public string Source => _syntax.Source;

        /// <summary>
        /// Gets the current source-preserving syntax document.
        /// A successful edit replaces this syntax document, so callers must reacquire
        /// node references before making another node-based edit.
        /// </summary>
        public TomlSyntaxDocument Syntax => _syntax;

        /// <summary>
        /// Disables the specified active assignment and refreshes the current syntax.
        /// </summary>
        public void DisableAssignment(TomlSyntaxNode node)
        {
            Apply(_syntax.DisableAssignment(node));
        }

        /// <summary>
        /// Enables the specified disabled assignment and refreshes the current syntax.
        /// </summary>
        public void EnableAssignment(TomlSyntaxNode node)
        {
            Apply(_syntax.EnableAssignment(node));
        }

        /// <summary>
        /// Replaces only the exact value source of the specified assignment and
        /// refreshes the current syntax.
        /// </summary>
        public void ReplaceAssignmentValue(TomlSyntaxNode node, string valueSource)
        {
            Apply(_syntax.ReplaceAssignmentValue(node, valueSource));
        }

        /// <summary>
        /// Inserts validated TOML source immediately before the specified node and
        /// refreshes the current syntax.
        /// </summary>
        public void InsertSourceBefore(TomlSyntaxNode node, string sourceFragment)
        {
            Apply(_syntax.InsertSourceBefore(node, sourceFragment));
        }

        /// <summary>
        /// Inserts validated TOML source immediately after the specified node and
        /// refreshes the current syntax.
        /// </summary>
        public void InsertSourceAfter(TomlSyntaxNode node, string sourceFragment)
        {
            Apply(_syntax.InsertSourceAfter(node, sourceFragment));
        }

        /// <summary>
        /// Inserts validated TOML source at the beginning of the document and
        /// refreshes the current syntax.
        /// </summary>
        public void InsertSourceAtStart(string sourceFragment)
        {
            Apply(_syntax.InsertSourceAtStart(sourceFragment));
        }

        /// <summary>
        /// Inserts validated TOML source at the end of the document and refreshes
        /// the current syntax.
        /// </summary>
        public void InsertSourceAtEnd(string sourceFragment)
        {
            Apply(_syntax.InsertSourceAtEnd(sourceFragment));
        }

        private void Apply(string editedSource)
        {
            var parsed = Toml.TryParse(editedSource);

            if (!parsed.IsSuccess || parsed.Syntax == null)
            {
                var message = "The source edit would make the TOML document invalid.";

                if (parsed.Diagnostics.Count > 0)
                    message += " " + parsed.Diagnostics[0];

                throw new InvalidOperationException(message);
            }

            _syntax = parsed.Syntax;
        }
    }
}
