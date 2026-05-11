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

using Nomad.Core.OnlineServices;
using Nomad.Networking.Messaging;

namespace Nomad.Networking.Rpc
{
    /// <summary>
    ///
    /// </summary>
    public interface INetworkRpcBus
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TRpc"></typeparam>
        /// <param name="handler"></param>
        void Register<TRpc>(NetworkRpcHandler<TRpc> handler)
            where TRpc : struct;

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TRpc"></typeparam>
        void Unregister<TRpc>()
            where TRpc : struct;

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TRpc"></typeparam>
        /// <param name="rpc"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        bool SendToHost<TRpc>(in TRpc rpc, NetworkSendMode mode = NetworkSendMode.Reliable)
            where TRpc : struct;

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TRpc"></typeparam>
        /// <param name="peerId"></param>
        /// <param name="rpc"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        bool SendToPeer<TRpc>(PeerId peerId, in TRpc rpc, NetworkSendMode mode = NetworkSendMode.Reliable)
            where TRpc : struct;

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TRpc"></typeparam>
        /// <param name="rpc"></param>
        /// <param name="mode"></param>
        /// <returns></returns>
        bool Broadcast<TRpc>(in TRpc rpc, NetworkSendMode mode = NetworkSendMode.Reliable)
            where TRpc : struct;

        /// <summary>
        ///
        /// </summary>
        void Pump();
    }
}
