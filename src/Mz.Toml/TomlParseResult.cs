using System.Collections.Generic;
using Mz.Toml.Internal;

namespace Mz.Toml
{
    /// <summary>
    /// Represents the result of a non-throwing TOML parse operation.
    /// </summary>
    public sealed class TomlParseResult
    {
        private readonly TomlDocument _document;
        private readonly IReadOnlyList<TomlDiagnostic> _diagnostics;

        internal TomlParseResult(
            TomlDocument document,
            IEnumerable<TomlDiagnostic> diagnostics)
        {
            var copy = new List<TomlDiagnostic>(diagnostics);
            _document = document;
            _diagnostics = new TomlReadOnlyList<TomlDiagnostic>(copy);
        }

        /// <summary>
        /// Gets a value indicating whether parsing succeeded.
        /// </summary>
        public bool IsSuccess
        {
            get { return _document != null && _diagnostics.Count == 0; }
        }

        /// <summary>
        /// Gets the parsed document, or null when parsing failed.
        /// </summary>
        public TomlDocument Document
        {
            get { return _document; }
        }

        /// <summary>
        /// Gets parse diagnostics. The collection is empty on success.
        /// </summary>
        public IReadOnlyList<TomlDiagnostic> Diagnostics
        {
            get { return _diagnostics; }
        }
    }
}