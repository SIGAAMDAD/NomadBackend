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
	
	GodotAudioEffectCompressor
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class GodotAudioEffectCompressor( IAudioBus owner ) : GodotAudioEffect<AudioEffectCompressor>( owner ), ICompressorEffect {
		public float Threshold {
			get => _effect.Threshold;
			set => _effect.Threshold = value;
		}
		public float Ratio {
			get => _effect.Ratio;
			set => _effect.Ratio = value;
		}
		public float Gain {
			get => _effect.Gain;
			set => _effect.Gain = value;
		}
		public float AttackUs {
			get => _effect.AttackUs;
			set => _effect.AttackUs = value;
		}
		public float ReleaseMs {
			get => _effect.ReleaseMs;
			set => _effect.ReleaseMs = value;
		}
		public float Mix {
			get => _effect.Mix;
			set => _effect.Mix = value;
		}
		public StringName SideChain {
			get => _effect.Sidechain;
			set => _effect.Sidechain = value;
		}
	};
};