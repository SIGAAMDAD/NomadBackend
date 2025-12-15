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

#define FMOD_LOGGING

using NomadCore.GameServices;
using NomadCore.Infrastructure.ServiceRegistry.Interfaces;
using NomadCore.Systems.Audio.Application.Interfaces;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Registries;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Services {
	/*
	===================================================================================
	
	FMODSystemService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class FMODSystemService : IAudioSystemService {
		public string AudioDriver {
			get => _drivers[ _audioDriver ];
			set {
				if ( _drivers[ _audioDriver ].Equals( value ) ) {
					return;
				}
				SetAudioDriver( value );
			}
		}
		private int _audioDriver;

		public string OutputDevice {
			get => "Unknown";
			set => _outputDevice = value;
		}
		private string _outputDevice;

		public bool CanSetAudioDriver => true;

		private readonly List<string> _drivers = new List<string>();

		public FMOD.Studio.System StudioSystem => _studioSystem;
		private FMOD.Studio.System _studioSystem;

		public FMOD.System System => _system;
		private FMOD.System _system;

		private readonly ILoggerService _logger;

		public IResourceCacheService<FMODEventId> EventRepository => _eventRepository;
		private readonly IResourceCacheService<FMODEventId> _eventRepository;

		private readonly IResourceCacheService<BankId> _bankRepository;
		
		public FMODGuidRepository GuidRepository => _guidRepository;
		private readonly FMODGuidRepository _guidRepository;

		private readonly StringBuilder _fmodDebugString = new StringBuilder( 1024 );

		/*
		===============
		FMODSystemService
		===============
		*/
		public FMODSystemService( IServiceLocator locator, IServiceRegistry registry ) {
			_logger = locator.GetService<ILoggerService>();

			var eventFactory = locator.GetService<IGameEventRegistryService>();
			var cvarSystem = locator.GetService<ICVarSystemService>();

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

			_guidRepository = new FMODGuidRepository();
			_eventRepository = new FMODEventRepository( _logger, eventFactory, this, _guidRepository );
			_bankRepository = new FMODBankRepository( _logger, eventFactory, this, _guidRepository );

			_logger.PrintLine( $"FMODSystemService: initializing FMOD sound system..." );

			FMODCVarRegistry.Register( cvarSystem );
			ConfigureFMODDevice( cvarSystem );

			_system.setCallback( OnAudioOutputDeviceListChanged, FMOD.SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			_logger.PrintLine( "FMODSystemService.Dispose: shutting down FMOD sound system..." );

			_eventRepository?.Dispose();
			_bankRepository?.Dispose();
			_guidRepository?.Dispose();

			if ( _studioSystem.isValid() ) {
				_system.close();
				_system.release();
				_system.clearHandle();
				_studioSystem.unloadAll();
				_studioSystem.release();
				_studioSystem.clearHandle();
			}
		}

		/*
		===============
		Update
		===============
		*/
		public void Update( float deltaTime ) {
			_system.update();
			_studioSystem.update();
		}

		/*
		===============
		GetAudioDriverNames
		===============
		*/
		public IReadOnlyList<string> GetAudioDriverNames() {
			return _drivers;
		}

		/*
		===============
		OnAudioOutputDeviceListChanged
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="system"></param>
		/// <param name="type"></param>
		/// <param name="commanddata1"></param>
		/// <param name="commanddata2"></param>
		/// <param name="userdata"></param>
		/// <returns></returns>
		private FMOD.RESULT OnAudioOutputDeviceListChanged( nint system, FMOD.SYSTEM_CALLBACK_TYPE type, nint commanddata1, nint commanddata2, nint userdata ) {
			GetAudioDrivers();
			return FMOD.RESULT.OK;
		}

		/*
		===============
		GetAudioDrivers
		===============
		*/
		private void GetAudioDrivers() {
			FMODValidator.ValidateCall( _system.getNumDrivers( out int numDrivers ) );
			_drivers.Clear();
			_drivers.EnsureCapacity( numDrivers );

			for ( int i = 0; i < numDrivers; i++ ) {
				FMODValidator.ValidateCall( _system.getDriverInfo( i, out string name, 256, out _, out _, out FMOD.SPEAKERMODE speakerMode, out int speakerChannels ) );
				_drivers.Add( name );
				_logger.PrintLine( $"FMODSystemService.GetAudioDrivers: found audio driver '{name}' - speakerMode = '{speakerMode}', channelCount = '{speakerChannels}'" );
			}
			FMODValidator.ValidateCall( _system.getDriver( out _audioDriver ) );
		}

		/*
		===============
		SetAudioDriver
		===============
		*/
		/// <summary>
		/// s
		/// </summary>
		/// <param name="driverName"></param>
		private void SetAudioDriver( string driverName ) {
			int driverIndex = -1;
			for ( int i = 0; i < _drivers.Count; i++ ) {
				if ( _drivers[ i ].Equals( driverName ) ) {
					driverIndex = i;
				}
			}
			if ( driverIndex == -1 ) {
				_logger.PrintError( $"FMODSystemService.SetAudioDriver: invalid audio driver id '{driverName}'" );
				return;
			}

			_logger.PrintLine( $"FMODSystemService.SetAudioDriver: setting audio driver to '{driverName}'..." );
			FMODValidator.ValidateCall( _system.setDriver( driverIndex ) );
			_audioDriver = driverIndex;
		}

		/*
		===============
		ConfigureFMODDevice
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cvarSystem"></param>
		/// <exception cref="Exception"></exception>
		private void ConfigureFMODDevice( ICVarSystemService cvarSystem ) {
			var streamBufferSize = cvarSystem.GetCVar<int>( "audio.fmod.StreamBufferSize" )
				?? throw new Exception( "Missing cvar audio.fmod.streamBufferSize!" );

			var flags = FMOD.INITFLAGS.CHANNEL_DISTANCEFILTER | FMOD.INITFLAGS.CHANNEL_LOWPASS | FMOD.INITFLAGS.VOL0_BECOMES_VIRTUAL;

			FMODValidator.ValidateCall( _system.setStreamBufferSize( (uint)streamBufferSize.Value * 1024, FMOD.TIMEUNIT.RAWBYTES ) );
			FMODValidator.ValidateCall( _studioSystem.initialize( 512, FMOD.Studio.INITFLAGS.LIVEUPDATE | FMOD.Studio.INITFLAGS.SYNCHRONOUS_UPDATE, flags, 0 ) );

			GetAudioDrivers();
			_audioDriver = 0;
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