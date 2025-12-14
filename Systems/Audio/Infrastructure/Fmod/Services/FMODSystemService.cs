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
using NomadCore.Systems.Audio.Application.Interfaces;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Registries;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories;
using System;
using System.Collections.Generic;

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

		private readonly List<string> _drivers;

		public FMOD.Studio.System StudioSystem => _system.StudioSystem;
		public FMOD.System CoreSystem => _system.System;

		private readonly FMODSystemHandle _system;

		private readonly ILoggerService _logger;

		private readonly FMODEventRepository _eventRepository;
		private readonly FMODBankRepository _bankRepository;

		/*
		===============
		FMODSystemService
		===============
		*/
		public FMODSystemService( ILoggerService logger, IGameEventRegistryService eventFactory, ICVarSystemService cvarSystem ) {
			_logger = logger;
			_eventRepository = new FMODEventRepository( logger, eventFactory, this );
			_bankRepository = new FMODBankRepository( logger, eventFactory, this );

			_logger.PrintLine( $"FMODSystemService: initializing FMOD sound system..." );

			_system = new FMODSystemHandle();

#if DEBUG
			var debugFlags = FMOD.DEBUG_FLAGS.LOG | FMOD.DEBUG_FLAGS.ERROR | FMOD.DEBUG_FLAGS.WARNING | FMOD.DEBUG_FLAGS.TYPE_TRACE | FMOD.DEBUG_FLAGS.DISPLAY_THREAD;
			FMODValidator.ValidateCall( FMOD.Debug.Initialize( debugFlags, FMOD.DEBUG_MODE.CALLBACK, DebugCallback ) );
#endif

			_system.System.setCallback( OnAudioOutputDeviceListChanged, FMOD.SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED );

			FMODCVarRegistry.Register( cvarSystem );

			ConfigureFMODDevice( cvarSystem );
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
			_system.Dispose();
		}

		/*
		===============
		Update
		===============
		*/
		public void Update( float deltaTime ) {
			_system.Update();
		}

		public IReadOnlyList<string> GetAudioDrivers() {
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
			_system.System.getNumDrivers( out int numDrivers );
			_drivers.Clear();
			_drivers.EnsureCapacity( numDrivers );

			for ( int i = 0; i < numDrivers; i++ ) {
				_system.System.getDriverInfo( i, out string name, 256, out _, out _, out FMOD.SPEAKERMODE speakerMode, out int speakerChannels );
			}
			_system.System.getDriver( out _audioDriver );

			return FMOD.RESULT.OK;
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
			FMODValidator.ValidateCall( _system.System.setDriver( driverIndex ) );
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
			
			var maxChannels = cvarSystem.GetCVar<int>( "audio.MaxChannels" )
				?? throw new Exception( "Missing cvar audio.MaxChannels!" );

			var flags = FMOD.INITFLAGS.CHANNEL_DISTANCEFILTER | FMOD.INITFLAGS.CHANNEL_LOWPASS | FMOD.INITFLAGS.VOL0_BECOMES_VIRTUAL;

			FMODValidator.ValidateCall( _system.System.setStreamBufferSize( (uint)streamBufferSize.Value * 1024, FMOD.TIMEUNIT.RAWBYTES ) );
			FMODValidator.ValidateCall( _system.StudioSystem.initialize( maxChannels.Value, FMOD.Studio.INITFLAGS.LIVEUPDATE | FMOD.Studio.INITFLAGS.SYNCHRONOUS_UPDATE, flags, 0 ) );
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
			if ( ( flags & FMOD.DEBUG_FLAGS.LOG ) != 0 ) {
				_logger.PrintLine( $"[FMOD] {message}" );
			} else if ( ( flags & FMOD.DEBUG_FLAGS.WARNING ) != 0 ) {
				_logger.PrintWarning( $"[FMOD] {message}" );
			} else if ( ( flags & FMOD.DEBUG_FLAGS.ERROR ) != 0 ) {
				_logger.PrintError( $"[FMOD] {message}" );
			}
			return FMOD.RESULT.OK;
		}
	};
};