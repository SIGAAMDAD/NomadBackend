/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;

namespace Nomad.Core.Util
{
    public static class StringExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="seed"></param>
        /// <returns></returns>
        public static uint HashFileName(this string filename, int seed = 0)
        {
            ArgumentException.ThrowIfNullOrEmpty(filename);

            int length = filename.Length;
            Span<byte> hash = stackalloc byte[length];
            for (int i = 0; i < length; i++)
            {
                char c = filename[i];
                hash[i] = (byte)(c >= 'A' && c <= 'Z' ? c + 32 : c);
            }

            return System.IO.Hashing.XxHash32.HashToUInt32(hash, seed);
        }
    }
}
