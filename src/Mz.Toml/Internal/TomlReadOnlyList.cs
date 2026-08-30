using System;
using System.Collections;
using System.Collections.Generic;

namespace Mz.Toml.Internal
{
    internal sealed class TomlReadOnlyList<T> : IReadOnlyList<T>
    {
        private readonly IList<T> _items;

        public TomlReadOnlyList(IList<T> items)
        {
            if (items == null)
                throw new ArgumentNullException("items");

            _items = items;
        }

        public int Count
        {
            get { return _items.Count; }
        }

        public T this[int index]
        {
            get { return _items[index]; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}