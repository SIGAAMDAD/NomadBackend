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
using Nomad.Core.Events;
using Nomad.Events.Private.EventTypes;

namespace Nomad.Events.Extensions
{
    /// <summary>
    ///
    /// </summary>
    public static class GameEventExtensions
    {
        /// <summary>
        /// Creates a one-time subscription handle for the provided GameEvent.
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ISubscriptionHandle SubscribeOnce<TArgs>(this IGameEvent<TArgs> gameEvent, EventCallback<TArgs> callback)
            where TArgs : struct
        {
            ISubscriptionHandle handle = null;
            EventCallback<TArgs> killAfterPublish = (in TArgs args) => { callback(in args); handle.Dispose(); };
            handle = gameEvent.Subscribe(killAfterPublish);
            return handle;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="predicate"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ISubscriptionHandle SubscribeWhere<TArgs>(this IGameEvent<TArgs> gameEvent, Func<TArgs, bool> predicate, EventCallback<TArgs> callback)
            where TArgs : struct
        {
            EventCallback<TArgs> filter = (in TArgs args) => { if (predicate(args)) { callback.Invoke(in args); } };
            return gameEvent.Subscribe(filter);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="predicate"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ISubscriptionHandle SubscribeUntil<TArgs>(this IGameEvent<TArgs> gameEvent, Func<TArgs, bool> predicate, EventCallback<TArgs> callback)
            where TArgs : struct
        {
            ISubscriptionHandle handle = null;
            EventCallback<TArgs> filter = (in TArgs args) => { if (predicate(args)) { callback.Invoke(in args); handle.Dispose(); } };
            handle = gameEvent.Subscribe(filter);
            return handle;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IGameEvent<TArgs> Where<TArgs>(this IGameEvent<TArgs> source, Func<TArgs, bool> predicate)
            where TArgs : struct
        {
            return new FilteredGameEvent<TArgs>(source, predicate);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="source"></param>
        /// <param name="payload"></param>
        /// <param name="publishIntervalMS"></param>
        /// <returns></returns>
        public static IGameEvent<TArgs> PublishEvery<TArgs>(this IGameEvent<TArgs> source, TArgs payload, int publishIntervalMS)
            where TArgs : struct
        {
            return new ScheduledEvent<TArgs>(source, payload, publishIntervalMS);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="source"></param>
        /// <param name="payloadCallback"></param>
        /// <param name="publishIntervalMS"></param>
        /// <returns></returns>
        public static IGameEvent<TArgs> PublishEvery<TArgs>(this IGameEvent<TArgs> source, Func<TArgs> payloadCallback, int publishIntervalMS)
            where TArgs : struct
        {
            return new ScheduledEvent<TArgs>(source, payloadCallback, publishIntervalMS);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="source"></param>
        /// <param name="waitMS"></param>
        /// <returns></returns>
        public static IGameEvent<TArgs> PublishAfter<TArgs>(this IGameEvent<TArgs> source, int waitMS)
            where TArgs : struct
        {
            return new DelayedEvent<TArgs>(source, waitMS);
        }
    }
}
