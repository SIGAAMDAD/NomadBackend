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
using System.Runtime.CompilerServices;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Interfaces;

namespace Nomad.Audio.Fmod.Private.ValueObjects
{
	/*
	===================================================================================

	FMODEventResource

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal readonly struct FMODEventResource : IAudioResource
	{
		private readonly FMOD.Studio.EventDescription _instance;
		public readonly bool IsValid => _instance.isValid();

		/*
		===============
		FMODEventResource
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="instance"></param>
		public FMODEventResource( FMOD.Studio.EventDescription instance )
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
		public void Dispose()
		{
			Unload();
		}

		/*
		===============
		CreateInstance
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="instance"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void CreateInstance( out FMODChannelResource instance )
		{
			FMODValidator.ValidateCall( _instance.createInstance( out var resource ) );
			instance = new FMODChannelResource( resource );
		}

		/*
		===============
		Unload
		===============
		*/
		/// <summary>
		/// Unloads and deallocates all event instances bound to this event description.
		/// </summary>
		public void Unload()
		{
			if ( _instance.isValid() ) {
				FMODValidator.ValidateCall( _instance.unloadSampleData() );
				FMODValidator.ValidateCall( _instance.releaseAllInstances() );
				_instance.clearHandle();
			}
		}

		public static implicit operator IntPtr( FMODEventResource resource ) => resource._instance.handle;
		public static implicit operator FMOD.Studio.EventDescription( FMODEventResource resource ) => resource._instance;
		public static bool operator ==( FMODEventResource a, FMODEventResource b ) => a._instance.handle == b._instance.handle;
		public static bool operator !=( FMODEventResource a, FMODEventResource b ) => a._instance.handle != b._instance.handle;
	};
};
