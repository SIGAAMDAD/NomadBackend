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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;
using Nomad.Audio.Fmod.Private.Registries;
using Nomad.Audio.Fmod.Private.Repositories;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.Audio.ValueObjects;
using Nomad.Core;
using Nomad.Core.Events;
using Nomad.Core.Exceptions;
using Nomad.Core.Logger;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.CVars;
using Nomad.ResourceCache;

namespace Nomad.Audio.Fmod.Private.Services {
	/*
	===================================================================================

	FMODDevice

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODDevice : IAudioDevice {
		public FMOD.Studio.System StudioSystem => _systemHandle.StudioSystem;
		public FMOD.System System => _systemHandle.System;

		private readonly FMODSystemHandle _systemHandle;
		private readonly ILoggerService _logger;

		public IResourceCacheService<FMODEventResource, EventId> EventRepository => _eventRepository;
		private readonly FMODEventRepository _eventRepository;

		private readonly FMODBankRepository _bankRepository;

		public FMODGuidRepository GuidRepository => _guidRepository;
		private readonly FMODGuidRepository _guidRepository;

		private readonly FMODDriverRepository _driverRepository;

		private readonly Dictionary<BankHandle, FMODBankResource> _bankCache = new();

		/*
		===============
		FMODDevice
		===============
		*/
		public FMODDevice( IServiceLocator locator, IServiceRegistry registry ) {
			_logger = locator.GetService<ILoggerService>();

			var eventFactory = locator.GetService<IGameEventRegistryService>();
			var cvarSystem = locator.GetService<ICVarSystemService>();

			FMODCVarRegistry.Register( cvarSystem );
			_systemHandle = new FMODSystemHandle( cvarSystem, _logger );
			ConfigureFMODDevice( cvarSystem );

			_driverRepository = new FMODDriverRepository( _logger, cvarSystem, _systemHandle.System );

			_guidRepository = new FMODGuidRepository();
			_bankRepository = new FMODBankRepository( _logger, eventFactory, cvarSystem, this, _guidRepository );
			_eventRepository = new FMODEventRepository( _logger, eventFactory, this, _guidRepository );

			_logger.PrintLine( $"FMODDevice: initializing FMOD sound system..." );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			_logger.PrintLine( "FMODDevice.Dispose: shutting down FMOD sound system..." );

			_eventRepository?.Dispose();
			_bankRepository?.Dispose();
			_guidRepository?.Dispose();
			_systemHandle.Dispose();
		}

		/*
		===============
		GetAudioDrivers
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetAudioDrivers() {
			string[] drivers = new string[ _driverRepository.Drivers.Length ];
			for ( int i = 0; i < drivers.Length; i++ ) {
				drivers[ i ] = new( _driverRepository.Drivers[ i ].Name.Span );
			}
			return drivers;
		}

		/*
		===============
		Update
		===============
		*/
		public void Update( float deltaTime ) {
			_systemHandle.Update();
		}

		/*
		===============
		LoadBank
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="bankPath"></param>
		/// <param name="bank"></param>
		/// <returns></returns>
		public AudioResult LoadBank( string bankPath, out BankHandle bank ) {
			bank = new BankHandle( bankPath.GetHashCode() );

			FMODValidator.ValidateCall( _systemHandle.StudioSystem.loadBankFile( bankPath, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out var handle ) );
			_bankCache[ bank ] = new FMODBankResource( handle );

			return AudioResult.Success;
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
			var streamBufferSize = cvarSystem.GetCVar<int>( Constants.CVars.Audio.FMOD.STREAM_BUFFER_SIZE )
				?? throw new CVarMissing( Constants.CVars.Audio.FMOD.STREAM_BUFFER_SIZE );
			var maxChannels = cvarSystem.GetCVar<int>( Constants.CVars.Audio.MAX_CHANNELS )
				?? throw new CVarMissing( Constants.CVars.Audio.MAX_CHANNELS );
			var dspBufferSize = cvarSystem.GetCVar<uint>( Constants.CVars.Audio.FMOD.DSP_BUFFER_SIZE ) ?? throw new CVarMissing( Constants.CVars.Audio.FMOD.DSP_BUFFER_SIZE );
			var dspBufferCount = cvarSystem.GetCVar<int>( Constants.CVars.Audio.FMOD.DSP_BUFFER_COUNT ) ?? throw new CVarMissing( Constants.CVars.Audio.FMOD.DSP_BUFFER_COUNT );

			var flags = FMOD.INITFLAGS.CHANNEL_DISTANCEFILTER | FMOD.INITFLAGS.CHANNEL_LOWPASS | FMOD.INITFLAGS.VOL0_BECOMES_VIRTUAL;

			FMODValidator.ValidateCall( System.setStreamBufferSize( ( uint )streamBufferSize.Value, FMOD.TIMEUNIT.MS ) );
			FMODValidator.ValidateCall( System.setDSPBufferSize( dspBufferSize.Value * 1024, dspBufferCount.Value ) );
			FMODValidator.ValidateCall( StudioSystem.initialize( maxChannels.Value, FMOD.Studio.INITFLAGS.LIVEUPDATE | FMOD.Studio.INITFLAGS.SYNCHRONOUS_UPDATE, flags, 0 ) );
		}
	};
};
