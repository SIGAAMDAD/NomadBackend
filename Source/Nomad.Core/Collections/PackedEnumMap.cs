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
using System.Runtime.CompilerServices;

namespace Nomad.Core.Collections
{
    public sealed class PackedEnumMap<TEnum, TValue> where TEnum : struct, Enum
    {
        private readonly TValue[] _values;
        private readonly ulong[] _occupied;

        public PackedEnumMap()
        {
            Array enumValues = Enum.GetValues(typeof(TEnum));
            int max = 0;
            for (int i = 0; i < enumValues.Length; i++)
            {
                int value = Convert.ToInt32(enumValues.GetValue(i)!);
                if (value > max)
                {
                    max = value;
                }
            }
            _values = new TValue[max + 1];
            _occupied = new ulong[((max + 1) + 63) >> 6];
        }

        public ref TValue this[TEnum key]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return ref _values[Convert.ToInt32(key)]; }
        }

        public void Set(TEnum key, TValue value)
        {
            int id = Convert.ToInt32(key);
            _values[id] = value;
            _occupied[id >> 6] |= 1UL << (id & 63);
        }

        public bool Contains(TEnum key)
        {
            int id = Convert.ToInt32(key);
            return (_occupied[id >> 6] & (1UL << (id & 63))) != 0UL;
        }
    }
}
