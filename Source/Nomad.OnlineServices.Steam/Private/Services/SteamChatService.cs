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
using System.Threading.Tasks;
using Nomad.Core.Events;
using Nomad.Core.OnlineServices;
using Nomad.OnlineServices.Steam.Private.Repositories;
using Nomad.OnlineServices.Steam.Private.ValueObjects;
using Steamworks;

namespace Nomad.OnlineServices.Steam.Private.Services
{
	/*
	===================================================================================

	SteamChatService

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class SteamChatService : IChatService
	{
		public IGameEvent<ChatMessageReceivedEventArgs> ChatMessageReceived => _chatMessageReceived;
		private readonly IGameEvent<ChatMessageReceivedEventArgs> _chatMessageReceived = default;

		private readonly Callback<LobbyChatMsg_t> _chatMessage;

		private readonly object _lock = new object();
		private readonly byte[] _messageBuffer;
		private SteamLobbyData? _currentLobby = null;

		private readonly ISubscriptionHandle _lobbyJoined;

		private readonly SteamLobbyRepository _lobbyRepository;

		public SteamChatService( SteamLobbyRepository lobbyRepository, IGameEventRegistryService eventFactory )
		{
			_chatMessage = Callback<LobbyChatMsg_t>.Create( OnChatMessageReceived );

			_lobbyRepository = lobbyRepository ?? throw new ArgumentNullException( nameof( lobbyRepository ) );

			_messageBuffer = ArrayPool<byte>.Shared.Rent( 4096 );

			_lobbyJoined = eventFactory
				.GetEvent<LobbyJoinedResultEventArgs>(
					LobbyJoinedResultEventArgs.Name,
					LobbyJoinedResultEventArgs.NameSpace
				)
				.Subscribe( OnLobbyJoined );
		}

		private void OnLobbyJoined( in LobbyJoinedResultEventArgs args )
		{
			if ( !_lobbyRepository.TryGetLobby( args.Id, out _currentLobby ) ) {
			}
		}

		private void OnChatMessageReceived( LobbyChatMsg_t pCallback )
		{
			_chatMessageReceived.Publish(
				new ChatMessageReceivedEventArgs(
				)
			);
		}

		public void Dispose()
		{
			throw new System.NotImplementedException();
		}

		public Task SendMessageAsync( string message, ChatMessageScope scope )
		{
			throw new System.NotImplementedException();
		}
	};
};
