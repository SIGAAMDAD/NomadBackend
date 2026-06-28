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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Events;
using Nomad.Core.OnlineServices;
using Nomad.Networking.Authority;
using Nomad.Networking.Diagnostics;
using Nomad.Networking.Transport;

namespace Nomad.Networking.Tests.Support
{
    internal readonly struct TestNetworkPayload
    {
        public readonly int Value;
        public readonly short Code;

        public TestNetworkPayload(int value, short code)
        {
            Value = value;
            Code = code;
        }
    }

    internal readonly struct AlternateNetworkPayload
    {
        public readonly int Value;

        public AlternateNetworkPayload(int value)
        {
            Value = value;
        }
    }

    internal sealed class RecordingAuthority : INetworkAuthority
    {
        public readonly List<NetworkAuthorityContext> Contexts = new List<NetworkAuthorityContext>();
        public bool Decision = true;
        public NetworkAuthorityDecision DefaultDecision { get; set; } = NetworkAuthorityDecision.Allow;

        public void AddRule(INetworkAuthorityRule rule)
        {
        }

        public bool Evaluate(in NetworkAuthorityContext context)
        {
            Contexts.Add(context);
            return Decision;
        }
    }

    internal sealed class RecordingDiagnostics : INetworkDiagnostics
    {
        private uint _packetsSent;
        private uint _packetsReceived;
        private uint _bytesSent;
        private uint _bytesReceived;
        private uint _packetsDropped;
        private uint _deserializeFailures;
        private uint _unknownMessageIds;
        private uint _authorityRejects;

        public NetworkStats Stats => new NetworkStats(
            _packetsSent,
            _packetsReceived,
            _bytesSent,
            _bytesReceived,
            _packetsDropped,
            _deserializeFailures,
            _unknownMessageIds,
            _authorityRejects
        );

        public void Reset()
        {
            _packetsSent = 0;
            _packetsReceived = 0;
            _bytesSent = 0;
            _bytesReceived = 0;
            _packetsDropped = 0;
            _deserializeFailures = 0;
            _unknownMessageIds = 0;
            _authorityRejects = 0;
        }

        public void RecordPacketSent(int bytes)
        {
            _packetsSent++;
            _bytesSent += (uint)bytes;
        }

        public void RecordPacketReceived(int bytes)
        {
            _packetsReceived++;
            _bytesReceived += (uint)bytes;
        }

        public void RecordPacketDropped()
        {
            _packetsDropped++;
        }

        public void RecordDeserializeFailure()
        {
            _deserializeFailures++;
        }

        public void RecordUnknownMessageId()
        {
            _unknownMessageIds++;
        }

        public void RecordAuthorityReject()
        {
            _authorityRejects++;
        }
    }

    internal sealed class RecordingTransport : INetworkTransport
    {
        private readonly Queue<InboundPacket> _packets = new Queue<InboundPacket>();

        public readonly List<SentPacket> SentToHost = new List<SentPacket>();
        public readonly List<SentPacket> SentToPeers = new List<SentPacket>();
        public readonly List<SentPacket> Broadcasts = new List<SentPacket>();

