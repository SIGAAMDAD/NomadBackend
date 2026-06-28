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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Core.OnlineServices;
using Nomad.Core.ServiceRegistry;
using Nomad.Core.ServiceRegistry.Interfaces;
using Nomad.Core.ServiceRegistry.Services;
using Nomad.Networking.Session;

namespace Nomad.Networking.Tests.Support
{
    internal sealed class RecordingLobbyService : ILobbyService
    {
        private readonly List<LobbyMemberInfo> _members = new List<LobbyMemberInfo>();

        public bool CreateSucceeds = true;
        public bool JoinSucceeds = true;
        public bool LeaveSucceeds = true;
        public LobbyFailureReason FailureReason = LobbyFailureReason.Unknown;
        public LobbyInfo? LobbyToCreate;
        public LobbyInfo? LobbyToJoin;
        public LobbyCreateInfo? LastCreateInfo;
        public LobbyId LastJoinedLobbyId;
        public bool RefreshEnabled;

        public bool IsInLobby => Current != null;
        public bool IsLobbyLeader { get; set; }
        public LobbyInfo? Current { get; set; }

        public IGameEvent<LobbyJoinedResultEventArgs> LobbyJoined { get; } = new TestGameEvent<LobbyJoinedResultEventArgs>();
        public IGameEvent<LobbyLeaveResultEventArgs> LobbyLeft { get; } = new TestGameEvent<LobbyLeaveResultEventArgs>();
        public IGameEvent<LobbyStartResultEventArgs> LobbyStarted { get; } = new TestGameEvent<LobbyStartResultEventArgs>();

        public void SetMembers(params LobbyMemberInfo[] members)
        {
            _members.Clear();
            _members.AddRange(members);
        }

        public Task<LobbyCreateResult> CreateLobby(LobbyCreateInfo lobbyInfo, CancellationToken ct = default)
        {
            LastCreateInfo = lobbyInfo;
            if (!CreateSucceeds)
            {
                return Task.FromResult(LobbyCreateResult.Failure(FailureReason));
            }

            Current = LobbyToCreate ?? new LobbyInfo
            {
                Id = new LobbyId(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001")),
                MaxPlayers = Math.Max(1, lobbyInfo.MaxPlayers),
                PlayerCount = _members.Count
            };
            return Task.FromResult(LobbyCreateResult.Created(Current.Id));
        }

        public Task<LobbyJoinResult> JoinLobby(LobbyId lobbyId, CancellationToken ct = default)
        {
            LastJoinedLobbyId = lobbyId;
            if (!JoinSucceeds)
            {
                return Task.FromResult(LobbyJoinResult.Failure(FailureReason));
            }

            Current = LobbyToJoin ?? new LobbyInfo
            {
                Id = lobbyId,
                MaxPlayers = Math.Max(1, _members.Count),
                PlayerCount = _members.Count
            };
            return Task.FromResult(LobbyJoinResult.Joined(Current));
        }

        public Task<bool> LeaveLobby(CancellationToken ct = default)
        {
            Current = null;
            return Task.FromResult(LeaveSucceeds);
        }

        public IReadOnlyList<LobbyMemberInfo> GetMembers()
        {
            return _members;
        }

        public void SetLobbyRefresh(bool active)
        {
            RefreshEnabled = active;
        }

        public void Dispose()
        {
        }
    }

    internal sealed class RecordingNetDriver : INetDriver
    {
        private readonly Queue<ReceivedPacket> _receiveQueue = new Queue<ReceivedPacket>();

        public bool ListenResult = true;
        public bool ConnectResult = true;
        public bool AcceptResult = true;
        public bool CloseResult = true;
        public bool SendResult = true;
        public int ListenCallCount;
        public int ConnectCallCount;
        public int CloseAllCallCount;
        public int SendCallCount;
        public PeerId LastConnectedPeer;
        public PeerId LastSentPeer;
        public byte[] LastSentPayload = Array.Empty<byte>();
        public NetworkSendMode LastSendMode;
        public string? LastCloseAllReason;

        public bool IsListening { get; set; }
        public bool IsInitialized { get; set; } = true;

        public event Action<NetConnection> ConnectionRequested = delegate { };
        public event Action<NetConnection> ConnectionEstablished = delegate { };
        public event Action<NetConnection> ConnectionClosed = delegate { };

