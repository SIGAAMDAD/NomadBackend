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
    public sealed class HandlePool<T>
    {
        private T[] _values;
        private int[] _generations;
        private int[] _nextFree;
        private bool[] _occupied;
        private int _freeHead;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }

        public HandlePool(int initialCapacity = 64)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _values = new T[initialCapacity];
            _generations = new int[initialCapacity];
            _nextFree = new int[initialCapacity];
            _occupied = new bool[initialCapacity];
            for (int i = 0; i < initialCapacity - 1; i++)
            {
                _nextFree[i] = i + 1;
            }

            _nextFree[initialCapacity - 1] = -1;
            _freeHead = 0;
        }

        public Handle Allocate(T value)
        {
            if (_freeHead < 0)
            {
                Grow();
            }

            int index = _freeHead;
            _freeHead = _nextFree[index];
            _values[index] = value;
            _occupied[index] = true;
            _count++;
            return new Handle(index, _generations[index]);
        }

        public bool Free(Handle handle)
        {
            if (!IsAlive(handle))
            {
                return false;
            }

            int index = handle.Index;
            _occupied[index] = false;
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                _values[index] = default!;
            }

            _generations[index]++;
            _nextFree[index] = _freeHead;
            _freeHead = index;
            _count--;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAlive(Handle handle)
        {
            return (uint)handle.Index < (uint)_values.Length && _occupied[handle.Index] && _generations[handle.Index] == handle.Generation;
        }

        public ref T Get(Handle handle)
        {
            Debug.Assert(IsAlive(handle));
            return ref _values[handle.Index];
        }

        private void Grow()
        {
            int old = _values.Length;
            int next = old << 1;
            Array.Resize(ref _values, next);
            Array.Resize(ref _generations, next);
            Array.Resize(ref _nextFree, next);
            Array.Resize(ref _occupied, next);
            for (int i = old; i < next - 1; i++)
            {
                _nextFree[i] = i + 1;
            }

            _nextFree[next - 1] = -1;
            _freeHead = old;
        }
    }
}
