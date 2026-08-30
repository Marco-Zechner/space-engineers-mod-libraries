namespace Mz.Toml
{
    /// <summary>
    /// Identifies the structural kind of a TOML node.
    /// </summary>
    public enum TomlNodeKind
    {
        /// <summary>
        /// A TOML table.
        /// </summary>
        Table,

        /// <summary>
        /// A scalar TOML value.
        /// </summary>
        Value
    }
}