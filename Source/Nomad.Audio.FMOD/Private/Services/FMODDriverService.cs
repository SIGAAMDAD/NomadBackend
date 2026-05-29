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
using Nomad.Audio.Fmod.Private.Repositories;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Core;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.CVars;

namespace Nomad.Audio.Fmod.Private.Services
{
	/*
	===================================================================================

	FMODDriverService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODDriverService : IDisposable
	{
		private readonly FMODDriverRepository _repository;
		private readonly FMODOutputDeviceRepository _outputDeviceRepository;
		private readonly FMODCallbackDispatcher _callbackDispatcher;

		private readonly ILoggerCategory _category;
		private readonly FMOD.System _system;

		private readonly ISubscriptionHandle _audioDriverChangedSubscription;
		private readonly ISubscriptionHandle _outputDeviceChangedSubscription;

		private int _requestedOutputDeviceIndex;
		private bool _isDisposed = false;

		/*
		===============
		FMODDriverService
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="category"></param>
		/// <param name="system"></param>
		/// <param name="cvarSystem"></param>
		/// <param name="callbackDispatcher"></param>
		/// <param name="repository"></param>
		/// <param name="outputDeviceRepository"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public FMODDriverService(
			ILoggerCategory category,
			FMOD.System system,
			ICVarSystemService cvarSystem,
			FMODCallbackDispatcher callbackDispatcher,
			FMODDriverRepository repository,
			FMODOutputDeviceRepository outputDeviceRepository
		)
		{
			ArgumentGuard.ThrowIfNull( cvarSystem );

			_category = category ?? throw new ArgumentNullException( nameof( category ) );
			_system = system;
			_callbackDispatcher = callbackDispatcher ?? throw new ArgumentNullException( nameof( callbackDispatcher ) );
			_repository = repository ?? throw new ArgumentNullException( nameof( repository ) );
			_outputDeviceRepository = outputDeviceRepository ?? throw new ArgumentNullException( nameof( outputDeviceRepository ) );

			var audioDriver = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.AUDIO_DRIVER );
			_audioDriverChangedSubscription = audioDriver.ValueChanged.Subscribe( OnAudioDriverValueChanged );

			var outputDeviceIndex = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.OUTPUT_DEVICE_INDEX );
			_requestedOutputDeviceIndex = outputDeviceIndex.Value;
			_outputDeviceChangedSubscription = outputDeviceIndex.ValueChanged.Subscribe( OnAudioDeviceValueChanged );

			_callbackDispatcher.DeviceListChanged += OnAudioOutputDeviceListChanged;
			_callbackDispatcher.DeviceLost += OnAudioOutputDeviceLost;
			_callbackDispatcher.DeviceReinitialized += OnAudioOutputDeviceReinitialized;

			ApplyAudioDriver( (FMOD.OUTPUTTYPE)audioDriver.Value );
			ApplyOutputDevice( _requestedOutputDeviceIndex );
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
			if ( !_isDisposed ) {
				_callbackDispatcher.DeviceListChanged -= OnAudioOutputDeviceListChanged;
				_callbackDispatcher.DeviceLost -= OnAudioOutputDeviceLost;
				_callbackDispatcher.DeviceReinitialized -= OnAudioOutputDeviceReinitialized;

				_audioDriverChangedSubscription?.Dispose();
				_outputDeviceChangedSubscription?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
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
		private void OnAudioDriverValueChanged( in CVarValueChangedEventArgs<int> args )
		{
			ApplyAudioDriver( (FMOD.OUTPUTTYPE)args.NewValue );
		}

		/*
		===============
		OnAudioDeviceValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnAudioDeviceValueChanged( in CVarValueChangedEventArgs<int> args )
		{
			_requestedOutputDeviceIndex = args.NewValue;
			ApplyOutputDevice( _requestedOutputDeviceIndex );
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
		private void OnAudioOutputDeviceListChanged( FMOD.System system )
		{
			_category.PrintLine( "FMODDriverService.OnAudioOutputDeviceListChanged: refreshing audio output device list..." );
			RefreshOutputDevices();
			ApplyOutputDevice( _requestedOutputDeviceIndex );
		}

		/*
		===============
		OnAudioOutputDeviceLost
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="system"></param>
		private void OnAudioOutputDeviceLost( FMOD.System system )
		{
			_category.PrintWarning( "FMODDriverService.OnAudioOutputDeviceLost: FMOD reported that the active output device was lost." );
			RefreshOutputDevices();
		}

		/*
		===============
		OnAudioOutputDeviceReinitialized
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="system"></param>
		private void OnAudioOutputDeviceReinitialized( FMOD.System system )
		{
			_category.PrintLine( "FMODDriverService.OnAudioOutputDeviceReinitialized: FMOD reinitialized the output device, refreshing cached devices..." );
			RefreshOutputDevices();
			ApplyOutputDevice( _requestedOutputDeviceIndex );
		}

		/*
		===============
		ApplyAudioDriver
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="audioDriver"></param>
		private void ApplyAudioDriver( FMOD.OUTPUTTYPE audioDriver )
		{
			_category.PrintLine( $"FMODDriverService.ApplyAudioDriver: setting audio driver API to '{_repository.GetDriverName( audioDriver )}'..." );
			FMODValidator.ValidateCall( _category, _system.setOutput( audioDriver ) );
			RefreshOutputDevices();
		}

		/*
		===============
		ApplyOutputDevice
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="outputDeviceIndex"></param>
		private void ApplyOutputDevice( int outputDeviceIndex )
		{
			if ( !_outputDeviceRepository.ContainsDeviceIndex( outputDeviceIndex ) ) {
				if ( _outputDeviceRepository.Devices.Count > 0 ) {
					_category.PrintWarning( $"FMODDriverService.ApplyOutputDevice: output device index '{outputDeviceIndex}' is unavailable for driver '{_repository.Driver}'." );
				}
				return;
			}

			var device = _outputDeviceRepository.GetDevice( outputDeviceIndex );
			_category.PrintLine( $"FMODDriverService.ApplyOutputDevice: setting output audio device to '{device.Name}'..." );
			FMODValidator.ValidateCall( _category, _system.setDriver( outputDeviceIndex ) );
			_outputDeviceRepository.SetCurrentDevice( outputDeviceIndex );
		}

		/*
		===============
		RefreshDriverState
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void RefreshDriverState()
		{
			FMODValidator.ValidateCall( _category, _system.getOutput( out FMOD.OUTPUTTYPE audioDriver ) );
			_repository.SetCurrentDriver( audioDriver );
		}

		/*
		===============
		RefreshOutputDevices
		===============
		*/
		/// <summary>
		///
		/// </summary>
		private void RefreshOutputDevices()
		{
			FMODValidator.ValidateCall( _category, _system.getNumDrivers( out int numDrivers ) );

			var devices = new FMODDeviceInfo[numDrivers];
			for ( int i = 0; i < numDrivers; i++ ) {
				FMODValidator.ValidateCall(
					_category,
					_system.getDriverInfo(
						i,
						out string name,
						256,
						out var guid,
						out int systemRate,
						out FMOD.SPEAKERMODE speakerMode,
						out int speakerChannels
					)
				);
				devices[i] = new FMODDeviceInfo( name, guid, systemRate, speakerMode, speakerChannels );
			}

			FMODValidator.ValidateCall( _category, _system.getDriver( out int currentDriverIndex ) );
			_outputDeviceRepository.Refresh( devices, currentDriverIndex );
		}
	};
};
