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

using NomadCore.GameServices;
using System;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects {
	/*
	===================================================================================
	
	FMODSystemHandle
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal readonly record struct FMODSystemHandle : IDisposable {
		public readonly FMOD.Studio.System StudioSystem { get; }
		public readonly FMOD.System System { get; }

		public FMODSystemHandle( ILoggerService logger ) {
#if FMOD_LOGGING
			var debugFlags = FMOD.DEBUG_FLAGS.LOG | FMOD.DEBUG_FLAGS.ERROR | FMOD.DEBUG_FLAGS.WARNING | FMOD.DEBUG_FLAGS.TYPE_TRACE | FMOD.DEBUG_FLAGS.DISPLAY_THREAD;
			FMODValidator.ValidateCall( FMOD.Debug.Initialize( debugFlags, FMOD.DEBUG_MODE.CALLBACK, DebugCallback ) );
#endif

			FMODValidator.ValidateCall( FMOD.Studio.System.create( out _studioSystem ) );
			if ( !_studioSystem.isValid() ) {
				_logger.PrintError( $"FMODSystemHandle: failed to create FMOD.Studio.System instance!" );
				return;
			}

			FMODValidator.ValidateCall( _studioSystem.getCoreSystem( out _system ) );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// Releases the unmanaged FMOD system handles.
		/// </summary>
		public void Dispose() {

		}

		/*
		===============
		Update
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Update() {
			StudioSystem.update();
			System.update();
		}
	};
};