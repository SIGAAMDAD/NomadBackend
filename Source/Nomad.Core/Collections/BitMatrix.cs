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
    public sealed class BitMatrix
    {
        private readonly int _width;
        private readonly int _height;
        private readonly ulong[] _words;

        public int Width { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _width; } }
        public int Height { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _height; } }

        public BitMatrix(int width, int height)
        {
            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            _width = width;
            _height = height;
            _words = new ulong[((width * height) + 63) >> 6];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int x, int y)
        {
            Debug.Assert((uint)x < (uint)_width && (uint)y < (uint)_height);
            int bit = y * _width + x;
            return (_words[bit >> 6] & (1UL << (bit & 63))) != 0UL;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int x, int y, bool value)
        {
            Debug.Assert((uint)x < (uint)_width && (uint)y < (uint)_height);
            int bit = y * _width + x;
            ulong mask = 1UL << (bit & 63);
            if (value)
            {
                _words[bit >> 6] |= mask;
            }
            else
            {
                _words[bit >> 6] &= ~mask;
            }
        }

        public void Clear()
        {
            Array.Clear(_words, 0, _words.Length);
        }
    }
}
