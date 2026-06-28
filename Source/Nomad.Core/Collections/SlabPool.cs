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
    public sealed class SlabPool<T>
    {
        private readonly int _slabSize;
        private T[][] _slabs;
        private int[] _free;
        private int _freeCount;
        private int _allocated;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _allocated - _freeCount; } }

        public SlabPool(int slabSize = 256)
        {
            if (slabSize < 1)
            {
                slabSize = 1;
            }

            _slabSize = slabSize;
            _slabs = new T[1][];
            _slabs[0] = new T[_slabSize];
            _free = new int[_slabSize];
            _allocated = _slabSize;
            _freeCount = _slabSize;
            for (int i = 0; i < _slabSize; i++)
            {
                _free[i] = _slabSize - 1 - i;
            }
        }

        public int Allocate()
        {
            if (_freeCount == 0)
            {
                AddSlab();
            }

            return _free[--_freeCount];
        }

        public void Free(int handle)
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Get(handle) = default!;
            }

            if (_freeCount == _free.Length)
            {
                Array.Resize(ref _free, _free.Length << 1);
            }

            _free[_freeCount++] = handle;
        }

        public ref T Get(int handle)
        {
            Debug.Assert((uint)handle < (uint)_allocated);
            return ref _slabs[handle / _slabSize][handle % _slabSize];
        }

        private void AddSlab()
        {
            int slabIndex = _slabs.Length;
            Array.Resize(ref _slabs, slabIndex + 1);
            _slabs[slabIndex] = new T[_slabSize];
            int oldAllocated = _allocated;
            _allocated += _slabSize;
            if (_free.Length < _freeCount + _slabSize)
            {
                Array.Resize(ref _free, _freeCount + _slabSize);
            }

            for (int i = 0; i < _slabSize; i++)
            {
                _free[_freeCount++] = oldAllocated + i;
            }
        }
    }
}
