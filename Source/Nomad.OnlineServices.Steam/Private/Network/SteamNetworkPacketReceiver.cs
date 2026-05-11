/*
===========================================================================
The Nomad MPLv2 Source Code
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
using Nomad.Core.Logger;
using Nomad.Networking.Messaging;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Network
{
	internal sealed class SteamNetworkPacketReceiver : IDisposable
	{
		private const int DEFAULT_MAX_MESSAGES_PER_POLL = 256;
		private const int DEFAULT_BATCH_SIZE = 32;
		private const int DEFAULT_MAX_BYTES_PER_POLL = 1 * 1024 * 1024;

		/// <summary>
		/// <see cref="NetworkPacketHeader.SIZE"/> (16 bytes) + the maximum value of a <see cref="ushort"/>
		/// </summary>
		private const int MAX_PACKET_SIZE = NetworkPacketHeader.SIZE + ushort.MaxValue;

		public bool IsOpen => _pollGroup != HSteamNetPollGroup.Invalid;

		private readonly ILoggerCategory _category = null;
		private readonly ConcurrentQueue<ReceivedNetworkPacket> _packets = new();
		private HSteamNetPollGroup _pollGroup = HSteamNetPollGroup.Invalid;

		private readonly nint[] _messageBatch = null;
		private readonly byte[] _messageBuffer = null;

		private readonly int _maxMessagesPerPoll = DEFAULT_MAX_MESSAGES_PER_POLL;
		private readonly int _maxBytesPerPoll = DEFAULT_MAX_BYTES_PER_POLL;

		private bool _isDisposed = false;

		/*
		===============
		SteamNetworkPacketReceiver
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="category"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SteamNetworkPacketReceiver( ILoggerCategory category )
		{
			_category = category ?? throw new ArgumentNullException( nameof( category ) );

			int batchSize = Math.Max( 1, DEFAULT_BATCH_SIZE );
			_messageBatch = new nint[batchSize];
			_messageBuffer = new byte[MAX_PACKET_SIZE];
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
			if ( _isDisposed ) {
				return;
			}

			CloseConnection();

			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		/*
		===============
		OpenConnection
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public bool OpenConnection( HSteamNetConnection connection )
		{
			if ( _pollGroup == HSteamNetPollGroup.Invalid ) {
				_pollGroup = SteamNetworkingSockets.CreatePollGroup();
				if ( _pollGroup == HSteamNetPollGroup.Invalid ) {
					return false;
				}
			}
			if ( !SteamNetworkingSockets.SetConnectionPollGroup( connection, _pollGroup ) ) {
				return false;
			}
			return true;
		}

		/*
		===============
		CloseConnection
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public bool CloseConnection()
		{
			if ( _pollGroup == HSteamNetPollGroup.Invalid ) {
				return true;
			}
			if ( !SteamNetworkingSockets.DestroyPollGroup( _pollGroup ) ) {
				return false;
			}
			return true;
		}

		/*
		===============
		Poll
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Poll()
		{
			int messagesProcessed = 0;
			int bytesProcessed = 0;

			while ( messagesProcessed < _maxMessagesPerPoll && bytesProcessed < _maxBytesPerPoll ) {
				int messagesReceived = SteamNetworkingSockets.ReceiveMessagesOnPollGroup( _pollGroup, _messageBatch, _messageBatch.Length );
				if ( messagesReceived < 0 ) {
					_category.PrintError( $"Received negative messages from SteamNetworkingSockets!" );
					return;
				}
				if ( messagesReceived == 0 ) {
					return;
				}
				for ( int i = 0; i < messagesReceived; i++ ) {
					SteamNetworkingMessage_t message = Marshal.PtrToStructure<SteamNetworkingMessage_t>( _messageBatch[i] );
					try {
						messagesProcessed++;
						if ( message.m_cbSize <= 0 || message.m_pData == IntPtr.Zero ) {
							_category.PrintWarning( $"Poll: Received message with zero length or nullptr data." );
							continue;
						} else if ( message.m_cbSize >= MAX_PACKET_SIZE ) {
							_category.PrintWarning( $"Poll: Received message that exceeds the maximum packet size ({MAX_PACKET_SIZE})." );
							continue;
						}
						bytesProcessed += message.m_cbSize;

						if ( bytesProcessed > _maxBytesPerPoll ) {
							return;
						}

						ProcessMessage( in message );
					} finally {
						message.Release();
					}
					if ( messagesProcessed >= _maxMessagesPerPoll ) {
						return;
					}
				}
				if ( messagesReceived < _messageBatch.Length ) {
					return;
				}
			}
		}

		/*
		===============
		TryReceive
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="destination"></param>
		/// <param name="packet"></param>
		/// <returns></returns>
		public bool TryReceive( Span<byte> destination, out ReceivedNetworkPacket packet )
		{
			Poll();

			packet = default;
			if ( !_packets.TryDequeue( out ReceivedNetworkPacket queued ) ) {
				return false;
			}

			if ( queued.BytesWritten > destination.Length ) {
				_category.PrintError( $"Dropped Steam network packet: payload size {queued.BytesWritten} exceeds destination size {destination.Length}." );
				queued.ReleasePayload();
				return false;
			}

			queued.Payload.AsSpan( 0, queued.BytesWritten ).CopyTo( destination );
			queued.ReleasePayload();
			packet = queued;

			return true;
		}

		/*
		===============
		ProcessMessage
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="message"></param>
		private void ProcessMessage( in SteamNetworkingMessage_t message )
		{
			CSteamID peerId = message.m_identityPeer.GetSteamID();
			if ( !peerId.IsValid() ) {
				_category.PrintError( "Error reading network packet: network peer identity in socket message isn't a valid CSteamID!" );
				return;
			}

			Marshal.Copy( message.m_pData, _messageBuffer, 0, message.m_cbSize );
			ReadOnlyMemory<byte> bytes = new ReadOnlyMemory<byte>(
				_messageBuffer,
				0,
				message.m_cbSize
			);
			if ( !NetworkPacketHeader.TryRead( bytes.Span, out var header, out string error ) ) {
				_category.PrintError( $"Error reading network packet header: {error}" );
				return;
			}

			if ( header.Type != NetworkPacketType.Payload ) {
				return;
			}

			if ( header.Flags < (ushort)NetworkSendMode.Min || header.Flags > (ushort)NetworkSendMode.Max ) {
				_category.PrintError( $"Error reading network packet: invalid send mode '{header.Flags}'." );
				return;
			}

			// TODO: create rotating ringbuffer for more efficient arraypool allocation.
			byte[] payload = ArrayPool<byte>.Shared.Rent( header.PayloadLength );
			bytes.Span.Slice( NetworkPacketHeader.SIZE, header.PayloadLength ).CopyTo( payload );

			_packets.Enqueue(
				new ReceivedNetworkPacket(
					peerId,
					payload,
					header.PayloadLength,
					(NetworkSendMode)(byte)header.Flags
				)
			);
		}
	};
};
