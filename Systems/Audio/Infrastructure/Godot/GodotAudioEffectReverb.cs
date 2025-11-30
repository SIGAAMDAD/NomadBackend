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
using NomadCore.Systems.Audio.Interfaces.Effects;

namespace NomadCore.Systems.Audio.Infrastructure.Godot {
	/*
	===================================================================================
	
	GodotAudioEffectReverb
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class GodotAudioEffectReverb( IAudioBus owner ) : GodotAudioEffect<AudioEffectReverb>( owner ), IReverbEffect {
		public float RoomSize {
			get => _effect.RoomSize;
			set => _effect.RoomSize = value;
		}
		public float Damping {
			get => _effect.Damping;
			set => _effect.Damping = value;
		}
		public float Spread {
			get => _effect.Spread;
			set => _effect.Spread = value;
		}
		public float HighPass {
			get => _effect.Hipass;
			set => _effect.Hipass = value;
		}
		public float Dry {
			get => _effect.Dry;
			set => _effect.Dry = value;
		}
		public float Wet {
			get => _effect.Wet;
			set => _effect.Wet = value;
		}
	};
};