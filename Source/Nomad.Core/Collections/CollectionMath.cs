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
    internal static class CollectionMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Min(int a, int b)
        {
            return a < b ? a : b;
        }

        public static int NextPowerOfTwo(int value)
        {
            if (value <= 1)
            {
                return 1;
            }

            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            value++;
            return value > 0 ? value : 1 << 30;
        }

        public static void ThrowArgumentOutOfRange(string name)
        {
            throw new ArgumentOutOfRangeException(name);
        }
    }
}
