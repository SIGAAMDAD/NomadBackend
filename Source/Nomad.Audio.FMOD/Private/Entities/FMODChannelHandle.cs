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

namespace Nomad.Audio.Fmod.Private.Entities
{
	internal readonly struct FMODChannelHandle
	{
		public readonly int Slot;
		public readonly uint Generation;

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public FMODChannelHandle( int slot, uint generation )
		{
			Slot = slot;
			Generation = generation;
		}

		public bool IsValid {
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			get => Slot >= 0 && Generation != 0;
		}
	}
};
