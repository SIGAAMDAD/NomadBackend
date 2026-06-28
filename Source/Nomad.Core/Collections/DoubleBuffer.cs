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

using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class DoubleBuffer<T>
    {
        public T Current;
        public T Next;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DoubleBuffer(T current, T next)
        {
            Current = current;
            Next = next;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Swap()
        {
            T temp = Current;
            Current = Next;
            Next = temp;
        }
    }
}
