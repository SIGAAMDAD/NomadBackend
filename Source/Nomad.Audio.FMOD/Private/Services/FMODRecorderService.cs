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
using System.Runtime.InteropServices;
using Nomad.Core.Logger;

namespace Nomad.Audio.Fmod.Private.Services
{
	internal sealed class FMODRecorderService
	{
		private readonly FMOD.DSP _recorder;
		private readonly FMOD.Sound _recordSound;

		public FMODRecorderService( FMODCallbackDispatcher callbackDispatcher, ILoggerCategory category, FMOD.System system )
		{
			FMODValidator.ValidateCall( category, system.getRecordNumDrivers( out int numDrivers, out int numConnected ) );

			system.getRecordDriverInfo(
				0,
				out string name,
				256,
				out Guid guid,
				out int sampleRate,
				out FMOD.SPEAKERMODE speakerMode,
				out int channels,
				out FMOD.DRIVER_STATE state
			);

			var ex = new FMOD.CREATESOUNDEXINFO() {
				cbsize = Marshal.SizeOf<FMOD.CREATESOUNDEXINFO>(),
				numchannels = channels,
				format = FMOD.SOUND_FORMAT.PCM16,
				defaultfrequency = sampleRate,
				length = (uint)(sampleRate * channels * sizeof( short ))
			};

			FMODValidator.ValidateCall( system.createSound(
				(string)null,
				FMOD.MODE.OPENUSER | FMOD.MODE.CREATESAMPLE | FMOD.MODE.LOOP_NORMAL,
				ref ex,
				out _recordSound
			) );
		}
	};
};
