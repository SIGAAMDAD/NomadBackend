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
using System.Runtime.InteropServices;

namespace Nomad.Audio.Fmod.Private.Entities
{
	/*
	===================================================================================
	
	FMODChannelResource
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	[StructLayout( LayoutKind.Sequential, Pack = 1, Size = 8 )]
	internal struct FMODChannelResource
	{
		public readonly FMOD.Studio.PLAYBACK_STATE PlaybackState {
			get {
				FMODValidator.ValidateCall( _instance.getPlaybackState( out FMOD.Studio.PLAYBACK_STATE state ) );
				return state;
			}
		}
		public readonly uint ListenerMask {
			get {
				FMODValidator.ValidateCall( _instance.getListenerMask( out uint mask ) );
				return mask;
			}
			set {
				if ( !_instance.isValid() ) {
					return;
				}
				FMODValidator.ValidateCall( _instance.setListenerMask( value ) );
			}
		}
		public Vector3 Position {
			get {
				FMODValidator.ValidateCall( _instance.get3DAttributes( out FMOD.ATTRIBUTES_3D attributes ) );
				return new Vector3( attributes.position.x, attributes.position.y, attributes.position.z );
			}
			set {
				if ( !_instance.isValid() ) {
					return;
				}
				FMODValidator.ValidateCall( _instance.set3DAttributes( value.Make3D() ) );
			}
		}
		public readonly float Volume {
			get {
				FMODValidator.ValidateCall( _instance.getVolume( out float volume ) );
				return volume;
			}
			set {
				if ( !_instance.isValid() ) {
					return;
				}
				FMODValidator.ValidateCall( _instance.setVolume( value ) );
			}
		}
		public readonly float Pitch {
			get {
				FMODValidator.ValidateCall( _instance.getPitch( out float pitch ) );
				return pitch;
			}
			set {
				FMODValidator.ValidateCall( _instance.setPitch( value ) );
			}
		}

		public readonly FMOD.Studio.MEMORY_USAGE MemoryUsage {
			get {
				FMODValidator.ValidateCall( _instance.getMemoryUsage( out FMOD.Studio.MEMORY_USAGE memoryUsage ) );
				return memoryUsage;
			}
		}
		public readonly FMOD.Studio.EventDescription Description {
			get {
				FMODValidator.ValidateCall( _instance.getDescription( out FMOD.Studio.EventDescription description ) );
				return description;
			}
		}
		public readonly bool IsPlaying => PlaybackState == FMOD.Studio.PLAYBACK_STATE.PLAYING;
		public readonly bool IsValid => _instance.isValid();

		private readonly FMOD.Studio.EventInstance _instance;

		/*
		===============
		FMODChannelResource
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="instance"></param>
		public FMODChannelResource( FMOD.Studio.EventInstance instance )
		{
			_instance = instance;
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public readonly void Dispose()
		{
			Unload();
		}

		/*
		===============
		Stop
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="stopMode"></param>
		/// <returns></returns>
		public readonly FMOD.RESULT Stop( FMOD.Studio.STOP_MODE stopMode )
		{
			return _instance.stop( stopMode );
		}

		/*
		===============
		Start
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public readonly FMOD.RESULT Start()
		{
			return _instance.start();
		}

		/*
		===============
		SetFinishedCallback
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="callback"></param>
		/// <returns></returns>
		public readonly FMOD.RESULT SetFinishedCallback( FMOD.Studio.EVENT_CALLBACK? callback )
		{
			return _instance.setCallback( callback, FMOD.Studio.EVENT_CALLBACK_TYPE.STOPPED | FMOD.Studio.EVENT_CALLBACK_TYPE.START_FAILED );
		}

		/*
		===============
		Unload
		===============
		*/
		/// <summary>
		/// Clears the unmanaged FMOD EventInstance.
		/// </summary>
		public readonly void Unload()
		{
			if ( _instance.isValid() ) {
				// ensure we unhook the callback (causes a seggy if its not done)
				FMODValidator.ValidateCall( SetFinishedCallback( null ) );

				FMODValidator.ValidateCall( _instance.stop( FMOD.Studio.STOP_MODE.IMMEDIATE ) );
				FMODValidator.ValidateCall( _instance.release() );
				_instance.clearHandle();
			}
		}

		public static implicit operator IntPtr( FMODChannelResource resource ) => resource._instance.handle;
		public static implicit operator FMOD.Studio.EventInstance( FMODChannelResource resource ) => resource._instance;
		public static bool operator ==( FMODChannelResource resource, FMOD.Studio.EventInstance eventInstance ) => resource._instance.handle == eventInstance.handle;
		public static bool operator !=( FMODChannelResource resource, FMOD.Studio.EventInstance eventInstance ) => resource._instance.handle != eventInstance.handle;
	};
};
