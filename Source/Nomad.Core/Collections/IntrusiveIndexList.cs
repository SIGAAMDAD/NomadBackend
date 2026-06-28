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

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class IntrusiveIndexList
    {
        private int[] _next;
        private int[] _prev;
        private int _head;
        private int _tail;
        private int _count;

        public int Head { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _head; } }
        public int Tail { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _tail; } }
        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }

        public IntrusiveIndexList(int capacity)
        {
            _next = new int[capacity];
            _prev = new int[capacity];
            Clear();
        }

        public void AddLast(int id)
        {
            EnsureCapacity(id + 1);
            _next[id] = -1;
            _prev[id] = _tail;
            if (_tail >= 0)
            {
                _next[_tail] = id;
            }
            else
            {
                _head = id;
            }

            _tail = id;
            _count++;
        }

        public void Remove(int id)
        {
            int prev = _prev[id];
            int next = _next[id];
            if (prev >= 0)
            {
                _next[prev] = next;
            }
            else
            {
                _head = next;
            }

            if (next >= 0)
            {
                _prev[next] = prev;
            }
            else
            {
                _tail = prev;
            }

            _next[id] = -1;
            _prev[id] = -1;
            _count--;
        }

        public int Next(int id) { return _next[id]; }
        public int Previous(int id) { return _prev[id]; }

        public void Clear()
        {
            for (int i = 0; i < _next.Length; i++) { _next[i] = -1; _prev[i] = -1; }
            _head = -1;
            _tail = -1;
            _count = 0;
        }

        private void EnsureCapacity(int capacity)
        {
            if (capacity <= _next.Length)
            {
                return;
            }

            int old = _next.Length;
            System.Array.Resize(ref _next, CollectionMath.NextPowerOfTwo(capacity));
            System.Array.Resize(ref _prev, _next.Length);
            for (int i = old; i < _next.Length; i++) { _next[i] = -1; _prev[i] = -1; }
        }
    }
}