        public bool Listen(int virtualPort = 0)
        {
            ListenCallCount++;
            IsListening = ListenResult;
            return ListenResult;
        }

        public bool Connect(PeerId peerId, int virtualPort = 0)
        {
            ConnectCallCount++;
            LastConnectedPeer = peerId;
            return ConnectResult;
        }

        public bool Accept(PeerId peerId)
        {
            return AcceptResult;
        }

        public bool Close(PeerId peerId, string reason)
        {
            return CloseResult;
        }

        public void CloseAll(string reason)
        {
            CloseAllCallCount++;
            LastCloseAllReason = reason;
        }

        public bool Send(PeerId peerId, ReadOnlySpan<byte> payload, NetworkSendMode mode)
        {
            SendCallCount++;
            LastSentPeer = peerId;
            LastSentPayload = payload.ToArray();
            LastSendMode = mode;
            return SendResult;
        }

        public void EnqueueReceive(PeerId peerId, byte[] payload, NetworkSendMode mode)
        {
            _receiveQueue.Enqueue(new ReceivedPacket(peerId, payload, mode));
        }

        public bool TryReceive(Span<byte> destination, out NetworkPacketInfo packet)
        {
            if (_receiveQueue.Count == 0)
            {
                packet = default;
                return false;
            }

            ReceivedPacket received = _receiveQueue.Dequeue();
            received.Payload.AsSpan().CopyTo(destination);
            packet = new NetworkPacketInfo(received.PeerId, received.Payload.Length, received.Mode);
            return true;
        }

        public bool TryGetConnection(PeerId peerId, out NetConnection connection)
        {
            connection = new NetConnection(peerId, NetworkConnectionState.Connected);
            return peerId.IsValid;
        }

        public void RaiseConnectionRequested(NetConnection connection)
        {
            ConnectionRequested(connection);
        }

        public void RaiseConnectionEstablished(NetConnection connection)
        {
            ConnectionEstablished(connection);
        }

        public void RaiseConnectionClosed(NetConnection connection)
        {
            ConnectionClosed(connection);
        }

        public void Dispose()
        {
        }

        private readonly struct ReceivedPacket
        {
            public readonly PeerId PeerId;
            public readonly byte[] Payload;
            public readonly NetworkSendMode Mode;

            public ReceivedPacket(PeerId peerId, byte[] payload, NetworkSendMode mode)
            {
                PeerId = peerId;
                Payload = payload;
                Mode = mode;
            }
        }
    }

    internal sealed class RecordingEventRegistry : IGameEventRegistryService
    {
        private readonly Dictionary<Type, object> _events = new Dictionary<Type, object>();

        public ISubscriptionGroup GetGroup(string name)
        {
            throw new NotSupportedException();
        }

        public void ClearAllGroups()
        {
        }

        public void RemoveGroup(ISubscriptionGroup group)
        {
        }

        public IGameEvent<TArgs> GetEvent<TArgs>(string name, string nameSpace, EventFlags flags = EventFlags.Default)
            where TArgs : struct
        {
            if (!_events.TryGetValue(typeof(TArgs), out object? value))
            {
                value = new TestGameEvent<TArgs> { DebugName = name, NameSpace = nameSpace };
                _events.Add(typeof(TArgs), value);
            }

            return (IGameEvent<TArgs>)value;
        }

        public bool TryGetEvent<TArgs>(string name, string nameSpace, out IGameEvent<TArgs>? gameEvent)
            where TArgs : struct
        {
            if (_events.TryGetValue(typeof(TArgs), out object? value))
            {
                gameEvent = (IGameEvent<TArgs>)value;
                return true;
            }

            gameEvent = null;
            return false;
        }

        public bool TryRemoveEvent<TArgs>(string nameSpace, string name)
            where TArgs : struct
        {
            return _events.Remove(typeof(TArgs));
        }

        public void ClearEventsInNamespace(string nameSpace)
        {
            _events.Clear();
        }

        public void ClearAllEvents()
        {
            _events.Clear();
        }

        public IReadOnlyCollection<IGameEvent> GetAllEvents()
        {
            return _events.Values.Cast<IGameEvent>().ToArray();
        }

        public void Dispose()
        {
        }
    }

