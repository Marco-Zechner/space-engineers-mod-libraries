using System;

namespace Mz.Toml
{
    /// <summary>
    /// Provides the primary TOML parsing and writing API.
    /// </summary>
    public static class Toml
    {
        /// <summary>
        /// Parses TOML text and throws <see cref="TomlParseException"/> on failure.
        /// </summary>
        public static TomlDocument Parse(string text)
        {
            var result = TryParse(text);
            if (result.IsSuccess)
                return result.Document;

            throw new TomlParseException(result.Diagnostics[0]);
        }

        /// <summary>
        /// Parses TOML text without throwing for syntax errors.
        /// </summary>
        public static TomlParseResult TryParse(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");

            return Internal.TomlParser.Parse(text);
        }

        /// <summary>
        /// Writes a TOML document using deterministic formatting.
        /// </summary>
        public static string Write(TomlDocument document)
        {
            if (document == null)
                throw new ArgumentNullException("document");

            return Internal.TomlWriter.Write(document);
        }
    }
}