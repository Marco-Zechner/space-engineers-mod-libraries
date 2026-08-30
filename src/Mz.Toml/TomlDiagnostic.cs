using System;

namespace Mz.Toml
{
    /// <summary>
    /// Describes a TOML parse error at a source location.
    /// </summary>
    public sealed class TomlDiagnostic
    {
        private readonly TomlDiagnosticCode _code;
        private readonly string _message;
        private readonly int _line;
        private readonly int _column;

        /// <summary>
        /// Initializes a TOML diagnostic.
        /// </summary>
        internal TomlDiagnostic(
            TomlDiagnosticCode code,
            string message,
            int line,
            int column)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentException("Diagnostic message cannot be null or empty.", "message");
            if (line < 1)
                throw new ArgumentException("Diagnostic line must be at least 1.", "line");
            if (column < 1)
                throw new ArgumentException("Diagnostic column must be at least 1.", "column");

            _code = code;
            _message = message;
            _line = line;
            _column = column;
        }

        /// <summary>
        /// Gets the stable diagnostic code.
        /// </summary>
        public TomlDiagnosticCode Code
        {
            get { return _code; }
        }

        /// <summary>
        /// Gets the human-readable diagnostic message.
        /// </summary>
        public string Message
        {
            get { return _message; }
        }

        /// <summary>
        /// Gets the one-based source line.
        /// </summary>
        public int Line
        {
            get { return _line; }
        }

        /// <summary>
        /// Gets the one-based source column.
        /// </summary>
        public int Column
        {
            get { return _column; }
        }

        /// <summary>
        /// Formats the diagnostic for logs and exceptions.
        /// </summary>
        public override string ToString()
        {
            return "TOML " + _code + " at " + _line + ":" + _column + ": " + _message;
        }
    }
}