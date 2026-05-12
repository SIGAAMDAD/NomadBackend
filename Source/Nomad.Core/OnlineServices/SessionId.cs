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

namespace Nomad.Core.OnlineServices
{
    public readonly struct SessionId : IEquatable<SessionId>
    {
        public static readonly SessionId Empty = new SessionId(Guid.Empty);

        public readonly Guid Value;

        public bool IsEmpty => Value == Guid.Empty;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SessionId(Guid value)
        {
            Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(SessionId other)
        {
            return Value.Equals(other.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj)
        {
            return obj is SessionId other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString()
        {
            return Value.ToString("N");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(SessionId left, SessionId right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(SessionId left, SessionId right)
        {
            return !left.Equals(right);
        }
    }
}
