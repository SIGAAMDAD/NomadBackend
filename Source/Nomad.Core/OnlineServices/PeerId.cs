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
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Nomad.Core.OnlineServices
{
    /// <summary>
    ///
    /// </summary>
    public readonly struct PeerId : IEquatable<PeerId>
    {
        public static readonly PeerId Invalid = new PeerId(Guid.Empty);

        public readonly Guid Id;

        public bool IsValid => Id != Guid.Empty;

        /// <summary>
        ///
        /// </summary>
        /// <param name="id"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PeerId(Guid id)
        {
            Id = id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(PeerId other)
        {
            return Id == other.Id;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return obj is PeerId other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString()
        {
            return Id.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(PeerId left, PeerId right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(PeerId left, PeerId right)
        {
            return !left.Equals(right);
        }
    }
}
