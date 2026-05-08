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
using System.Runtime.InteropServices;
using Nomad.Core.CVars;
using Nomad.Core.Logger;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Services.NetworkServices
{
	internal sealed class SteamNetworkPacketReceiver
	{
		private const int DEFAULT_MAX_MESSAGES_PER_POLL = 256;
		private const int DEFAULT_BATCH_SIZE = 32;
		private const int DEFAULT_MAX_BYTES_PER_POLL = 1024 * 1024;

		private readonly ILoggerCategory _category;
		private readonly HSteamNetPollGroup _pollGroup;
		private readonly nint[] _messageBatch;

		private readonly CVarBinding<int> _maxMessagesPerPoll;
		private readonly CVarBinding<int> _maxBytesPerPoll;

		/*
		===============
		SteamNetworkPacketReceiver
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="hPollGroup"></param>
		/// <param name="category"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public SteamNetworkPacketReceiver( HSteamNetPollGroup hPollGroup, ILoggerCategory category )
		{
			_pollGroup = hPollGroup;
			_category = category ?? throw new ArgumentNullException( nameof( category ) );

			int batchSize = Math.Max( 1, DEFAULT_BATCH_SIZE );
			_messageBatch = new nint[batchSize];
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

			while ( messagesProcessed < _maxMessagesPerPoll.Value && bytesProcessed < _maxBytesPerPoll.Value ) {
				int messagesReceived = SteamNetworkingSockets.ReceiveMessagesOnPollGroup( _pollGroup, _messageBatch, _maxMessagesPerPoll.Value );
				if ( messagesReceived < 0 ) {
					_category.PrintError( $"" );
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
							continue;
						}
						bytesProcessed += message.m_cbSize;

						if ( bytesProcessed > _maxBytesPerPoll.Value ) {
							return;
						}
					} finally {
						message.Release();
					}
					if ( messagesProcessed >= _maxMessagesPerPoll.Value ) {
						return;
					}
				}
				if ( messagesReceived < _messageBatch.Length ) {
					return;
				}
			}
		}

		private void ProcessMessage( in SteamNetworkingMessage_t message )
		{
			CSteamID peerId = message.m_identityPeer.GetSteamID();
			if ( !peerId.IsValid() ) {
				_category.PrintError( "" );
				return;
			}

			byte[] rented = ArrayPool<byte>.Shared.Rent( message.m_cbSize );
			try {
				Marshal.Copy( message.m_pData, rented, 0, message.m_cbSize );
				ReadOnlyMemory<byte> bytes = new ReadOnlyMemory<byte>(
					rented,
					0,
					message.m_cbSize
				);
				if ( !NetworkPacketHeader.TryRead( bytes.Span, out var header, out string error ) ) {
					_category.PrintError( $"" );
					return;
				}
			} finally {
				ArrayPool<byte>.Shared.Return( rented );
			}
		}
	};
};
