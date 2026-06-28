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
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class SparseSet
    {
        private int[] _dense;
        private int[] _sparse;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }
        public Span<int> DenseIds { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _dense.AsSpan(0, _count); } }

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

            _dense = new int[denseCapacity];
            _sparse = new int[idCapacity];
            Array.Fill(_sparse, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int id)
        {
            return (uint)id < (uint)_sparse.Length && (uint)_sparse[id] < (uint)_count && _dense[_sparse[id]] == id;
        }

        public bool Add(int id)
        {
            Debug.Assert(id >= 0);
            EnsureIdCapacity(id + 1);
            if (Contains(id))
            {
                return false;
            }

            if (_count == _dense.Length)
            {
                Array.Resize(ref _dense, _dense.Length << 1);
            }

            _sparse[id] = _count;
            _dense[_count++] = id;
            return true;
        }

        public bool Remove(int id)
        {
            if (!Contains(id))
            {
                return false;
            }

            int index = _sparse[id];
            int lastIndex = _count - 1;
            int lastId = _dense[lastIndex];
            _dense[index] = lastId;
            _sparse[lastId] = index;
            _dense[lastIndex] = 0;
            _sparse[id] = -1;
            _count = lastIndex;
            return true;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDenseId(int index)
        {
            Debug.Assert((uint)index < (uint)_count);
            return _dense[index];
        }

        public int[] ToArray()
        {
            int[] result = new int[_count];
            Array.Copy(_dense, 0, result, 0, _count);
            return result;
        }

        public void Clear()
        {
            for (int i = 0; i < _count; i++)
            {
                _sparse[_dense[i]] = -1;
            }

            Array.Clear(_dense, 0, _count);
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
    }
}
