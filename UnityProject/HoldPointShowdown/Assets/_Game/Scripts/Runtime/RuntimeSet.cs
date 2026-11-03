using System.Collections.Generic;
using UnityEngine;

namespace HoldPointShowdown.Runtime
{
    public abstract class RuntimeSet<T> : ScriptableObject where T : Object
    {
        private readonly List<T> _items = new();

        public IReadOnlyList<T> Items => _items;
        public int Count => _items.Count;

        public void Add(T item)
        {
            if (item != null && !_items.Contains(item))
            {
                _items.Add(item);
            }
        }

        public void Remove(T item)
        {
            if (item != null)
            {
                _items.Remove(item);
            }
        }

        public void RemoveMissingEntries()
        {
            _items.RemoveAll(item => item == null);
        }

        public void Clear()
        {
            _items.Clear();
        }

        protected virtual void OnEnable()
        {
            _items.Clear();
        }
    }
}
