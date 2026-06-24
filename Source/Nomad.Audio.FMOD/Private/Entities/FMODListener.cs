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
using Nomad.Audio.Interfaces;

namespace Nomad.Audio.Fmod.Private.Entities
{
	/*
	===================================================================================

	FMODListener

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODListener : IListener
	{
		public Vector3 Position {
			get => _position;
			set {
				if ( _position == value ) {
					return;
				}
				_position = value;
				_system.setListenerAttributes( _listenerIndex, value.Make3D() );
			}
		}
		private Vector3 _position = Vector3.Zero;

		private readonly int _listenerIndex;
		private readonly FMOD.Studio.System _system;

		public FMODListener( FMOD.Studio.System system, int index )
		{
			_listenerIndex = index;
			_system = system;
		}
	};
};
