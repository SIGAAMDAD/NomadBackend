/*
===========================================================================
The Nomad MPLv2 Source Code
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System.Numerics;

namespace Nomad.Audio.Fmod.Private
{
	internal static class Vector2Extensions
	{
		public static FMOD.ATTRIBUTES_3D Make3D( this Vector2 vector )
		{
			return new FMOD.ATTRIBUTES_3D {
				position = new FMOD.VECTOR { x = vector.X, y = 0.0f, z = vector.Y },
				velocity = new FMOD.VECTOR { x = 0.0f, y = 0.0f, z = 0.0f },

				// Listener / emitter facing "out of the screen"
				forward = new FMOD.VECTOR { x = 0.0f, y = 0.0f, z = -1.0f },

				// Because screen-space Y grows downward
				up = new FMOD.VECTOR { x = 0.0f, y = 1.0f, z = 0.0f }
			};
		}
	};
};
