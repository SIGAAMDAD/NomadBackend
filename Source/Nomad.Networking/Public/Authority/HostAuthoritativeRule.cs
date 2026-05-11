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

using Nomad.Networking.Messaging;

namespace Nomad.Networking.Authority
{
    public sealed class HostAuthoritativeRule : INetworkAuthorityRule
    {
        public NetworkAuthorityDecision Evaluate(
            in NetworkAuthorityContext context
        )
        {
            bool senderIsHost = context.Sender.Equals(context.HostPeer);

            switch (context.Kind)
            {
                case NetworkMessageKind.Rpc:
                    return EvaluateRpc(in context, senderIsHost);

                case NetworkMessageKind.Event:
                    return EvaluateEvent(in context, senderIsHost);

                case NetworkMessageKind.Command:
                case NetworkMessageKind.Input:
                    return EvaluateInput(in context, senderIsHost);

                case NetworkMessageKind.Snapshot:
                    return senderIsHost
                        ? NetworkAuthorityDecision.Allow
                        : NetworkAuthorityDecision.Deny;

                default:
                    return NetworkAuthorityDecision.Abstain;
            }
        }

        private static NetworkAuthorityDecision EvaluateRpc(
            in NetworkAuthorityContext context,
            bool senderIsHost
        )
        {
            if (context.Operation == NetworkAuthorityOperation.Send)
            {
                return NetworkAuthorityDecision.Allow;
            }

            if (context.Operation == NetworkAuthorityOperation.ExecuteRpc)
            {
                return NetworkAuthorityDecision.Allow;
            }

            return NetworkAuthorityDecision.Abstain;
        }

        private static NetworkAuthorityDecision EvaluateEvent(
            in NetworkAuthorityContext context,
            bool senderIsHost
        )
        {
            // In a host-authoritative model, clients should not create authoritative events.
            return senderIsHost
                ? NetworkAuthorityDecision.Allow
                : NetworkAuthorityDecision.Deny;
        }

        private static NetworkAuthorityDecision EvaluateInput(
            in NetworkAuthorityContext context,
            bool senderIsHost
        )
        {
            // Clients send input/commands to host. Host may also author commands locally.
            return NetworkAuthorityDecision.Allow;
        }
    }
}
