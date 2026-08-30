namespace Mz.Toml
{
    /// <summary>
    /// Base type for nodes in a TOML document.
    /// </summary>
    public abstract class TomlNode
    {
        private readonly TomlNodeKind _kind;
        private readonly int _line;
        private readonly int _column;

        internal TomlNode(TomlNodeKind kind, int line, int column)
        {
            _kind = kind;
            _line = line;
            _column = column;
        }

        /// <summary>
        /// Gets the structural node kind.
        /// </summary>
        public TomlNodeKind Kind
        {
            get { return _kind; }
        }

        /// <summary>
        /// Gets the one-based source line where the node originated.
        /// A value of zero indicates a programmatically-created node.
        /// </summary>
        public int Line
        {
            get { return _line; }
        }

        /// <summary>
        /// Gets the one-based source column where the node originated.
        /// A value of zero indicates a programmatically-created node.
        /// </summary>
        public int Column
        {
            get { return _column; }
        }
    }
}