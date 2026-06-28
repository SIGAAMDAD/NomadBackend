/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T>? _reset;
        private T[] _items;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }

        public ObjectPool(Func<T> factory, Action<T>? reset = null, int initialCapacity = 32)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _factory = factory;
            _reset = reset;
            _items = new T[initialCapacity];
        }

        public T Rent()
        {
            if (_count == 0)
            {
                return _factory();
            }

            T item = _items[--_count];
            _items[_count] = null!;
            return item;
        }

        public void Return(T item)
        {
            if (item == null)
            {
                return;
            }

            _reset?.Invoke(item);
            if (_count == _items.Length)
            {
                Array.Resize(ref _items, _items.Length << 1);
            }

            _items[_count++] = item;
        }
    }
}
