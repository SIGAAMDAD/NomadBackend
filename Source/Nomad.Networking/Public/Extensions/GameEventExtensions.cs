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
using System.Runtime.CompilerServices;
using Nomad.Core.Events;
using Nomad.Core.Exceptions;
using Nomad.Core.OnlineServices;
using Nomad.Networking.Events;
using Nomad.Networking.Messaging;

namespace Nomad.Networking.Extensions
{
    public static class GameEventExtensions
    {
        private static INetworkEventBus EventBus => _eventBus ?? throw new SubsystemNotInitializedException();
        private static INetworkEventBus _eventBus = null;

        internal static void Initialize(INetworkEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NetworkRegister<TArgs>(this IGameEvent<TArgs> gameEvent)
            where TArgs : struct
        {
            EventBus.Register(gameEvent);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NetworkUnregister<TArgs>(this IGameEvent<TArgs> gameEvent)
            where TArgs : struct
        {
            EventBus.Unregister<TArgs>();
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="payload"></param>
        /// <param name="mode"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NetworkPublishToHost<TArgs>(this IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable)
            where TArgs : struct
        {
            return EventBus.PublishToHost(gameEvent, in payload, mode);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="peer"></param>
        /// <param name="payload"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NetworkPublishToPeer<TArgs>(this IGameEvent<TArgs> gameEvent, PeerId peer, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable)
            where TArgs : struct
        {
            return EventBus.PublishToPeer(peer, gameEvent, in payload, mode);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="payload"></param>
        /// <param name="mode"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NetworkPublishToAll<TArgs>(this IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable)
            where TArgs : struct
        {
            return EventBus.PublishToAll(gameEvent, in payload, mode);
        }
    }
}
