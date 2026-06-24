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
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Fmod.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.Audio.ValueObjects;

namespace Nomad.Audio.Fmod.Private.Entities
{
	/*
	===================================================================================
	
	FMODEmitter
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class FMODEmitter : IAudioEmitter
	{
		public Vector3 Position {
			get => _position;
			set {
				_position = value;

				if ( TryResolveCurrentHandle( out var handle ) ) {
					_channelService.TrySetPosition( handle, value );
				}
			}
		}
		private Vector3 _position = Vector3.Zero;

		public float Volume {
			get => _volume;
			set {
				_volume = value;

				if ( TryResolveCurrentHandle( out var handle ) ) {
					_channelService.TrySetVolume( handle, value );
				}
			}
		}
		private float _volume = 1.0f;

		public float Pitch {
			get => _pitch;
			set {
				_pitch = value;

				if ( TryResolveCurrentHandle( out var handle ) ) {
					_channelService.TrySetPitch( handle, value );
				}
			}
		}
		private float _pitch = 1.0f;

		public string Category => _category.Config.Name;

		private readonly SoundCategory _category;
		private readonly FMODChannelService _channelService;

		private FMODChannelHandle? _currentHandle;

		public ChannelStatus Status {
			get {
				if ( !TryResolveCurrentHandle( out var handle ) ) {
					return ChannelStatus.Stopped;
				}

				return _channelService.IsPlaying( handle )
					? ChannelStatus.Playing
					: ChannelStatus.Stopped;
			}
		}

		public FMODEmitter( FMODChannelService channelService, SoundCategory category )
		{
			_channelService = channelService;
			_category = category;
		}

		public void PlaySound( string id, float priority = 0.5f )
		{
			// We do not need to unhook events anymore because handles are value types.
			_currentHandle = null;

			FMODChannelHandle? handle = _channelService.AllocateChannel(
				id,
				_position,
				_category,
				priority );

			if ( handle == null ) {
				return;
			}

			_currentHandle = handle.Value;
			ApplyCachedState();
		}

		public void Stop()
		{
			if ( TryResolveCurrentHandle( out var handle ) ) {
				_channelService.TryStopChannel( handle, false );
			}

			_currentHandle = null;
		}

		private void ApplyCachedState()
		{
			if ( !TryResolveCurrentHandle( out var handle ) ) {
				return;
			}

			_channelService.TrySetPosition( handle, _position );
			_channelService.TrySetVolume( handle, _volume );
			_channelService.TrySetPitch( handle, _pitch );
		}

		private bool TryResolveCurrentHandle( out FMODChannelHandle handle )
		{
			if ( _currentHandle is FMODChannelHandle existing && _channelService.IsAlive( existing ) ) {
				handle = existing;
				return true;
			}

			_currentHandle = null;
			handle = default;
			return false;
		}
	};
};
