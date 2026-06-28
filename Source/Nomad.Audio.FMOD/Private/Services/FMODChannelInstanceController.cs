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
using System.Numerics;
using System.Runtime.CompilerServices;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.ResourceCache;

namespace Nomad.Audio.Fmod.Private.Services
{
	/// <summary>
	/// Owns FMOD EventInstance creation and primitive mutations so channel policy code does
	/// not directly mix native FMOD calls with dense-array bookkeeping.
	/// </summary>
	internal sealed class FMODChannelInstanceController
	{
		private readonly IResourceCacheService<IAudioResource, string> _eventRepository;

		public FMODChannelInstanceController( IResourceCacheService<IAudioResource, string> eventRepository )
		{
			_eventRepository = eventRepository;
		}

		public nint CreateStartedInstance(
			string eventName,
			Vector3 position,
			FMOD.Studio.EVENT_CALLBACK finishedCallback )
		{
			FMODEventResource resource = GetEventResource( eventName );
			resource.CreateInstance( out var instance );
			instance.Position = position;
			FMODValidator.ValidateCall( instance.SetFinishedCallback( finishedCallback ) );
			FMODValidator.ValidateCall( instance.Start() );
			return (nint)instance;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static bool IsPlaying( nint instanceHandle )
		{
			var instance = new FMOD.Studio.EventInstance( instanceHandle );
			if ( !instance.isValid() ) {
				return false;
			}

			FMOD.RESULT result = instance.getPlaybackState( out var state );
			return result == FMOD.RESULT.OK && state == FMOD.Studio.PLAYBACK_STATE.PLAYING;
		}

		public static void StopAndRelease( nint instanceHandle )
		{
			var instance = new FMOD.Studio.EventInstance( instanceHandle );
			if ( !instance.isValid() ) {
				return;
			}

			instance.setCallback( null );
			instance.stop( FMOD.Studio.STOP_MODE.IMMEDIATE );
			instance.release();
			instance.clearHandle();
		}

		public static void SetPosition( nint instanceHandle, Vector3 position )
		{
			var instance = new FMOD.Studio.EventInstance( instanceHandle );
			if ( instance.isValid() ) {
				instance.set3DAttributes( position.Make3D() );
			}
		}

		public static void SetVolume( nint instanceHandle, float volume )
		{
			var instance = new FMOD.Studio.EventInstance( instanceHandle );
			if ( instance.isValid() ) {
				instance.setVolume( volume );
			}
		}

		public static void SetPitch( nint instanceHandle, float pitch )
		{
			var instance = new FMOD.Studio.EventInstance( instanceHandle );
			if ( instance.isValid() ) {
				instance.setPitch( pitch );
			}
		}

		private FMODEventResource GetEventResource( string eventName )
		{
			var cached = _eventRepository.GetCached( eventName )
				?? throw new InvalidOperationException( $"Couldn't find event description for '{eventName}'." );
			cached.Get( out var description );

			if ( description is not FMODEventResource eventResource ) {
				throw new InvalidCastException( $"Cached audio resource '{eventName}' is not an FMOD event resource." );
			}

			return eventResource;
		}
	}
}
