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
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Nomad.OnlineServices.Steam.Private.ValueObjects
{
	[StructLayout( LayoutKind.Explicit, Size = 16, Pack = 8 )]
	internal readonly struct NetworkPacketHeader
	{
		public const uint MAGIC = 0x4E4D504B; // "NPMK"
		public const ushort PROTOCOL_VERSION = 1;
		public const int SIZE = 16;

		[FieldOffset( 0 )] public readonly uint Magic;
		[FieldOffset( 4 )] public readonly ushort ProtocolVersion;
		[FieldOffset( 6 )] public readonly NetworkPacketType Type;
		[FieldOffset( 8 )] public readonly uint Sequence;
		[FieldOffset( 12 )] public readonly ushort Flags;
		[FieldOffset( 14 )] public readonly ushort PayloadLength;

		public NetworkPacketHeader(
			NetworkPacketType type,
			uint sequence,
			ushort flags,
			ushort payloadLength
		)
		{
			Magic = MAGIC;
			ProtocolVersion = PROTOCOL_VERSION;
			Type = type;
			Sequence = sequence;
			Flags = flags;
			PayloadLength = payloadLength;
		}

		public void WriteTo( Span<byte> data )
		{
			BinaryPrimitives.WriteUInt32LittleEndian( data.Slice( 0, 4 ), Magic );
			BinaryPrimitives.WriteUInt16LittleEndian( data.Slice( 4, 2 ), ProtocolVersion );
			BinaryPrimitives.WriteUInt16LittleEndian( data.Slice( 6, 2 ), (ushort)Type );
			BinaryPrimitives.WriteUInt32LittleEndian( data.Slice( 8, 4 ), Sequence );
			BinaryPrimitives.WriteUInt16LittleEndian( data.Slice( 12, 2 ), Flags );
			BinaryPrimitives.WriteUInt16LittleEndian( data.Slice( 14, 2 ), PayloadLength );
		}

		public static bool TryRead( ReadOnlySpan<byte> data, out NetworkPacketHeader header, out string error )
		{
			error = string.Empty;

			header = default;
			if ( data.Length < SIZE ) {
				error = "Packet length is invalid (smaller than header)";
				return false;
			}

			uint magic = BinaryPrimitives.ReadUInt32LittleEndian( data.Slice( 0 ) );
			if ( magic != MAGIC ) {
				error = "Packet header has incorrect magic";
				return false;
			}

			ushort protocolVersion = BinaryPrimitives.ReadUInt16LittleEndian( data.Slice( 4, 2 ) );
			if ( protocolVersion != PROTOCOL_VERSION ) {
				error = "Packet header has mismatched protocol version";
				return false;
			}

			NetworkPacketType type = (NetworkPacketType)BinaryPrimitives.ReadUInt16LittleEndian( data.Slice( 6, 2 ) );
			uint sequence = BinaryPrimitives.ReadUInt32LittleEndian( data.Slice( 8, 4 ) );
			ushort flags = BinaryPrimitives.ReadUInt16LittleEndian( data.Slice( 12, 2 ) );
			ushort payloadLength = BinaryPrimitives.ReadUInt16LittleEndian( data.Slice( 14, 2 ) );

			if ( data.Length - SIZE < payloadLength ) {
				error = "Packet length is less than header + total size (corruption)";
				return false;
			}

			header = new NetworkPacketHeader(
				type,
				sequence,
				flags,
				payloadLength
			);

			return true;
		}
	};
};
