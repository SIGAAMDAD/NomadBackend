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
using System.Collections.Generic;
using Nomad.Audio.Fmod.Private.ValueObjects;
using Nomad.Core.Logger;

namespace Nomad.Audio.Fmod.Private.Repositories
{
	/*
	===================================================================================

	FMODOutputDeviceRepository

	===================================================================================
	*/
	/// <summary>
	/// Holds the currently available FMOD output devices and the active selection.
	/// </summary>

	internal sealed class FMODOutputDeviceRepository : IDisposable
	{
		/// <summary>
		/// A list of all output devices available to the FMOD API.
		/// </summary>
		public IReadOnlyList<FMODDeviceInfo> Devices => _devices;
		private FMODDeviceInfo[] _devices = Array.Empty<FMODDeviceInfo>();

		/// <summary>
		/// The index of the current audio output device.
		/// </summary>
		public int OutputDeviceIndex => _outputDeviceIndex;
		private int _outputDeviceIndex = -1;

		/// <summary>
		/// The names of the available output devices.
		/// </summary>
		public IReadOnlyList<string> OutputDevices => _outputDevices;
		private readonly List<string> _outputDevices = new();

		private readonly ILoggerCategory _category;

		/*
		===============
		FMODOutputDeviceRepository
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="category"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public FMODOutputDeviceRepository( ILoggerCategory category )
		{
			_category = category ?? throw new ArgumentNullException( nameof( category ) );
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
			_devices = Array.Empty<FMODDeviceInfo>();
			_outputDevices.Clear();
			_outputDeviceIndex = -1;
		}

		/*
		===============
		Refresh
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="devices"></param>
		/// <param name="outputDeviceIndex"></param>
		public void Refresh( FMODDeviceInfo[] devices, int outputDeviceIndex )
		{
			_devices = devices ?? Array.Empty<FMODDeviceInfo>();

			_outputDevices.Clear();
			_outputDevices.Capacity = _devices.Length;

			for ( int i = 0; i < _devices.Length; i++ ) {
				FMODDeviceInfo device = _devices[i];
				_outputDevices.Add( device.Name );
				_category.PrintLine( $"FMODOutputDeviceRepository.Refresh: found audio output device '{device.Name}' - speakerMode = '{device.SpeakerMode}', channelCount = '{device.SpeakerChannels}'" );
			}

			_outputDeviceIndex = ContainsDeviceIndex( outputDeviceIndex ) ? outputDeviceIndex : -1;
		}

		/*
		===============
		ContainsDeviceIndex
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="deviceIndex"></param>
		/// <returns></returns>
		public bool ContainsDeviceIndex( int deviceIndex )
		{
			return deviceIndex >= 0 && deviceIndex < _devices.Length;
		}

		/*
		===============
		GetDevice
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="deviceIndex"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public FMODDeviceInfo GetDevice( int deviceIndex )
		{
			if ( !ContainsDeviceIndex( deviceIndex ) ) {
				throw new ArgumentOutOfRangeException( nameof( deviceIndex ) );
			}
			return _devices[deviceIndex];
		}

		/*
		===============
		SetCurrentDevice
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="deviceIndex"></param>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void SetCurrentDevice( int deviceIndex )
		{
			if ( !ContainsDeviceIndex( deviceIndex ) ) {
				throw new ArgumentOutOfRangeException( nameof( deviceIndex ) );
			}
			_outputDeviceIndex = deviceIndex;
		}
	};
};