        public bool IsActive { get; set; } = true;
        public bool IsHost { get; set; }
        public bool IsClient { get; set; } = true;
        public PeerId LocalPeerId { get; set; } = new PeerId(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        public PeerId HostPeerId { get; set; } = new PeerId(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
        public bool SendResult { get; set; } = true;

        public void EnqueueReceive(PeerId from, byte[] payload, NetworkSendMode mode = NetworkSendMode.Reliable)
        {
            _packets.Enqueue(new InboundPacket(from, payload, mode));
        }

        public bool SendToHost(ReadOnlySpan<byte> payload, NetworkSendMode mode)
        {
            SentToHost.Add(new SentPacket(HostPeerId, payload.ToArray(), mode));
            return SendResult;
        }

        public bool SendToPeer(PeerId peerId, ReadOnlySpan<byte> payload, NetworkSendMode mode)
        {
            SentToPeers.Add(new SentPacket(peerId, payload.ToArray(), mode));
            return SendResult;
        }

        public bool Broadcast(ReadOnlySpan<byte> payload, NetworkSendMode mode)
        {
            Broadcasts.Add(new SentPacket(default, payload.ToArray(), mode));
            return SendResult;
        }

        public bool TryReceive(Span<byte> destination, out NetworkPacketInfo packet)
        {
            if (_packets.Count == 0)
            {
                packet = default;
                return false;
            }

            InboundPacket inbound = _packets.Dequeue();
            inbound.Payload.AsSpan().CopyTo(destination);
            packet = new NetworkPacketInfo(inbound.From, inbound.Payload.Length, inbound.Mode);
            return true;
        }

        internal readonly struct SentPacket
        {
            public readonly PeerId Peer;
            public readonly byte[] Payload;
            public readonly NetworkSendMode Mode;

            public SentPacket(PeerId peer, byte[] payload, NetworkSendMode mode)
            {
                Peer = peer;
                Payload = payload;
                Mode = mode;
            }
        }

        private readonly struct InboundPacket
        {
            public readonly PeerId From;
            public readonly byte[] Payload;
            public readonly NetworkSendMode Mode;

            public InboundPacket(PeerId from, byte[] payload, NetworkSendMode mode)
            {
                From = from;
                Payload = payload;
                Mode = mode;
            }
        }
    }

    internal sealed class FailingSerializer : INetworkSerializer
    {
        public bool SerializeResult = true;
        public bool DeserializeResult = true;

        public int GetMaxSize<T>() where T : struct
        {
            return 4;
        }

        public bool Serialize<T>(in T value, Span<byte> destination, out int bytesWritten) where T : struct
        {
            bytesWritten = SerializeResult ? 4 : 0;
            if (SerializeResult && destination.Length >= 4)
            {
                destination[0] = 1;
                destination[1] = 2;
                destination[2] = 3;
                destination[3] = 4;
                return true;
            }

            return false;
        }

        public bool Deserialize<T>(ReadOnlySpan<byte> source, out T value) where T : struct
        {
            value = default;
            return DeserializeResult;
        }
    }

    internal sealed class TestGameEvent<TArgs> : IGameEvent<TArgs>
        where TArgs : struct
    {
        private int _subscriberCount;

        public string DebugName { get; set; } = "TestEvent";
        public string NameSpace { get; set; } = "Nomad.Networking.Tests";
        public int Id { get; set; } = 1;
        public int PublishCallCount { get; private set; }
        public TArgs PublishedPayload { get; private set; }

#if EVENT_DEBUG
        public int SubscriberCount => _subscriberCount;
        public long PublishCount => PublishCallCount;
        public DateTime LastPublishTime { get; private set; }
        public TArgs LastPayload => PublishedPayload;
#endif

        public event EventCallback<TArgs> OnPublished = delegate { };
        public event AsyncEventCallback<TArgs> OnPublishedAsync = delegate { return Task.CompletedTask; };

        public Task PublishAsync(TArgs eventArgs, CancellationToken ct = default)
        {
            PublishedPayload = eventArgs;
            PublishCallCount++;
#if EVENT_DEBUG
            LastPublishTime = DateTime.UtcNow;
#endif
            return OnPublishedAsync.Invoke(eventArgs, ct);
        }

        public void Publish(in TArgs eventArgs)
        {
            PublishedPayload = eventArgs;
            PublishCallCount++;
#if EVENT_DEBUG
            LastPublishTime = DateTime.UtcNow;
#endif
            OnPublished.Invoke(in eventArgs);
        }

        public ISubscriptionHandle SubscribeAsync(AsyncEventCallback<TArgs> asyncCallback)
        {
            OnPublishedAsync += asyncCallback;
            _subscriberCount++;
            return new DelegateSubscription(() =>
            {
                OnPublishedAsync -= asyncCallback;
                _subscriberCount--;
            });
        }

        public ISubscriptionHandle Subscribe(EventCallback<TArgs> callback)
        {
            OnPublished += callback;
            _subscriberCount++;
            return new DelegateSubscription(() =>
            {
                OnPublished -= callback;
                _subscriberCount--;
            });
        }

        public void UnsubscribeAsync(AsyncEventCallback<TArgs> asyncCallback)
        {
            OnPublishedAsync -= asyncCallback;
        }

        public void Unsubscribe(EventCallback<TArgs> callback)
        {
            OnPublished -= callback;
        }

        public void Dispose()
        {
        }

        private sealed class DelegateSubscription : ISubscriptionHandle
        {
            private readonly Action _dispose;

            public DelegateSubscription(Action dispose)
            {
                _dispose = dispose;
            }

            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }

                IsDisposed = true;
                _dispose();
            }
        }
    }
}
