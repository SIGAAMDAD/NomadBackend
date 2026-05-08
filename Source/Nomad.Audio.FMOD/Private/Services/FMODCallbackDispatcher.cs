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

namespace Nomad.Audio.Fmod.Private.Services
{
	/*
	===================================================================================
	
	FMODCallbackDispatcher
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class FMODCallbackDispatcher : IDisposable
	{
		public event Action<FMOD.System> DeviceListChanged;
		public event Action<FMOD.System> DeviceLost;
		public event Action<FMOD.System> DeviceReinitialized;

		private readonly FMOD.System _system;
		private readonly FMOD.SYSTEM_CALLBACK _systemCallback;

		private bool _isDisposed = false;

		public FMODCallbackDispatcher( FMOD.System system )
		{
			_system = system;
			_systemCallback = OnFMODSystemCallback;

			FMODValidator.ValidateCall(
				_system.setCallback(
					_systemCallback,
					FMOD.SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED |
					FMOD.SYSTEM_CALLBACK_TYPE.DEVICELOST |
					FMOD.SYSTEM_CALLBACK_TYPE.DEVICEREINITIALIZE
				)
			);
		}

		public void Dispose()
		{
			if ( !_isDisposed && _system.hasHandle() ) {
				FMODValidator.ValidateCall(
					_system.setCallback(
						null,
						FMOD.SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED |
						FMOD.SYSTEM_CALLBACK_TYPE.DEVICELOST |
						FMOD.SYSTEM_CALLBACK_TYPE.DEVICEREINITIALIZE
					)
				);
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		private FMOD.RESULT OnFMODSystemCallback( nint system, FMOD.SYSTEM_CALLBACK_TYPE type, nint commanddata1, nint commanddata2, nint userdata )
		{
			var fmodSystem = new FMOD.System( system );
			switch ( type ) {
				case FMOD.SYSTEM_CALLBACK_TYPE.DEVICELISTCHANGED:
					DeviceListChanged?.Invoke( fmodSystem );
					break;
				case FMOD.SYSTEM_CALLBACK_TYPE.DEVICELOST:
					DeviceLost?.Invoke( fmodSystem );
					break;
				case FMOD.SYSTEM_CALLBACK_TYPE.DEVICEREINITIALIZE:
					DeviceReinitialized?.Invoke( fmodSystem );
					break;
			}
			return FMOD.RESULT.OK;
		}
	};
};
