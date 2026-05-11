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
using System.Collections.Generic;
using Nomad.Core.Events;
using Nomad.Core.OnlineServices;
using Nomad.Networking.Authority;
using Nomad.Networking.Diagnostics;
using Nomad.Networking.Events;
using Nomad.Networking.Extensions;
using Nomad.Networking.Messaging;
using Nomad.Networking.Private.Messaging;
using Nomad.Networking.Transport;
using Nomad.Networking.ValueObjects;

namespace Nomad.Networking.Private.Events
{
	/*
	===================================================================================

	NomadEventBus

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class NetworkEventBus : INetworkEventBus
	{
		private readonly INetworkMessageRegistry _messageRegistry;
		private readonly INetworkSerializer _serializer;
		private readonly INetworkTransport _transport;
		private readonly INetworkAuthority _authority;
		private readonly INetworkDiagnostics? _diagnostics;

		private readonly Dictionary<ushort, IEventInvoker> _invokersById = new();
		private readonly Queue<InboundEvent> _pending = new();

		/*
		===============
		NetworkEventBus
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="registry"></param>
		/// <param name="serializer"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public NetworkEventBus( INetworkMessageRegistry registry, INetworkSerializer serializer )
		{
			_messageRegistry = registry ?? throw new ArgumentNullException( nameof( registry ) );
			_serializer = serializer ?? throw new ArgumentNullException( nameof( serializer ) );

			GameEventExtensions.Initialize( this );
		}

		/*
		===============
		NetworkEventBus
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="registry"></param>
		/// <param name="serializer"></param>
		/// <param name="transport"></param>
		/// <param name="authority"></param>
		/// <param name="diagnostics"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public NetworkEventBus(
			INetworkMessageRegistry registry,
			INetworkSerializer serializer,
			INetworkTransport transport,
			INetworkAuthority authority,
			INetworkDiagnostics? diagnostics = null
		)
			: this( registry, serializer )
		{
			_authority = authority ?? throw new ArgumentNullException( nameof( authority ) );
			_transport = transport ?? throw new ArgumentNullException( nameof( transport ) );
			_diagnostics = diagnostics;
		}

		/*
		===============
		PublishToAll
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="gameEvent"></param>
		/// <param name="payload"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool PublishToAll<TArgs>( IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable )
			where TArgs : struct
		{
			Register( gameEvent );
			if ( !TrySerialize( in payload, NetworkTargetKind.All, out PooledSendBuffer buffer ) ) {
				return false;
			}

			try {
				bool sent = _transport.Broadcast( buffer.Buffer, mode );
				if ( sent ) {
					_diagnostics?.RecordPacketSent( buffer.Length );
				}
				return sent;
			} finally {
				buffer.Dispose();
			}
		}

		/*
		===============
		PublishToHost
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="gameEvent"></param>
		/// <param name="payload"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool PublishToHost<TArgs>( IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable )
			where TArgs : struct
		{
			Register( gameEvent );
			if ( !TrySerialize( in payload, NetworkTargetKind.Host, out PooledSendBuffer buffer ) ) {
				return false;
			}

			try {
				bool sent = _transport.SendToHost( buffer.Buffer, mode );
				if ( sent ) {
					_diagnostics?.RecordPacketSent( buffer.Length );
				}
				return sent;
			} finally {
				buffer.Dispose();
			}
		}

		/*
		===============
		PublishToPeer
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="peerId"></param>
		/// <param name="gameEvent"></param>
		/// <param name="payload"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool PublishToPeer<TArgs>( PeerId peerId, IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable )
			where TArgs : struct
		{
			Register( gameEvent );
			if ( !TrySerialize( in payload, NetworkTargetKind.Peer, out PooledSendBuffer buffer ) ) {
				return false;
			}

			try {
				bool sent = _transport.SendToPeer( peerId, buffer.Buffer, mode );
				if ( sent ) {
					_diagnostics?.RecordPacketSent( buffer.Length );
				}
				return sent;
			} finally {
				buffer.Dispose();
			}
		}

		/*
		===============
		Register
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="gameEvent"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public void Register<TArgs>( IGameEvent<TArgs> gameEvent )
			where TArgs : struct
		{
			if ( !_messageRegistry.TryGetId<TArgs>( out ushort id ) ) {
				throw new InvalidOperationException(
					$"Network event type '{typeof( TArgs ).FullName}' is not registered."
				);
			}

			_invokersById[id] = new EventInvoker<TArgs>(
				gameEvent,
				_serializer,
				_diagnostics
			);
		}

		/*
		===============
		Unregister
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		public void Unregister<TArgs>()
			where TArgs : struct
		{
			if ( !_messageRegistry.TryGetId<TArgs>( out ushort id ) ) {
				throw new InvalidOperationException(
					$"Network event type '{typeof( TArgs ).FullName}' is not registered."
				);
			}
			_invokersById.Remove( id );
		}

		/*
		===============
		Enqueue
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="messageId"></param>
		/// <param name="payload"></param>
		internal void Enqueue( PeerId sender, ushort messageId, ReadOnlySpan<byte> payload )
		{
			byte[] rented = ArrayPool<byte>.Shared.Rent( payload.Length );
			payload.CopyTo( rented );

			_pending.Enqueue( new InboundEvent(
				sender,
				messageId,
				rented,
				payload.Length
			) );
		}

		/*
		===============
		Pump
		===============
		*/
		/// <summary>
		///
		/// </summary>
		internal void Pump()
		{
			while ( _pending.Count > 0 ) {
				Dispatch( _pending.Dequeue() );
			}
		}

		/*
		===============
		TrySerialize
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TArgs"></typeparam>
		/// <param name="payload"></param>
		/// <param name="sendBuffer"></param>
		/// <returns></returns>
		private bool TrySerialize<TArgs>( in TArgs payload, NetworkTargetKind target, out PooledSendBuffer sendBuffer )
			where TArgs : struct
		{
			sendBuffer = default;

			if ( !_messageRegistry.TryGetId<TArgs>( out ushort id ) ) {
				return false;
			}

			var context = new NetworkAuthorityContext(
				NetworkAuthorityOperation.Send,
				_transport.LocalPeerId,
				default,
				_transport.LocalPeerId,
				_transport.HostPeerId,
				id,
				NetworkMessageKind.Event,
				target,
				_transport.IsHost
			);

			if ( !_authority.Evaluate( in context ) ) {
				_diagnostics?.RecordAuthorityReject();
				return false;
			}

			int maxSize = NetworkMessageEnvelope.HEADER_SIZE + _serializer.GetMaxSize<TArgs>();
			byte[] rented = ArrayPool<byte>.Shared.Rent( maxSize );

			NetworkMessageEnvelope.Write(
				id,
				rented
			);

			if ( !_serializer.Serialize(
				in payload,
				rented.AsSpan( NetworkMessageEnvelope.HEADER_SIZE ),
				out int bodyBytesWritten
			) ) {
				ArrayPool<byte>.Shared.Return( rented );
				return false;
			}

			sendBuffer = new PooledSendBuffer(
				rented,
				NetworkMessageEnvelope.HEADER_SIZE + bodyBytesWritten
			);

			return true;
		}

		/*
		===============
		Dispatch
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="inbound"></param>
		private void Dispatch( InboundEvent inbound )
		{
			try {
				if ( !_invokersById.TryGetValue( inbound.MessageId, out IEventInvoker? invoker ) ) {
					_diagnostics?.RecordUnknownMessageId();
					return;
				}

				invoker.Dispatch( in inbound );
			} finally {
				inbound.Dispose();
			}
		}
	};
};
