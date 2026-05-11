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
using Nomad.Networking.ValueObjects;

namespace Nomad.Networking.Authority
{
    public readonly struct NetworkAuthorityContext
    {
        public readonly NetworkAuthorityOperation Operation;
        public readonly PeerId Sender;
        public readonly PeerId Target;
        public readonly PeerId LocalPeer;
        public readonly PeerId HostPeer;
        public readonly ushort MessageId;
        public readonly NetworkMessageKind Kind;
        public readonly NetworkTargetKind TargetKind;
        public readonly bool LocalIsHost;

        public NetworkAuthorityContext(
            NetworkAuthorityOperation operation,
            PeerId sender,
            PeerId target,
            PeerId localPeer,
            PeerId hostPeer,
            ushort messageId,
            NetworkMessageKind kind,
            NetworkTargetKind targetKind,
            bool localIsHost
        )
        {
            Operation = operation;
            Sender = sender;
            Target = target;
            LocalPeer = localPeer;
            HostPeer = hostPeer;
            MessageId = messageId;
            Kind = kind;
            TargetKind = targetKind;
            LocalIsHost = localIsHost;
        }
    }
}
