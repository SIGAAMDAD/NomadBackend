/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Godot;
using System;
using FMOD.Studio;
using Nomad.Audio.ValueObjects;
using Nomad.Audio.Fmod.Entities;

namespace Nomad.Audio.Fmod.Private.Entities {
	/*
	===================================================================================

	FMODChannel

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODChannel : IDisposable {
		public FMODChannelResource Instance;
		public Vector2 Position;
		public float BasePriority;
		public float CurrentPriority;
		public float StartTime;
		public int ChannelId;
		public ChannelGroupHandle Category;
		public float LastStolenTime = 0.0f;

		public ChannelGroupHandle Group;

		public float Volume {
			get => _volume;
			set {
				if ( _volume == value ) {
					return;
				}
				_volume = value;
				Instance.Volume = value;
			}
		}
		private float _volume = 1.0f;

		public float Pitch {
			get => _pitch;
			set {
				if ( _pitch == value ) {
					return;
				}
				_pitch = value;
				Instance.Pitch = value;
			}
		}
		private float _pitch = 1.0f;

		public int PlayCount = 0;
		public bool IsEssential = false;

		public float Age => Time.GetTicksMsec() / 1000.0f - StartTime;
		public bool IsPlaying => Instance.IsPlaying;

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			Instance.Dispose();
		}
	};
};
