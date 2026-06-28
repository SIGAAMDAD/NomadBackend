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
using Nomad.Audio.Interfaces;

namespace Nomad.Audio.Fmod.Private.Entities
{
	/*
	===================================================================================
	
	FMODAudioGroup
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class FMODAudioGroup : IAudioGroup
	{
		public string Name => _name;
		private readonly string _name;

		public float Volume {
			get => _volume;
			set {
				_volume = Math.Clamp( value, 0.0f, 100.0f ) * 0.01f;
				_bus.setVolume( _volume );
			}
		}
		private float _volume;

		public bool Muted {
			get => _muted;
			set {
				_muted = value;
				_bus.setMute( value );
			}
		}
		private bool _muted;

		private readonly FMOD.Studio.Bus _bus;

		/*
		===============
		FMODAudioGroup
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bus"></param>
		/// <param name="name"></param>
		public FMODAudioGroup( FMOD.Studio.Bus bus, string name )
		{
			_bus = bus;
			_name = name;

			FMODValidator.ValidateCall( bus.getVolume( out _volume ) );
			FMODValidator.ValidateCall( bus.getMute( out _muted ) );
		}
	};
};
