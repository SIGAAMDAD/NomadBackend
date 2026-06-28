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
    public sealed class StringIdTable
    {
        private readonly Dictionary<string, int> _ids;
        private string[] _strings;
        private int _count;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _count; } }

        public StringIdTable(int initialCapacity = 256, StringComparer? comparer = null)
        {
            if (initialCapacity < 1)
            {
                initialCapacity = 1;
            }

            _ids = new Dictionary<string, int>(initialCapacity, comparer ?? StringComparer.Ordinal);
            _strings = new string[initialCapacity];
        }

        public int GetOrAdd(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            int id;
            if (_ids.TryGetValue(value, out id))
            {
                return id;
            }

            id = _count++;
            if (id == _strings.Length)
            {
                Array.Resize(ref _strings, _strings.Length << 1);
            }

            _strings[id] = value;
            _ids.Add(value, id);
            return id;
        }

        public bool TryGetId(string value, out int id)
        {
            return _ids.TryGetValue(value, out id);
        }


        public string[] ToArray()
        {
            string[] result = new string[_count];
            Array.Copy(_strings, 0, result, 0, _count);
            return result;
        }

        public string GetString(int id)
        {
            return _strings[id];
        }
    }
}
