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

using System.Numerics;

namespace Nomad.Audio.Fmod.Private
{
	internal static class Vector3Extensions
	{
		public static FMOD.ATTRIBUTES_3D Make3D( this Vector3 vector )
		{
			return new FMOD.ATTRIBUTES_3D {
				position = new FMOD.VECTOR { x = vector.X, y = vector.Y, z = vector.Z },
				velocity = new FMOD.VECTOR { x = 0.0f, y = 0.0f, z = 0.0f },
				forward = new FMOD.VECTOR { x = 0.0f, y = 0.0f, z = -1.0f },
				up = new FMOD.VECTOR { x = 0.0f, y = 1.0f, z = 0.0f }
			};
		}
	};
};
