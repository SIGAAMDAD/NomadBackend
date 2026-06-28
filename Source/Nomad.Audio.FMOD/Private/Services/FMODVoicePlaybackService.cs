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

	internal sealed class FMODVoicePlaybackService : IDisposable
	{
		private const int DEFAULT_BUFFER_MILLISECONDS = 500;
		private const int BYTES_PER_SAMPLE = sizeof( short );

		private readonly FMOD.System _system;
		private FMOD.Sound _sound;
		private FMOD.Channel _channel;

		private readonly FMOD.SOUND_PCMREAD_CALLBACK _pcmReadCallback;
		private readonly FMOD.SOUND_PCMSETPOS_CALLBACK _pcmSetPosCallback;

		private readonly object _lock = new();

		private readonly byte[] _buffer;
		private int _readIndex;
		private int _writeIndex;
		private int _bufferedBytes;

		private GCHandle _selfHandle;
		private bool _isDisposed;

		private readonly int _sampleRate;
		private readonly int _channels;
		private readonly int _bytesPerSecond;

		public int BufferedBytes {
			get {
				lock ( _lock ) {
					return _bufferedBytes;
				}
			}
		}

		public FMODVoicePlaybackService( FMOD.System system, int sampleRate, int channels )
			: this( system, sampleRate, channels, DEFAULT_BUFFER_MILLISECONDS )
		{
		}

		public FMODVoicePlaybackService( FMOD.System system, int sampleRate, int channels, int bufferMilliseconds )
		{
			if ( sampleRate <= 0 ) {
				throw new ArgumentOutOfRangeException( nameof( sampleRate ) );
			}
			if ( channels <= 0 ) {
				throw new ArgumentOutOfRangeException( nameof( channels ) );
			}
			if ( bufferMilliseconds <= 0 ) {
				throw new ArgumentOutOfRangeException( nameof( bufferMilliseconds ) );
			}

			_system = system;
			_sampleRate = sampleRate;
			_channels = channels;
			_bytesPerSecond = _sampleRate * _channels * BYTES_PER_SAMPLE;
			_buffer = new byte[Math.Max( _bytesPerSecond * bufferMilliseconds / 1000, _channels * BYTES_PER_SAMPLE )];

			_selfHandle = GCHandle.Alloc( this );
			_pcmReadCallback = PcmReadCallback;
			_pcmSetPosCallback = PcmSetPosCallback;

			var exInfo = new FMOD.CREATESOUNDEXINFO {
				cbsize = Marshal.SizeOf<FMOD.CREATESOUNDEXINFO>(),
				numchannels = _channels,
				defaultfrequency = _sampleRate,
				format = FMOD.SOUND_FORMAT.PCM16,
				length = (uint)_bytesPerSecond,
				decodebuffersize = (uint)Math.Max( _sampleRate / 20, 1 ),
				userdata = GCHandle.ToIntPtr( _selfHandle ),
			};
			exInfo.pcmreadcallback = _pcmReadCallback;
			exInfo.pcmsetposcallback = _pcmSetPosCallback;

			FMODValidator.ValidateCall( _system.createSound(
				(string)null,
				FMOD.MODE.OPENUSER | FMOD.MODE.CREATESTREAM | FMOD.MODE.LOOP_NORMAL | FMOD.MODE._2D,
				ref exInfo,
				out _sound
			) );
		}

		public void Push( byte[] pcmData )
		{
			if ( pcmData == null || pcmData.Length == 0 ) {
				return;
			}

			Push( pcmData.AsSpan() );
		}

		public void Push( ReadOnlySpan<byte> pcmData )
		{
			if ( pcmData.Length == 0 ) {
				return;
			}

			lock ( _lock ) {
				int sourceOffset = 0;
				if ( pcmData.Length >= _buffer.Length ) {
					sourceOffset = pcmData.Length - _buffer.Length;
					_readIndex = 0;
					_writeIndex = 0;
					_bufferedBytes = 0;
				} else {
					int overflow = (_bufferedBytes + pcmData.Length) - _buffer.Length;
					if ( overflow > 0 ) {
						DiscardOldest( overflow );
					}
				}

				while ( sourceOffset < pcmData.Length ) {
					int contiguous = Math.Min( _buffer.Length - _writeIndex, pcmData.Length - sourceOffset );
					pcmData.Slice( sourceOffset, contiguous ).CopyTo( _buffer.AsSpan( _writeIndex, contiguous ) );
					_writeIndex = (_writeIndex + contiguous) % _buffer.Length;
					_bufferedBytes += contiguous;
					sourceOffset += contiguous;
				}
			}
		}

		public void Start()
		{
			ThrowIfDisposed();
			if ( IsPlaying() ) {
				return;
			}

			FMODValidator.ValidateCall( _system.playSound( _sound, default, false, out _channel ) );
		}

		public void Stop()
		{
			if ( _channel.hasHandle() ) {
				FMOD.RESULT result = _channel.stop();
				if ( result != FMOD.RESULT.ERR_INVALID_HANDLE && result != FMOD.RESULT.ERR_CHANNEL_STOLEN ) {
					FMODValidator.ValidateCall( result );
				}
				_channel.clearHandle();
			}
		}

		public void Clear()
		{
			lock ( _lock ) {
				_readIndex = 0;
				_writeIndex = 0;
				_bufferedBytes = 0;
			}
		}

		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			Stop();

			if ( _sound.hasHandle() ) {
				FMODValidator.ValidateCall( _sound.release() );
				_sound.clearHandle();
			}

			if ( _selfHandle.IsAllocated ) {
				_selfHandle.Free();
			}

			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		private FMOD.RESULT PcmReadCallback( IntPtr soundRaw, IntPtr data, uint datalen )
		{
			try {
				int requested = checked((int)datalen);
				int written;
				lock ( _lock ) {
					written = ReadBuffered( data, requested );
				}

				if ( written < requested ) {
					FillSilence( IntPtr.Add( data, written ), requested - written );
				}

				return FMOD.RESULT.OK;
			} catch {
				return FMOD.RESULT.ERR_INTERNAL;
			}
		}

		private FMOD.RESULT PcmSetPosCallback( IntPtr soundRaw, int subsound, uint position, FMOD.TIMEUNIT postype )
		{
			return FMOD.RESULT.OK;
		}

		private int ReadBuffered( IntPtr destination, int requestedBytes )
		{
			int written = 0;
			while ( written < requestedBytes && _bufferedBytes > 0 ) {
				int take = Math.Min( requestedBytes - written, _bufferedBytes );
				take = Math.Min( take, _buffer.Length - _readIndex );

				Marshal.Copy( _buffer, _readIndex, IntPtr.Add( destination, written ), take );

				_readIndex = (_readIndex + take) % _buffer.Length;
				_bufferedBytes -= take;
				written += take;
			}
			return written;
		}

		private void DiscardOldest( int bytes )
		{
			int discard = Math.Min( bytes, _bufferedBytes );
			_readIndex = (_readIndex + discard) % _buffer.Length;
			_bufferedBytes -= discard;
		}

		private bool IsPlaying()
		{
			if ( !_channel.hasHandle() ) {
				return false;
			}

			FMOD.RESULT result = _channel.isPlaying( out bool isPlaying );
			return result == FMOD.RESULT.OK && isPlaying;
		}

		private void ThrowIfDisposed()
		{
			if ( _isDisposed ) {
				throw new ObjectDisposedException( nameof( FMODVoicePlaybackService ) );
			}
		}

		private static void FillSilence( IntPtr destination, int bytes )
		{
			for ( int i = 0; i < bytes; i++ ) {
				Marshal.WriteByte( destination, i, 0 );
			}
		}
	};
};