    internal sealed class RecordingLoggerService : ILoggerService
    {
        public readonly List<string> Lines = new List<string>();
        public readonly List<RecordingLoggerCategory> Categories = new List<RecordingLoggerCategory>();

        public void PrintLine(string message) => Lines.Add(message);
        public void PrintDebug(string message) => Lines.Add(message);
        public void PrintWarning(string message) => Lines.Add(message);
        public void PrintError(string message) => Lines.Add(message);
        public void Clear() => Lines.Clear();
        public void InitConfig(ICVarSystemService cvarSystem) { }
        public void AddSink(ILoggerSink sink) { }

        public ILoggerCategory CreateCategory(string name, LogLevel level, bool enabled)
        {
            var category = new RecordingLoggerCategory(name, level, enabled);
            Categories.Add(category);
            return category;
        }

        public void Dispose()
        {
        }
    }

    internal sealed class RecordingLoggerCategory : ILoggerCategory
    {
        public readonly List<string> Lines = new List<string>();

        public RecordingLoggerCategory(string name, LogLevel level, bool enabled)
        {
            Name = name;
            Level = level;
            Enabled = enabled;
        }

        public string Name { get; }
        public LogLevel Level { get; }
        public bool Enabled { get; set; }

        public void PrintLine(string message) => Lines.Add(message);
        public void PrintWarning(string message) => Lines.Add(message);
        public void PrintError(string message) => Lines.Add(message);
        public void PrintDebug(string message) => Lines.Add(message);
        public void AddSink(ILoggerSink sink) { }
        public void RemoveSink(ILoggerSink sink) { }
        public void Dispose() { }
    }

    internal sealed class RecordingOnlinePlatformService : IOnlinePlatformService
    {
        public OnlinePlatform Platform { get; set; }
        public string PlatformName { get; set; } = "Test";
        public bool IsAvailable { get; set; } = true;
        public IStatsService Stats { get; set; } = null!;
        public ILobbyService Lobbies { get; set; } = null!;
        public INetDriver NetDriver { get; set; } = null!;
        public IAchievementService Achievements { get; set; } = null!;
        public ICloudStorageService CloudStorage { get; set; } = null!;
        public IUserAvatarService AvatarService { get; set; } = null!;
        public void Frame() { }
        public void Dispose() { }
    }

    internal sealed class RecordingServiceRegistry : IServiceRegistry
    {
        private readonly Dictionary<Type, object> _instances = new Dictionary<Type, object>();

        public IReadOnlyDictionary<Type, object> Instances => _instances;

        public IServiceRegistry Register<TService, TImplementation>(ServiceLifetime lifetime)
            where TService : class
            where TImplementation : class, TService
        {
            return this;
        }

        public IServiceRegistry AddSingleton<TService>(TService instance)
            where TService : class
        {
            _instances[typeof(TService)] = instance;
            foreach (Type interfaceType in instance.GetType().GetInterfaces())
            {
                _instances[interfaceType] = instance;
            }
            return this;
        }

        public IServiceRegistry AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return this;
        }

        public IServiceRegistry AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            return this;
        }

        public IServiceScope AddScoped<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            throw new NotSupportedException();
        }

        public bool IsRegistered<TService>()
            where TService : class
        {
            return _instances.ContainsKey(typeof(TService));
        }

        public IEnumerable<ServiceDescriptor> GetDescriptors()
        {
            return Array.Empty<ServiceDescriptor>();
        }

        public void Dispose()
        {
        }
    }

    internal sealed class RecordingServiceLocator : IServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public RecordingServiceLocator(IServiceRegistry registry)
        {
            Collection = registry;
        }

        public IServiceRegistry Collection { get; }

        public void Add<TService>(TService service)
            where TService : class
        {
            _services[typeof(TService)] = service;
        }

        public TService GetService<TService>()
            where TService : class
        {
            return (TService)_services[typeof(TService)];
        }

        public bool TryGetService<TService>(out TService? service)
            where TService : class
        {
            if (_services.TryGetValue(typeof(TService), out object? value))
            {
                service = (TService)value;
                return true;
            }

            service = null;
            return false;
        }

        public IEnumerable<TService> GetServices<TService>()
            where TService : class
        {
            return _services.Values.OfType<TService>();
        }

        public TService CreateInstance<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            throw new NotSupportedException();
        }

        public IServiceScope CreateScope()
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }
}
