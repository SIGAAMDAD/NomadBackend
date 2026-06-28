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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class SparseSet<TValue>
    {
        private int[] _denseIds;
        private int[] _sparse;
        private TValue[] _values;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }
        public Span<int> DenseIds { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _denseIds.AsSpan(0, _count); } }
        public Span<TValue> Values { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _values.AsSpan(0, _count); } }

        public SparseSet(int idCapacity = 64, int denseCapacity = 64)
        {
            if (idCapacity < 1)
            {
                idCapacity = 1;
            }

            if (denseCapacity < 1)
            {
                denseCapacity = 1;
            }

            _denseIds = new int[denseCapacity];
            _values = new TValue[denseCapacity];
            _sparse = new int[idCapacity];
            Array.Fill(_sparse, -1);
        }

        public ref TValue this[int id]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { Debug.Assert(Contains(id)); return ref _values[_sparse[id]]; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int id)
        {
            return (uint)id < (uint)_sparse.Length && (uint)_sparse[id] < (uint)_count && _denseIds[_sparse[id]] == id;
        }

        public bool Add(int id, TValue value)
        {
            Debug.Assert(id >= 0);
            EnsureIdCapacity(id + 1);
            if (Contains(id))
            {
                return false;
            }

            if (_count == _denseIds.Length)
            {
                GrowDense();
            }

            _sparse[id] = _count;
            _denseIds[_count] = id;
            _values[_count] = value;
            _count++;
            return true;
        }

        public void Set(int id, TValue value)
        {
            if (Contains(id))
            {
                _values[_sparse[id]] = value;
            }
            else
            {
                Add(id, value);
            }
        }

        public bool Remove(int id)
        {
            if (!Contains(id))
            {
                return false;
            }

            int index = _sparse[id];
            int lastIndex = _count - 1;
            int lastId = _denseIds[lastIndex];
            _denseIds[index] = lastId;
            _values[index] = _values[lastIndex];
            _sparse[lastId] = index;
            _denseIds[lastIndex] = 0;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                _values[lastIndex] = default!;
            }

            _sparse[id] = -1;
            _count = lastIndex;
            return true;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDenseId(int index)
        {
            Debug.Assert((uint)index < (uint)_count);
            return _denseIds[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref TValue GetValueByDenseIndexRef(int index)
        {
            Debug.Assert((uint)index < (uint)_count);
            return ref _values[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TValue GetValueByDenseIndex(int index)
        {
            Debug.Assert((uint)index < (uint)_count);
            return _values[index];
        }

        public KeyValuePair<int, TValue>[] ToPairArray()
        {
            KeyValuePair<int, TValue>[] result = new KeyValuePair<int, TValue>[_count];
            for (int i = 0; i < _count; i++)
            {
                result[i] = new KeyValuePair<int, TValue>(_denseIds[i], _values[i]);
            }
            return result;
        }

        public void Clear()
        {
            for (int i = 0; i < _count; i++)
            {
                _sparse[_denseIds[i]] = -1;
            }

            Array.Clear(_denseIds, 0, _count);
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TValue>())
            {
                Array.Clear(_values, 0, _count);
            }

            _count = 0;
        }

        public void EnsureIdCapacity(int capacity)
        {
            if (capacity <= _sparse.Length)
            {
                return;
            }

            int old = _sparse.Length;
            Array.Resize(ref _sparse, CollectionMath.NextPowerOfTwo(capacity));
            for (int i = old; i < _sparse.Length; i++)
            {
                _sparse[i] = -1;
            }
        }

        private void GrowDense()
        {
            int newCapacity = _denseIds.Length << 1;
            Array.Resize(ref _denseIds, newCapacity);
            Array.Resize(ref _values, newCapacity);
        }
    }
}
