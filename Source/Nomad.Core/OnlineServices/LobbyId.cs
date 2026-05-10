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
    public readonly struct LobbyId : IEquatable<LobbyId>
    {
        public static readonly LobbyId Empty = new LobbyId(Guid.Empty);
        public static readonly LobbyId Invalid = Empty;

        public readonly Guid Value;

        public bool IsEmpty { get { return Value == Guid.Empty; } }

        public LobbyId(Guid value)
        {
            Value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(LobbyId other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object? obj)
        {
            return obj is LobbyId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString("N");
        }

        public static bool operator ==(LobbyId left, LobbyId right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(LobbyId left, LobbyId right)
        {
            return !left.Equals(right);
        }
    }
}
