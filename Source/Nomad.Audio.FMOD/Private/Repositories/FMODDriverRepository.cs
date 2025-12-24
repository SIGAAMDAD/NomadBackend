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
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Core;
using Nomad.Core.Exceptions;
using Nomad.Core.Logger;
using Nomad.CVars;

namespace Nomad.Audio.Fmod.Private.Repositories {
	/*
	===================================================================================

	FMODDriverRepository

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODDriverRepository : IDisposable {
		public FMODDriverInfo[] Drivers => _drivers;
		private FMODDriverInfo[] _drivers;

		public int DriverIndex {
			get => _driverIndex;
			set {
				SetDriver( value );
			}
		}
		private int _driverIndex = 0;

		private readonly ILoggerService _logger;
		private readonly FMOD.System _system;

		/*
		===============
		FMODDriverRepository
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="logger"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="system"></param>
		public FMODDriverRepository( ILoggerService logger, ICVarSystemService cvarSystem, FMOD.System system ) {
			_logger = logger;
			_system = system;

			var audioDriver = cvarSystem.GetCVar<string>( Constants.CVars.Audio.AUDIO_DRIVER ) ?? throw new CVarMissing( Constants.CVars.Audio.AUDIO_DRIVER );
			audioDriver.ValueChanged.Subscribe( this, OnAudioDriverValueChanged );

			var outputDeviceIndex = cvarSystem.GetCVar<int>( Constants.CVars.Audio.OUTPUT_DEVICE_INDEX ) ?? throw new CVarMissing( Constants.CVars.Audio.OUTPUT_DEVICE_INDEX );

			FMODValidator.ValidateCall( _system.setCallback( OnAudioOutputDeviceListChanged, FMOD.SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED ) );

			GetAudioDriverData();
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Dispose() {
			_drivers = null;
		}

		/*
		===============
		SetDriver
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="driverIndex"></param>
		public void SetDriver( int driverIndex ) {
			if ( driverIndex < 0 || driverIndex >= _drivers.Length ) {
				throw new ArgumentOutOfRangeException( nameof( driverIndex ) );
			}
			var driver = _drivers[ driverIndex ];
			_logger.PrintLine( $"FMODDriverRepository.SetAudioDriver: setting audio driver to '{driver.Name}'..." );
			FMODValidator.ValidateCall( _system.setDriver( driverIndex ) );
			_driverIndex = driverIndex;
		}

		/*
		===============
		GetAudioDriverData
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void GetAudioDriverData() {
			FMODValidator.ValidateCall( _system.getNumDrivers( out int numDrivers ) );

			_drivers = new FMODDriverInfo[ numDrivers ];
			for ( int i = 0; i < numDrivers; i++ ) {
				FMODValidator.ValidateCall( _system.getDriverInfo( i, out ReadOnlyMemory<char> name, 256, out var guid, out int systemRate, out FMOD.SPEAKERMODE speakerMode, out int speakerChannels ) );
				_drivers[ i ] = new FMODDriverInfo( name, guid, systemRate, speakerMode, speakerChannels );
				_logger.PrintLine( $"FMODDevice.GetAudioDriverData: found audio driver '{name}' - speakerMode = '{speakerMode}', channelCount = '{speakerChannels}'" );
			}

			_system.getDriver( out _driverIndex );
		}

		/*
		===============
		OnAudioDriverValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnAudioDriverValueChanged( in CVarValueChangedEventArgs<string> args ) {
			ReadOnlySpan<char> span = args.NewValue;
			int driverIndex = -1;

			for ( int i = 0; i < _drivers.Length; i++ ) {
				if ( _drivers[ i ].Name.Span.SequenceEqual( span ) ) {
					driverIndex = i;
					break;
				}
			}
			if ( driverIndex == -1 ) {
				_logger.PrintError( $"FMODDriverRepository.OnAudioDriverValueChanged: invalid audio driver name '{args.NewValue}'" );
				return;
			}
			SetDriver( driverIndex );
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
			GetAudioDriverData();
			return FMOD.RESULT.OK;
		}
	};
};
