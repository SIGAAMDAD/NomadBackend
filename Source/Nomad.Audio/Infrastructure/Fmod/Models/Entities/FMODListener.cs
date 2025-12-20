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

using NomadCore.Systems.Audio.Domain.Interfaces;
using Godot;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Models.Entities {
	/*
	===================================================================================
	
	FMODListener
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class FMODListener( FMOD.Studio.System system, int index ) : IListener {
		public Vector2 Position {
			get => _position;
			set {
				if ( _position == value ) {
					return;
				}
				FMOD.ATTRIBUTES_3D attributes = new FMOD.ATTRIBUTES_3D {
					position = new FMOD.VECTOR { x = _position.X, y = _position.Y, z = 0.0f }
				};
				_system.setListenerAttributes( _listenerIndex, attributes );
			}
		}
		private Vector2 _position = Vector2.Zero;

		private readonly int _listenerIndex = index;
		private readonly FMOD.Studio.System _system = system;
	};
};