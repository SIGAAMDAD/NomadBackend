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
using Nomad.Core.OnlineServices;
using Nomad.Networking.Authority;
using Nomad.Networking.Diagnostics;
using Nomad.Networking.Messaging;
using Nomad.Networking.Private.Messaging;
using Nomad.Networking.Rpc;
using Nomad.Networking.Transport;
using Nomad.Networking.ValueObjects;

namespace Nomad.Networking.Private.Rpc
{
	/*
	===================================================================================

	NetworkRpcBus

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class NetworkRpcBus : INetworkRpcBus
	{
		private readonly INetworkMessageRegistry _messageRegistry;
		private readonly INetworkSerializer _serializer;
		private readonly INetworkTransport _transport;
		private readonly INetworkAuthority _authority;
		private readonly INetworkDiagnostics? _diagnostics;

		private readonly Dictionary<ushort, IRpcInvoker> _invokersById = new();
		private readonly Dictionary<Type, ushort> _messageIdByType = new();
		private readonly Queue<InboundRpc> _pending = new();

		/*
		===============
		NetworkRpcBus
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="registry"></param>
		/// <param name="serializer"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public NetworkRpcBus( INetworkMessageRegistry registry, INetworkSerializer serializer )
		{
			_messageRegistry = registry ?? throw new ArgumentNullException( nameof( registry ) );
			_serializer = serializer ?? throw new ArgumentNullException( nameof( serializer ) );
		}

		/*
		===============
		NetworkRpcBus
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
		public NetworkRpcBus(
			INetworkMessageRegistry registry,
			INetworkSerializer serializer,
			INetworkTransport transport,
			INetworkAuthority authority,
			INetworkDiagnostics? diagnostics = null
		)
			: this( registry, serializer )
		{
			_transport = transport ?? throw new ArgumentNullException( nameof( transport ) );
			_authority = authority ?? throw new ArgumentNullException( nameof( authority ) );
			_diagnostics = diagnostics;
		}

		/*
		===============
		Broadcast
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TRpc"></typeparam>
		/// <param name="rpc"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool Broadcast<TRpc>( in TRpc rpc, NetworkSendMode mode = NetworkSendMode.Reliable )
			where TRpc : struct
		{
			if ( !TrySerialize( in rpc, NetworkTargetKind.All, out PooledSendBuffer buffer ) ) {
				return false;
			}

			try {
				bool sent = _transport.Broadcast( buffer.Span, mode );
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
		Pump
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void Pump()
		{
			while ( _pending.Count > 0 ) {
				InboundRpc rpc = _pending.Dequeue();
				Dispatch( rpc );
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
		/// <typeparam name="TRpc"></typeparam>
		/// <param name="handler"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public void Register<TRpc>( NetworkRpcHandler<TRpc> handler )
			where TRpc : struct
		{
			if ( !_messageRegistry.TryGetId<TRpc>( out ushort id ) ) {
				throw new InvalidOperationException(
					$"RPC type '{typeof( TRpc ).FullName}' is not registered."
				);
			}

			_invokersById[id] = new RpcInvoker<TRpc>(
				handler,
				_serializer,
				_transport,
				_diagnostics
			);
			_messageIdByType[typeof( TRpc )] = id;
		}

		/*
		===============
		SendToHost
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TRpc"></typeparam>
		/// <param name="rpc"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool SendToHost<TRpc>( in TRpc rpc, NetworkSendMode mode = NetworkSendMode.Reliable )
			where TRpc : struct
		{
			if ( !TrySerialize( in rpc, NetworkTargetKind.Host, out PooledSendBuffer buffer ) ) {
				return false;
			}

			try {
				bool sent = _transport.SendToHost( buffer.Span, mode );
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
		SendToPeer
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TRpc"></typeparam>
		/// <param name="peerId"></param>
		/// <param name="rpc"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool SendToPeer<TRpc>( PeerId peerId, in TRpc rpc, NetworkSendMode mode = NetworkSendMode.Reliable )
			where TRpc : struct
		{
			if ( _transport == null ) {
				return false;
			}

			if ( !TrySerialize( in rpc, NetworkTargetKind.Peer, out PooledSendBuffer buffer ) ) {
				return false;
			}

			try {
				bool sent = _transport.SendToPeer( peerId, buffer.Span, mode );
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
		Unregister
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TRpc"></typeparam>
		public void Unregister<TRpc>()
			where TRpc : struct
		{
			if ( _messageIdByType.TryGetValue( typeof( TRpc ), out ushort id ) ) {
				_messageIdByType.Remove( typeof( TRpc ) );
				_invokersById.Remove( id );
			}
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

			_pending.Enqueue( new InboundRpc(
				sender,
				messageId,
				rented,
				payload.Length
			) );
		}

		/*
		===============
		TrySerialize
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="TRpc"></typeparam>
		/// <param name="rpc"></param>
		/// <param name="target"></param>
		/// <param name="buffer"></param>
		/// <returns></returns>
		private bool TrySerialize<TRpc>( in TRpc rpc, NetworkTargetKind target, out PooledSendBuffer sendBuffer )
			where TRpc : struct
		{
			sendBuffer = default;

			if ( !_messageRegistry.TryGetId<TRpc>( out ushort id ) ) {
				return false;
			}

			var context = new NetworkAuthorityContext(
				NetworkAuthorityOperation.Send,
				_transport.LocalPeerId,
				default,
				_transport.LocalPeerId,
				_transport.HostPeerId,
				id,
				NetworkMessageKind.Rpc,
				target,
				_transport.IsHost
			);

			if ( !_authority.Evaluate( in context ) ) {
				_diagnostics?.RecordAuthorityReject();
				return false;
			}

			int maxSize = NetworkMessageEnvelope.HEADER_SIZE + _serializer.GetMaxSize<TRpc>();
			byte[] rented = ArrayPool<byte>.Shared.Rent( maxSize );

			NetworkMessageEnvelope.Write(
				id,
				rented
			);

			if ( !_serializer.Serialize(
				in rpc,
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
		private void Dispatch( InboundRpc inbound )
		{
			try {
				if ( _invokersById.TryGetValue( inbound.MessageId, out IRpcInvoker? invoker ) ) {
					_diagnostics?.RecordUnknownMessageId();
					return;
				}

				var context = new NetworkAuthorityContext(
					operation: NetworkAuthorityOperation.ExecuteRpc,
					sender: inbound.Sender,
					target: _transport.LocalPeerId,
					localPeer: _transport.LocalPeerId,
					hostPeer: _transport.HostPeerId,
					messageId: inbound.MessageId,
					kind: NetworkMessageKind.Rpc,
					targetKind: NetworkTargetKind.Peer,
					localIsHost: _transport.IsHost
				);
				if ( !_authority.Evaluate( in context ) ) {
					_diagnostics?.RecordAuthorityReject();
					return;
				}

				invoker.Dispatch( in inbound );
			} finally {
				inbound.Dispose();
			}
		}
	};
};
