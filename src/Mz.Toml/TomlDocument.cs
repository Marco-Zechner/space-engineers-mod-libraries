using System;

namespace Mz.Toml
{
    /// <summary>
    /// Represents a parsed or programmatically-created TOML document.
    /// </summary>
    public sealed class TomlDocument
    {
        private readonly TomlTable _root;

        /// <summary>
        /// Initializes an empty TOML document.
        /// </summary>
        public TomlDocument()
            : this(new TomlTable())
        {
        }

        /// <summary>
        /// Initializes a TOML document with the specified root table.
        /// </summary>
        public TomlDocument(TomlTable root)
        {
            if (root == null)
                throw new ArgumentNullException("root");

            _root = root;
        }

        /// <summary>
        /// Gets the root table.
        /// </summary>
        public TomlTable Root
        {
            get { return _root; }
        }
    }
}