using System;
using System.Collections;
using System.Collections.Generic;
using Mz.Toml.Internal;

namespace Mz.Toml
{
    /// <summary>
    /// Represents an ordered heterogeneous TOML array.
    /// </summary>
    public sealed class TomlArray : TomlNode, IEnumerable<TomlNode>
    {
        private readonly List<TomlNode> _items;
        private readonly IReadOnlyList<TomlNode> _readOnlyItems;
        private readonly TomlArrayDefinitionKind _definitionKind;

        /// <summary>
        /// Initializes an empty programmatic TOML array.
        /// </summary>
        public TomlArray()
            : this(0, 0, TomlArrayDefinitionKind.Static)
        {
        }

        internal TomlArray(int line, int column)
            : this(line, column, TomlArrayDefinitionKind.Static)
        {
        }

        internal TomlArray(int line, int column, TomlArrayDefinitionKind definitionKind)
            : base(TomlNodeKind.Array, line, column)
        {
            _items = new List<TomlNode>();
            _readOnlyItems = new TomlReadOnlyList<TomlNode>(_items);
            _definitionKind = definitionKind;
        }

        internal TomlArrayDefinitionKind DefinitionKind => _definitionKind;

        /// <summary>
        /// Gets the number of elements in the array.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Gets the array elements as a read-only ordered list.
        /// </summary>
        public IReadOnlyList<TomlNode> Items => _readOnlyItems;

        /// <summary>
        /// Gets the element at the specified zero-based index.
        /// </summary>
        public TomlNode this[int index] => _items[index];

        /// <summary>
        /// Appends an element to the array.
        /// TOML 1.0 arrays may contain heterogeneous element kinds.
        /// </summary>
        public void Add(TomlNode value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            _items.Add(value);
        }

        /// <summary>
        /// Returns an enumerator over the array elements.
        /// </summary>
        public IEnumerator<TomlNode> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
