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
using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Nomad.Audio.Fmod.Private.Services
{
	/*
	===================================================================================
	
	FMODVoicePlaybackService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class FMODVoicePlaybackService
	{
		private readonly FMOD.System _system;
		private readonly FMOD.Sound _sound;

		private readonly FMOD.CREATESOUNDEXINFO _exInfo;
		private readonly FMOD.SOUND_PCMREAD_CALLBACK _pcmReadCallback;
		private readonly FMOD.SOUND_PCMSETPOS_CALLBACK _pcmSetPosCallback;

		private readonly object _lock = new();

		// TODO: make a ring buffer for this thing
		private readonly ConcurrentQueue<byte[]> _queue = new();
		private byte[] _leftover = Array.Empty<byte>();
		private GCHandle _selfHandle;

		private readonly int _sampleRate;
		private readonly int _channels;

		public FMODVoicePlaybackService( FMOD.System system, int sampleRate, int channels )
		{
			_system = system;
			_sampleRate = sampleRate;
			_channels = channels;

			_selfHandle = GCHandle.Alloc( this );

			_exInfo = new FMOD.CREATESOUNDEXINFO {
				cbsize = Marshal.SizeOf<FMOD.CREATESOUNDEXINFO>(),
				numchannels = _channels,
				defaultfrequency = _sampleRate,
				format = FMOD.SOUND_FORMAT.PCM16,
				userdata = GCHandle.ToIntPtr( _selfHandle ),
			};
		}

		public void Push( byte[] pcmData )
		{
			if ( pcmData != null && pcmData.Length > 0 ) {
				_queue.Enqueue( pcmData );
			}
		}

		private FMOD.RESULT PcmReadCallback( IntPtr soundRaw, IntPtr data, uint datalen )
		{
			try {
				byte[] block = new byte[datalen];
				int written = 0;

				if ( _leftover.Length > 0 ) {
					int take = Math.Min( _leftover.Length, (int)datalen );
					Buffer.BlockCopy( _leftover, 0, block, 0, take );
					written += take;

					if ( take == _leftover.Length ) {
						_leftover = Array.Empty<byte>();
					} else {
						byte[] remain = new byte[_leftover.Length - take];
						Buffer.BlockCopy( _leftover, take, remain, 0, remain.Length );
						_leftover = remain;
					}
				}

				while ( written < datalen && _queue.TryDequeue( out var packet ) ) {
					int remaining = (int)datalen - written;
					int take = Math.Min( packet.Length, remaining );

					Buffer.BlockCopy( packet, 0, block, written, take );
					written += take;

					if ( take < packet.Length ) {
						_leftover = new byte[packet.Length - take];
						Buffer.BlockCopy( packet, take, _leftover, 0, _leftover.Length );
						break;
					}
				}

				Marshal.Copy( block, 0, data, block.Length );

				return FMOD.RESULT.OK;
			} catch {
				return FMOD.RESULT.ERR_INTERNAL;
			}
		}
	};
};
