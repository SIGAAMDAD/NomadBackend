/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using Godot;
using NomadCore.Systems.Audio.Interfaces;
using System;

namespace NomadCore.Systems.Audio.Infrastructure.Godot {
	/*
	===================================================================================
	
	GodotAudioEffect
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public class GodotAudioEffect<T> : IAudioEffect where T : AudioEffect, new() {
		public bool Active {
			get => AudioServer.IsBusEffectEnabled( _owner.BusIndex, _effectIndex );
			set => AudioServer.SetBusEffectEnabled( _owner.BusIndex, _effectIndex, value );
		}

		protected readonly T _effect;

		private readonly IAudioBus _owner;
		private readonly int _effectIndex;

		/*
		===============
		GodotAudioEffect
		===============
		*/
		public GodotAudioEffect( IAudioBus owner ) {
			_owner = owner;
			_effect = new T();

			_effectIndex = AudioServer.GetBusEffectCount( _owner.BusIndex );
			AudioServer.AddBusEffect( _owner.BusIndex, _effect );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			AudioServer.RemoveBusEffect( _owner.BusIndex, _effectIndex );
			GC.SuppressFinalize( this );
		}
	};
};