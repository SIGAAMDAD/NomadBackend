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
    public sealed class IndexedPriorityQueue
    {
        private int[] _heap;
        private int[] _positions;
        private float[] _priorities;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }

        public IndexedPriorityQueue(int idCapacity = 64, int heapCapacity = 64)
        {
            if (idCapacity < 1)
            {
                idCapacity = 1;
            }

            if (heapCapacity < 1)
            {
                heapCapacity = 1;
            }

            _heap = new int[heapCapacity];
            _positions = new int[idCapacity];
            _priorities = new float[idCapacity];
            Array.Fill(_positions, -1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int id)
        {
            return (uint)id < (uint)_positions.Length && _positions[id] >= 0;
        }

        public void PushOrDecrease(int id, float priority)
        {
            Debug.Assert(id >= 0);
            EnsureIdCapacity(id + 1);
            int pos = _positions[id];
            if (pos >= 0)
            {
                if (priority >= _priorities[id])
                {
                    return;
                }

                _priorities[id] = priority;
                SiftUp(pos);
                return;
            }
            if (_count == _heap.Length)
            {
                Array.Resize(ref _heap, _heap.Length << 1);
            }

            int index = _count++;
            _heap[index] = id;
            _positions[id] = index;
            _priorities[id] = priority;
            SiftUp(index);
        }

        public int PopMin()
        {
            Debug.Assert(_count > 0);
            int result = _heap[0];
            int last = _heap[--_count];
            _positions[result] = -1;
            if (_count > 0)
            {
                _heap[0] = last;
                _positions[last] = 0;
                SiftDown(0);
            }
            return result;
        }

        public void Clear()
        {
            for (int i = 0; i < _count; i++)
            {
                _positions[_heap[i]] = -1;
            }

            _count = 0;
        }

        private void SiftUp(int index)
        {
            int id = _heap[index];
            float priority = _priorities[id];
            while (index > 0)
            {
                int parent = (index - 1) >> 1;
                int parentId = _heap[parent];
                if (priority >= _priorities[parentId])
                {
                    break;
                }

                _heap[index] = parentId;
                _positions[parentId] = index;
                index = parent;
            }
            _heap[index] = id;
            _positions[id] = index;
        }

        private void SiftDown(int index)
        {
            int id = _heap[index];
            float priority = _priorities[id];
            int half = _count >> 1;
            while (index < half)
            {
                int child = (index << 1) + 1;
                int right = child + 1;
                int childId = _heap[child];
                if (right < _count && _priorities[_heap[right]] < _priorities[childId])
                {
                    child = right;
                    childId = _heap[right];
                }
                if (_priorities[childId] >= priority)
                {
                    break;
                }

                _heap[index] = childId;
                _positions[childId] = index;
                index = child;
            }
            _heap[index] = id;
            _positions[id] = index;
        }

        private void EnsureIdCapacity(int capacity)
        {
            if (capacity <= _positions.Length)
            {
                return;
            }

            int old = _positions.Length;
            int next = CollectionMath.NextPowerOfTwo(capacity);
            Array.Resize(ref _positions, next);
            Array.Resize(ref _priorities, next);
            for (int i = old; i < next; i++)
            {
                _positions[i] = -1;
            }
        }
    }
}
