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

using Nomad.Core.Events;
using Nomad.Core.OnlineServices;
using Nomad.Networking.Messaging;

namespace Nomad.Networking.Events
{
    /// <summary>
    ///
    /// </summary>
    public interface INetworkEventBus
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        void Register<TArgs>(IGameEvent<TArgs> gameEvent)
            where TArgs : struct;

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        void Unregister<TArgs>()
            where TArgs : struct;

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="networkEvent"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        bool PublishToHost<TArgs>(IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable)
            where TArgs : struct;

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="peerId"></param>
        /// <param name="networkEvent"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        bool PublishToPeer<TArgs>(PeerId peerId, IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable)
            where TArgs : struct;

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <param name="networkEvent"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        bool PublishToAll<TArgs>(IGameEvent<TArgs> gameEvent, in TArgs payload, NetworkSendMode mode = NetworkSendMode.Reliable)
            where TArgs : struct;
    }
}
