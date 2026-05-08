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

using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Util;

namespace Nomad.Save.Private.Serialization.FieldSerializers
{
	/*
	===================================================================================
	
	ByteSerializer
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class ByteSerializer : IFieldSerializer<byte>
	{
		public AnyType FieldType => AnyType.UInt8;

		public void Serialize( IWriteStream stream, in Any value ) => stream.WriteUInt8( value.GetPrimitiveValue<byte>() );
		public Any Deserialize( IReadStream stream ) => new Any( stream.ReadUInt8() );
	};
};
