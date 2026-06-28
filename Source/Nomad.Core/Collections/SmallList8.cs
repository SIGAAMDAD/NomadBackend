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
    public sealed class SmallList8<T>
    {
        private int _count;
        private T _item0;
        private T _item1;
        private T _item2;
        private T _item3;
        private T _item4;
        private T _item5;
        private T _item6;
        private T _item7;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }
        public int Capacity { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return 8; } }

        public ref T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Debug.Assert((uint)index < (uint)_count);
                switch (index)
                {
                case 0: return ref _item0;
                case 1: return ref _item1;
                case 2: return ref _item2;
                case 3: return ref _item3;
                case 4: return ref _item4;
                case 5: return ref _item5;
                case 6: return ref _item6;
                case 7: return ref _item7;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryAdd(T value)
        {
            if (_count >= 8)
            {
                return false;
            }

            this[_count++] = value;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Clear()
        {
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                for (int i = 0; i < _count; i++)
                {
                    this[i] = default!;
                }
            }
            _count = 0;
        }
    }
}
