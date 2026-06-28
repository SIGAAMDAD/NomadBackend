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
    public sealed class DirtySet
    {
        private readonly SparseSet _set;

        public int Count { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _set.Count; } }
        public System.Span<int> DirtyIds { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return _set.DenseIds; } }

        public DirtySet(int idCapacity = 64, int dirtyCapacity = 64)
        {
            _set = new SparseSet(idCapacity, dirtyCapacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsDirty(int id)
        {
            return _set.Contains(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MarkDirty(int id)
        {
            return _set.Add(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MarkClean(int id)
        {
            return _set.Remove(id);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetDirtyId(int index)
        {
            return _set.GetDenseId(index);
        }

        public void ClearDirty()
        {
            _set.Clear();
        }
    }
}
