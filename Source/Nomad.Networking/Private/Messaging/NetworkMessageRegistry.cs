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
using Nomad.Networking.Messaging;

namespace Nomad.Networking.Private.Messaging
{
	/*
	===================================================================================

	NetworkMessageRegistry

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class NetworkMessageRegistry : INetworkMessageRegistry
	{
		private readonly Dictionary<Type, NetworkMessageInfo> _byType = new();
		private readonly Dictionary<ushort, NetworkMessageInfo> _byId = new();

		/*
		===============
		Register
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="id"></param>
		/// <param name="kind"></param>
		/// <exception cref="InvalidOperationException"></exception>
		public void Register<TMessage>( ushort id, NetworkMessageKind kind )
			where TMessage : struct
		{
			Type type = typeof( TMessage );
			if ( _byId.TryGetValue( id, out NetworkMessageInfo existing ) && existing.Type != type ) {
				throw new InvalidOperationException( $"Network message id {id} is already registered for {existing.Type.FullName}." );
			}
			if ( _byType.TryGetValue( type, out existing ) && existing.Id != id ) {
				throw new InvalidOperationException( $"Network message type {type.FullName} is already registered with id {existing.Id}." );
			}

			var info = new NetworkMessageInfo( id, type, kind );
			_byType[type] = info;
			_byId[id] = info;
		}

		/*
		===============
		TryGetId
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TMessage"></typeparam>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool TryGetId<TMessage>( out ushort id )
			where TMessage : struct
		{
			if ( _byType.TryGetValue( typeof( TMessage ), out NetworkMessageInfo info ) ) {
				id = info.Id;
				return true;
			}

			id = default;
			return false;
		}

		/*
		===============
		TryGetInfo
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <param name="info"></param>
		/// <returns></returns>
		public bool TryGetInfo( ushort id, out NetworkMessageInfo info )
		{
			return _byId.TryGetValue( id, out info );
		}
	}
}
