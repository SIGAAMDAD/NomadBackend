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

using System;
using System.Collections.Generic;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.ValueObjects;
using Nomad.Core.Util;
using Steamworks;

namespace Nomad.Audio.Fmod.Private.Repositories {
	/*
	===================================================================================

	FMODBusRepository

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODBusRepository : IDisposable {
		public ChannelGroupHandle UIGroup => _uiChannel;
		private readonly ChannelGroupHandle _uiChannel;

		public ChannelGroupHandle MusicGroup => _musicChannel;
		private readonly ChannelGroupHandle _musicChannel;

		public ChannelGroupHandle AmbientGroup => _ambientChannel;
		private readonly ChannelGroupHandle _ambientChannel;

		private readonly Dictionary<uint, FMODChannelGroup> _categories;
		private readonly FMOD.Studio.System _system;

		/*
		===============
		FMODBusRepository
		===============
		*/
		/// <summary>
		/// Creates an FMODBusRepository.
		/// </summary>
		/// <param name="system"></param>
		public FMODBusRepository( FMOD.Studio.System system ) {
			_system = system;

			_categories = new Dictionary<uint, FMODChannelGroup>();

			CreateChannelGroup(
				new SoundCategory {
					Name = "SoundCategory:UI",
					MaxSimultaneous = 4,
					PriorityScale = 1.5f,
					StealProtectionTime = 0.2f,
					AllowStealingFromSameCategory = false
				},
				out _uiChannel
			);
			CreateChannelGroup(
				new SoundCategory {
					Name = "SoundCategory:Music",
					MaxSimultaneous = 2,
					PriorityScale = 2.0f,
					StealProtectionTime = 1.5f,
					AllowStealingFromSameCategory = false
				},
				out _musicChannel
			);
			CreateChannelGroup(
				new SoundCategory {
					Name = "SoundCategory:Ambient",
					MaxSimultaneous = 10,
					PriorityScale = 0.8f,
					StealProtectionTime = 0.8f,
					AllowStealingFromSameCategory = true
				},
				out _ambientChannel
			);
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Releases all unmanaged FMOD busses.
		/// </summary>
		public void Dispose() {
			foreach ( var category in _categories ) {
				category.Value?.Dispose();
			}
			_categories.Clear();
		}

		/*
		===============
		CreateChannelGroup
		===============
		*/
		/// <summary>
		/// Creates a new <see cref="SoundCategory"/>.
		/// </summary>
		/// <param name="category"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		public AudioResult CreateChannelGroup( SoundCategory category, out ChannelGroupHandle group ) {
			uint hash = category.Name.HashFileName();
			group = new( hash );

			if ( _categories.ContainsKey( hash ) ) {
				return AudioResult.Success;
			}

			_categories[ hash ] = new FMODChannelGroup( category, _system );
			return AudioResult.Success;
		}

		/*
		===============
		GetChannelGroup
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		public AudioResult GetChannelGroup( string name, out ChannelGroupHandle group ) {
			uint hash = name.HashFileName();

			if ( !_categories.TryGetValue( hash, out var category ) ) {
				group = new( 0 );
				return AudioResult.Error_ResourceNotFound;
			}

			group = new( hash );
			return AudioResult.Success;
		}

		/*
		===============
		GetSoundCategoryFromGroup
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="name"></param>
		/// <param name="group"></param>
		/// <returns></returns>
		public AudioResult GetSoundCategoryFromGroup( ReadOnlyMemory<char> name, out SoundCategory group ) {
			return AudioResult.Success;
		}

		/*
		===============
		SetChannelGroupVolume
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="group"></param>
		/// <param name="volume"></param>
		/// <returns></returns>
		public AudioResult SetChannelGroupVolume( ChannelGroupHandle group, float volume ) {
			if ( !_categories.TryGetValue( group, out var category ) ) {
				return AudioResult.Error_InvalidParameter;
			}

			category.Volume = volume;
			return AudioResult.Success;
		}

		/*
		===============
		SetChannelGroupPitch
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="group"></param>
		/// <param name="pitch"></param>
		/// <returns></returns>
		public AudioResult SetChannelGroupPitch( ChannelGroupHandle group, float pitch ) {
			if ( !_categories.TryGetValue( group, out var category ) ) {
				return AudioResult.Error_InvalidParameter;
			}

			category.Pitch = pitch;
			return AudioResult.Success;
		}
	};
};
