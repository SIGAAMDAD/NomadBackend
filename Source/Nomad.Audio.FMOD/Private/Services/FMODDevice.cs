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

#if DEBUG
#define FMOD_LOGGING
#endif

using System;
using Nomad.Audio.Fmod.Private.Registries;
using Nomad.Audio.Fmod.Private.Repositories;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.Core;
using Nomad.Core.Logger;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.Core.Events;
using Nomad.Core.CVars;
using Nomad.CVars;
using Nomad.Core.Engine.Globals;
using Nomad.Core.Engine.Services;
using System.Collections.Generic;
using Nomad.Audio.ValueObjects;

namespace Nomad.Audio.Fmod.Private.Services
{
	/*
	===================================================================================

	FMODDevice

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODDevice : IAudioDevice
	{
		public int OutputDevice => _outputDeviceRepository.OutputDeviceIndex;
		public string AudioDriver => _driverRepository.Driver;

		public FMODEventRepository EventRepository => _eventRepository;
		private readonly FMODEventRepository _eventRepository;

		public FMODGuidRepository GuidRepository => _guidRepository;
		private readonly FMODGuidRepository _guidRepository;

		public IReadOnlyList<string> OutputDevices => _outputDeviceRepository.OutputDevices;
		public IReadOnlyList<string> AudioDrivers => _driverRepository.Drivers;

		public FMOD.Studio.System StudioSystem => _systemHandle.StudioSystem;
		public FMOD.System System => _systemHandle.System;

		private readonly FMODAudioGroupRepository _groupRepository;

		private readonly FMODSystemHandle _systemHandle;
		private readonly ILoggerCategory _category;

		private readonly FMODBankRepository _bankRepository;

		private readonly FMODDriverRepository _driverRepository;
		private readonly FMODOutputDeviceRepository _outputDeviceRepository;
		private readonly FMODDriverService _driverService;
		private readonly FMODCallbackDispatcher _callbackDispatcher;

		private bool _isDisposed = false;

		/*
		===============
		FMODDevice
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="locator"></param>
		/// <param name="registry"></param>
		public FMODDevice( IServiceLocator locator, IServiceRegistry registry )
		{
			var logger = locator.GetService<ILoggerService>();

			var cvarSystem = locator.GetService<ICVarSystemService>();
			var eventFactory = locator.GetService<IGameEventRegistryService>();

			FMODValidator.Initialize( logger );

			_category = logger.CreateCategory( "FMOD", LogLevel.Info, true );
			_category.PrintLine( "Initializing FMOD sound system..." );

			FMODCVarRegistry.Register( cvarSystem );
			_systemHandle = new FMODSystemHandle( cvarSystem, logger );
			ConfigureFMODDevice( cvarSystem );

			_callbackDispatcher = new FMODCallbackDispatcher( _systemHandle.System );
			_driverRepository = new FMODDriverRepository();
			_outputDeviceRepository = new FMODOutputDeviceRepository( _category );
			_driverService = new FMODDriverService( _category, _systemHandle.System, cvarSystem, _callbackDispatcher, _driverRepository, _outputDeviceRepository );

			_guidRepository = new FMODGuidRepository();
			_bankRepository = new FMODBankRepository( logger, eventFactory, this );
			_eventRepository = new FMODEventRepository( logger, eventFactory, this );

			// preload the strings bank so we get all the human-readable event names cached
			_bankRepository.GetCached( EngineService.GetStoragePath( "Audio/Banks/Desktop/Master.strings.bank", StorageScope.StreamingAssets ) );
			_bankRepository.GetCached( EngineService.GetStoragePath( "Audio/Banks/Desktop/Master.bank", StorageScope.StreamingAssets ) );

			_groupRepository = new FMODAudioGroupRepository( _systemHandle.StudioSystem, _category, cvarSystem );
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
				_category.PrintLine( "Shutting down FMOD sound system..." );

				_eventRepository?.Dispose();
				_bankRepository?.Dispose();
				_guidRepository?.Dispose();
				_driverService?.Dispose();
				_outputDeviceRepository?.Dispose();
				_callbackDispatcher?.Dispose();
				_category?.Dispose();
				_systemHandle.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		LoadBank
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="assetPath"></param>
		public void LoadBank( string assetPath )
		{
			_bankRepository.GetCached( assetPath );
		}

		/*
		===============
		UnloadBank
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="assetPath"></param>
		public void UnloadBank( string assetPath )
		{
			_bankRepository.Unload( assetPath );
		}

		/*
		===============
		UnloadBanks
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void UnloadBanks()
		{
			_bankRepository.UnloadAll();
		}

		/*
		===============
		Update
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="deltaTime"></param>
		public void Update( float deltaTime )
		{
			_systemHandle.Update();
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
		private void ConfigureFMODDevice( ICVarSystemService cvarSystem )
		{
			var streamBufferSize = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.FMOD.STREAM_BUFFER_SIZE );
			var maxChannels = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.MAX_CHANNELS );
			var dspBufferSize = cvarSystem.GetCVarOrThrow<uint>( Constants.CVars.EngineUtils.Audio.FMOD.DSP_BUFFER_SIZE );
			var dspBufferCount = cvarSystem.GetCVarOrThrow<int>( Constants.CVars.EngineUtils.Audio.FMOD.DSP_BUFFER_COUNT );
			var speakerMode = cvarSystem.GetCVarOrThrow<SpeakerMode>( Constants.CVars.EngineUtils.Audio.SPEAKER_MODE );

			var flags = FMOD.INITFLAGS.CHANNEL_DISTANCEFILTER | FMOD.INITFLAGS.CHANNEL_LOWPASS | FMOD.INITFLAGS.VOL0_BECOMES_VIRTUAL;

			FMODValidator.ValidateCall( _category, System.set3DSettings( 0.0f, 1.0f, 1.0f ) );
			FMODValidator.ValidateCall( _category, System.setSoftwareFormat( 44100, SpeakerModeToFMOD( speakerMode.Value ), 0 ) );
			FMODValidator.ValidateCall( _category, System.setStreamBufferSize( (uint)streamBufferSize.Value, FMOD.TIMEUNIT.MS ) );
			FMODValidator.ValidateCall( _category, System.setDSPBufferSize( dspBufferSize.Value, dspBufferCount.Value ) );
			FMODValidator.ValidateCall( _category, StudioSystem.initialize( maxChannels.Value, FMOD.Studio.INITFLAGS.LIVEUPDATE | FMOD.Studio.INITFLAGS.SYNCHRONOUS_UPDATE, flags, (IntPtr)null ) );
		}

		/*
		===============
		SpeakerModeToFMOD
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		private static FMOD.SPEAKERMODE SpeakerModeToFMOD( SpeakerMode mode ) => mode switch {
			SpeakerMode.Auto => FMOD.SPEAKERMODE.DEFAULT,
			SpeakerMode.StereoSpeakers or SpeakerMode.Headphones => FMOD.SPEAKERMODE.STEREO,
			SpeakerMode.Surround_5_1 => FMOD.SPEAKERMODE._5POINT1,
			SpeakerMode.Surround_7_1 => FMOD.SPEAKERMODE._7POINT1,
			SpeakerMode.Atmos => FMOD.SPEAKERMODE._7POINT1POINT4,
			_ => throw new ArgumentOutOfRangeException( nameof( mode ) )
		};
	};
};
