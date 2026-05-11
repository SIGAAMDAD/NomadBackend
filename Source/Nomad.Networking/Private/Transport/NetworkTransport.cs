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
using Nomad.Core.OnlineServices;
using Nomad.Networking.Authority;
using Nomad.Networking.Messaging;
using Nomad.Networking.Session;
using Nomad.Networking.Transport;

namespace Nomad.Networking.Private.Transport
{
	/*
	===================================================================================

	NetworkTransport

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class NetworkTransport : INetworkTransport
	{
		public bool IsActive => _sessionService.IsSessionActive;
		public bool IsHost => _sessionService.IsHost;
		public bool IsClient => _sessionService.IsClient;
		public PeerId LocalPeerId => _sessionService.CurrentSession?.LocalPeerId ?? default;
		public PeerId HostPeerId => _sessionService.CurrentSession?.HostPeerId ?? default;

		private readonly INetworkSessionService _sessionService;

		/*
		===============
		NetworkTransport
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="sessionService"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public NetworkTransport( INetworkSessionService sessionService )
		{
			_sessionService = sessionService ?? throw new ArgumentNullException( nameof( sessionService ) );
		}

		/*
		===============
		SendToHost
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="payload"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool SendToHost( ReadOnlySpan<byte> payload, NetworkSendMode mode )
		{
			if ( !_sessionService.IsSessionActive ) {
				return false;
			}

			_sessionService.SendToHost( payload, mode );
			return true;
		}

		/*
		===============
		SendToPeer
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="peerId"></param>
		/// <param name="payload"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool SendToPeer( PeerId peerId, ReadOnlySpan<byte> payload, NetworkSendMode mode )
		{
			if ( !_sessionService.IsSessionActive ) {
				return false;
			}

			_sessionService.SendToPeer( peerId, payload, mode );
			return true;
		}

		/*
		===============
		Broadcast
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="payload"></param>
		/// <param name="mode"></param>
		/// <returns></returns>
		public bool Broadcast( ReadOnlySpan<byte> payload, NetworkSendMode mode )
		{
			if ( !_sessionService.IsSessionActive ) {
				return false;
			}

			_sessionService.Broadcast( payload, mode );
			return true;
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
		public bool TryReceive( Span<byte> destination, out NetworkPacketInfo packet )
		{
			return _sessionService.TryReceive( destination, out packet );
		}
	}
}
