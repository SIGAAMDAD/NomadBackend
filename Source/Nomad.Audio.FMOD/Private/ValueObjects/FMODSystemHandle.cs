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

using Nomad.Core.Exceptions;
using Nomad.Core.Logger;
using Nomad.Core;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Nomad.Core.CVars;
using Nomad.CVars;

namespace Nomad.Audio.Fmod.Private.ValueObjects
{
	/*
	===================================================================================

	FMODSystemHandle

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODSystemHandle : IDisposable
	{
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
		private readonly object _systemLock = new object();

		private static ILoggerService _logger;
		private static readonly StringBuilder _fmodDebugString = new StringBuilder( 256 );

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
		public FMODSystemHandle( ICVarSystemService cvarSystem, ILoggerService logger )
		{
			_logger = logger;

			var fmodLogging = cvarSystem.GetCVarOrThrow<bool>( Constants.CVars.EngineUtils.Audio.FMOD.LOGGING );
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
		public void Dispose()
		{
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
		public void Update()
		{
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
		private static FMOD.RESULT DebugCallback( FMOD.DEBUG_FLAGS flags, nint file, int line, nint func, nint message )
		{
			string formattedMessage = Marshal.PtrToStringAnsi( message );
			string formattedFile = Marshal.PtrToStringAnsi( file );
			string formattedFunc = Marshal.PtrToStringAnsi( func );

			_fmodDebugString.Clear();
			_fmodDebugString.Append( $"[FMOD {formattedFile}:{line}, {formattedFunc}] {formattedMessage}" );

			// remove the '\n' escape
			_fmodDebugString.Length--;

			if ( (flags & FMOD.DEBUG_FLAGS.LOG) != 0 ) {
				_logger.PrintLine( _fmodDebugString.ToString() );
			} else if ( (flags & FMOD.DEBUG_FLAGS.WARNING) != 0 ) {
				_logger.PrintWarning( _fmodDebugString.ToString() );
			} else if ( (flags & FMOD.DEBUG_FLAGS.ERROR) != 0 ) {
				_logger.PrintError( _fmodDebugString.ToString() );
			}
			return FMOD.RESULT.OK;
		}
	};
};
