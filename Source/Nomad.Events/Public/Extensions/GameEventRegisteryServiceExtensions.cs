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

using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;

namespace Nomad.Events.Extensions
{
    /// <summary>
    ///
    /// </summary>
    public static class GameEventRegistryServiceExtensions
    {
        public static ISubscriptionGroup Group(this IGameEventRegistryService events, string name)
        {
            ArgumentGuard.ThrowIfNull(events);
            return events.GetGroup(name);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="events"></param>
        /// <param name="name"></param>
        /// <param name="nameSpace"></param>
        /// <param name="callback"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static ISubscriptionHandle On<TArgs>(
            this IGameEventRegistryService events,
            string name,
            string nameSpace,
            EventCallback<TArgs> callback,
            EventFlags flags = EventFlags.Default
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(events);
            ArgumentGuard.ThrowIfNull(callback);

            return events
                .GetEvent<TArgs>(
                    name,
                    nameSpace,
                    flags
                )
                .Subscribe(callback);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="events"></param>
        /// <param name="name"></param>
        /// <param name="nameSpace"></param>
        /// <param name="callback"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static ISubscriptionHandle OnAsync<TArgs>(
            this IGameEventRegistryService events,
            string name,
            string nameSpace,
            AsyncEventCallback<TArgs> callback,
            EventFlags flags = EventFlags.Default
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(events);
            ArgumentGuard.ThrowIfNull(callback);

            return events
                .GetEvent<TArgs>(
                    name,
                    nameSpace,
                    flags
                )
                .SubscribeAsync(callback);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="events"></param>
        /// <param name="name"></param>
        /// <param name="nameSpace"></param>
        /// <param name="callback"></param>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static bool TryOn<TArgs>(
            this IGameEventRegistryService events,
            string name,
            string nameSpace,
            EventCallback<TArgs> callback,
            out ISubscriptionHandle? handle
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(events);
            ArgumentGuard.ThrowIfNull(callback);

            handle = null;

            if (!events.TryGetEvent(name, nameSpace, out IGameEvent<TArgs>? gameEvent))
            {
                return false;
            }

            handle = gameEvent.Subscribe(callback);
            return true;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="registry"></param>
        /// <param name="name"></param>
        /// <param name="nameSpace"></param>
        /// <param name="args"></param>
        /// <param name="flags"></param>
        public static void Publish<TArgs>(
            this IGameEventRegistryService registry,
            string name,
            string nameSpace,
            in TArgs args,
            EventFlags flags = EventFlags.Default
        )
            where TArgs : struct
        {
            IGameEvent<TArgs> gameEvent = registry.GetEvent<TArgs>(
                name,
                nameSpace,
                flags
            );

            gameEvent.Publish(args);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="registry"></param>
        /// <param name="name"></param>
        /// <param name="nameSpace"></param>
        /// <param name="args"></param>
        /// <param name="ct"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        public static Task PublishAsync<TArgs>(
            this IGameEventRegistryService registry,
            string name,
            string nameSpace,
            TArgs args,
            CancellationToken ct = default,
            EventFlags flags = EventFlags.Default
        )
            where TArgs : struct
        {
            IGameEvent<TArgs> gameEvent = registry.GetEvent<TArgs>(
                name,
                nameSpace,
                flags
            );

            return gameEvent.PublishAsync(args, ct);
        }
    }
}
