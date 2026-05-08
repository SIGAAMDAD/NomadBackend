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
using Nomad.Audio.Fmod.ValueObjects;

namespace Nomad.Audio.Fmod.Private.Entities
{
	/*
	===================================================================================

	FMODChannel

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODChannel : IDisposable
	{
		public FMODChannelResource Instance = new FMODChannelResource();

		public float BasePriority = 0.0f;
		public float CurrentPriority = 0.0f;
		public float StartTimeSeconds = 0.0f;
		public float LastStolenTime = 0.0f;

		public int Generation;
		public int ChannelId;

		public string EventId = string.Empty;

		public IntPtr Id;
		public SoundCategory? Category;
		public FMODChannelHandle? Handle;

		public int PlayCount = 0;
		public bool IsEssential = false;

		public bool IsPlaying => Instance.IsPlaying;

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

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			Instance.Dispose();
		}

		/*
		===============
		AgeSeconds
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="now"></param>
		/// <returns></returns>
		public float AgeSeconds( float now )
		{
			return now - StartTimeSeconds;
		}

		/*
		===============
		Reset
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Reset()
		{
			Instance = new FMODChannelResource();

			BasePriority = 0.0f;
			CurrentPriority = 0.0f;
			StartTimeSeconds = 0.0f;
			LastStolenTime = 0.0f;

			ChannelId = -1;
			Generation = 0;

			EventId = string.Empty;
			Category = null;
			Handle = null;

			IsEssential = false;
			PlayCount = 0;

			_volume = 1.0f;
			_pitch = 1.0f;
		}
	};
};
