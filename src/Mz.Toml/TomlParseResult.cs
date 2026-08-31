using System.Collections.Generic;
using Mz.Toml.Internal;

namespace Mz.Toml
{
    /// <summary>
    /// Represents the result of a non-throwing TOML parse operation.
    /// </summary>
    public sealed class TomlParseResult
    {
        internal TomlParseResult(TomlDocument document, IEnumerable<TomlDiagnostic> diagnostics)
        {
            var copy = new List<TomlDiagnostic>(diagnostics);
            Document = document;
            Diagnostics = new TomlReadOnlyList<TomlDiagnostic>(copy);
        }

        /// <summary>
        /// Gets a value indicating whether parsing succeeded.
        /// </summary>
        public bool IsSuccess => Document != null && Diagnostics.Count == 0;

        /// <summary>
        /// Gets the parsed document, or null when parsing failed.
        /// </summary>
        public TomlDocument Document { get; }

        /// <summary>
        /// Gets parse diagnostics. The collection is empty on success.
        /// </summary>
        public IReadOnlyList<TomlDiagnostic> Diagnostics { get; }
    }
}
