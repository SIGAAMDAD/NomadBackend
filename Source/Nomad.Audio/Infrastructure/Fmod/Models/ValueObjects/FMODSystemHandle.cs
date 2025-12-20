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

using NomadCore.Domain;
using NomadCore.Domain.Exceptions;
using NomadCore.GameServices;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects {
	/*
	===================================================================================
	
	FMODSystemHandle
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class FMODSystemHandle : IDisposable {
		public FMOD.Studio.System StudioSystem {
			get {
				lock ( _systemLock ) {
					return _studioSystem;
				}
			}
		}
		private readonly FMOD.Studio.System _studioSystem;

		public FMOD.System System {
			get {
				lock ( _systemLock ) {
					return _system;
				}
			}
		}
		private readonly FMOD.System _system;

		private readonly ILoggerService _logger;

		private readonly object _systemLock = new object();
		private readonly StringBuilder _fmodDebugString = new StringBuilder( 256 );

		/*
		===============
		FMODSystemHandle
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cvarSystem"></param>
		/// <param name="logger"></param>
		/// <exception cref="CVarMissing"></exception>
		public FMODSystemHandle( ICVarSystemService cvarSystem, ILoggerService logger ) {
			_logger = logger;

			var fmodLogging = cvarSystem.GetCVar<bool>( Constants.CVars.Audio.FMOD.LOGGING ) ?? throw new CVarMissing( Constants.CVars.Audio.FMOD.LOGGING );
			if ( fmodLogging.Value ) {
				var debugFlags = FMOD.DEBUG_FLAGS.LOG | FMOD.DEBUG_FLAGS.ERROR | FMOD.DEBUG_FLAGS.WARNING | FMOD.DEBUG_FLAGS.TYPE_TRACE | FMOD.DEBUG_FLAGS.DISPLAY_THREAD;
				FMODValidator.ValidateCall( FMOD.Debug.Initialize( debugFlags, FMOD.DEBUG_MODE.CALLBACK, DebugCallback ) );
			}

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
			if ( _studioSystem.isValid() ) {
				FMODValidator.ValidateCall( _system.close() );
				FMODValidator.ValidateCall( _system.release() );
				_system.clearHandle();
				
				FMODValidator.ValidateCall( _studioSystem.unloadAll() );
				FMODValidator.ValidateCall( _studioSystem.release() );
				_studioSystem.clearHandle();
			}
		}

		/*
		===============
		Update
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Update() {
#if DEBUG
			FMODValidator.ValidateCall( StudioSystem.update() );
			FMODValidator.ValidateCall( System.update() );
#else
			StudioSystem.update();
			System.update();
#endif
		}

		/*
		===============
		DebugCallback
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="flags"></param>
		/// <param name="file"></param>
		/// <param name="line"></param>
		/// <param name="func"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		private FMOD.RESULT DebugCallback( FMOD.DEBUG_FLAGS flags, nint file, int line, nint func, nint message ) {
			string formattedMessage = Marshal.PtrToStringAnsi( message );
			string formattedFile = Marshal.PtrToStringAnsi( file );
			string formattedFunc = Marshal.PtrToStringAnsi( func );

			_fmodDebugString.Clear();
			_fmodDebugString.Append( $"[FMOD {formattedFile}:{line}, {formattedFunc}] {formattedMessage}" );

			// remove the '\n' escape
			_fmodDebugString.Length--;

			if ( ( flags & FMOD.DEBUG_FLAGS.LOG ) != 0 ) {
				_logger.PrintLine( _fmodDebugString.ToString() );
			} else if ( ( flags & FMOD.DEBUG_FLAGS.WARNING ) != 0 ) {
				_logger.PrintWarning( _fmodDebugString.ToString()  );
			} else if ( ( flags & FMOD.DEBUG_FLAGS.ERROR ) != 0 ) {
				_logger.PrintError( _fmodDebugString.ToString()  );
			}
			return FMOD.RESULT.OK;
		}
	};
};