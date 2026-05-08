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
using System.Runtime.InteropServices;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Logger;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Services
{
	internal sealed class SteamNetworkPacketSender
	{
		private readonly ILoggerCategory _category;

		public SteamNetworkPacketSender( ILoggerCategory category )
		{
			_category = category ?? throw new ArgumentNullException( nameof( category ) );
		}

		public unsafe void SendToPeer( SteamNetConnection connection, in NetworkPacket packet )
		{
#if DEBUG
			ArgumentGuard.ThrowIfNull( connection, nameof( connection ) );
#endif
			int packetLength = NetworkPacketHeader.SIZE + packet.Payload.Length;
			byte[] buffer = ArrayPool<byte>.Shared.Rent( packetLength );

			nint headerPtr = IntPtr.Zero;
			Marshal.StructureToPtr( packet.Header, headerPtr, false );
			Marshal.Copy( buffer, 0, headerPtr, NetworkPacketHeader.SIZE );

			fixed ( byte* ptr = buffer ) {
				packet.Payload.Slice( NetworkPacketHeader.SIZE ).CopyTo( buffer );
				EResult result = SteamNetworkingSockets.SendMessageToConnection( connection.Connection, (nint)ptr, (uint)packetLength, 0, out long messageNumber );
				if ( result != EResult.k_EResultOK ) {
					_category.PrintError( $"Couldn't send packet on connection: {result}" );
				}
			}
		}
	};
};
