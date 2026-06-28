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
    public sealed class SoA4<T0, T1, T2, T3>
    {
        private T0[] _a;
        private T1[] _b;
        private T2[] _c;
        private T3[] _d;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }
        public Span<T0> A { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _a.AsSpan(0, _count); } }
        public Span<T1> B { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _b.AsSpan(0, _count); } }
        public Span<T2> C { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _c.AsSpan(0, _count); } }
        public Span<T3> D { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _d.AsSpan(0, _count); } }

        public SoA4(int initialCapacity = 64)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _a = new T0[initialCapacity];
            _b = new T1[initialCapacity];
            _c = new T2[initialCapacity];
            _d = new T3[initialCapacity];
        }

        public int Add(T0 a, T1 b, T2 c, T3 d)
        {
            if (_count == _a.Length)
            {
                Grow();
            }

            int index = _count++;
            _a[index] = a;
            _b[index] = b;
            _c[index] = c;
            _d[index] = d;
            return index;
        }

        private void Grow()
        {
            int next = _a.Length << 1;
            Array.Resize(ref _a, next);
            Array.Resize(ref _b, next);
            Array.Resize(ref _c, next);
            Array.Resize(ref _d, next);
        }
    }
}
