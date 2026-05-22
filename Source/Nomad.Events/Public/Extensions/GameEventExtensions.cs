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
using Nomad.Core.Compatibility.Guards;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.Events.Private.EventTypes;

namespace Nomad.Events.Extensions
{
    /// <summary>
    ///
    /// </summary>
    public static class GameEventExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ISubscriptionHandle On<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            EventCallback<TArgs> callback
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(gameEvent);
            ArgumentGuard.ThrowIfNull(callback);

            return gameEvent.Subscribe(callback);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ISubscriptionHandle OnAsync<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            AsyncEventCallback<TArgs> callback
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(gameEvent);
            ArgumentGuard.ThrowIfNull(callback);

            return gameEvent.SubscribeAsync(callback);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="callback"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static ISubscriptionHandle OnSafe<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            EventCallback<TArgs> callback,
            ILoggerCategory logger
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(gameEvent);
            ArgumentGuard.ThrowIfNull(callback);
            ArgumentGuard.ThrowIfNull(logger);

            EventCallback<TArgs> wrapper = (in TArgs args) =>
            {
                try
                {
                    callback(in args);
                }
                catch (Exception ex)
                {
                    logger.PrintError(
                        $"Safe event subscriber failed. Event='{gameEvent.DebugName}', " +
                        $"Payload='{typeof(TArgs).Name}', Handler='{callback.Method.DeclaringType?.FullName}.{callback.Method.Name}', " +
                        $"Exception='{ex}'");
                }
            };

            return gameEvent.Subscribe(wrapper);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="callback"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static ISubscriptionHandle OnSafeAsync<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            AsyncEventCallback<TArgs> callback,
            ILoggerCategory logger
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(gameEvent);
            ArgumentGuard.ThrowIfNull(callback);
            ArgumentGuard.ThrowIfNull(logger);

            AsyncEventCallback<TArgs> wrapper = async (args, ct) =>
            {
                try
                {
                    await callback(args, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.PrintError(
                        $"Safe async event subscriber failed. Event='{gameEvent.DebugName}', " +
                        $"Payload='{typeof(TArgs).Name}', Handler='{callback.Method.DeclaringType?.FullName}.{callback.Method.Name}', " +
                        $"Exception='{ex}'");
                }
            };

            return gameEvent.SubscribeAsync(wrapper);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="predicate"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ISubscriptionHandle When<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            FilteredEventPredicate<TArgs> predicate,
            EventCallback<TArgs> callback
        )
            where TArgs : struct
        {
            return SubscribeWhere(gameEvent, predicate, callback);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IGameEvent<TArgs> Where<TArgs>(
            this IGameEvent<TArgs> source,
            FilteredEventPredicate<TArgs> predicate
        )
            where TArgs : struct
        {
            return new FilteredGameEvent<TArgs>(source, predicate);
        }

        /// <summary>
        /// Creates a one-time subscription handle for the provided GameEvent.
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ISubscriptionHandle Once<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            EventCallback<TArgs> callback
        )
            where TArgs : struct
        {
            return SubscribeOnce(gameEvent, callback);
        }

        /// <summary>
        /// Creates a one-time subscription handle for the provided GameEvent.
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ISubscriptionHandle SubscribeOnce<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            EventCallback<TArgs> callback
        )
            where TArgs : struct
        {
            ISubscriptionHandle handle = null;

            EventCallback<TArgs> killAfterPublish = (in TArgs args) =>
            {
                handle?.Dispose();
                callback.Invoke(in args);
            };

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
        public static ISubscriptionHandle SubscribeWhere<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            FilteredEventPredicate<TArgs> predicate,
            EventCallback<TArgs> callback
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(gameEvent);
            ArgumentGuard.ThrowIfNull(predicate);
            ArgumentGuard.ThrowIfNull(callback);

            EventCallback<TArgs> filter = (in TArgs args) =>
            {
                if (predicate.Invoke(in args))
                {
                    callback.Invoke(in args);
                }
            };

            return gameEvent.Subscribe(filter);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="source"></param>
        /// <param name="payload"></param>
        /// <param name="publishIntervalMS"></param>
        /// <returns></returns>
        public static IGameEvent<TArgs> PublishEvery<TArgs>(
            this IGameEvent<TArgs> source,
            TArgs payload, int publishIntervalMS
        )
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
        public static IGameEvent<TArgs> PublishEvery<TArgs>(
            this IGameEvent<TArgs> source,
            Func<TArgs> payloadCallback,
            int publishIntervalMS
        )
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
        public static IGameEvent<TArgs> PublishAfter<TArgs>(
            this IGameEvent<TArgs> source,
            int waitMS
        )
            where TArgs : struct
        {
            return new DelayedEvent<TArgs>(source, waitMS);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="args"></param>
        public static void PublishNow<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            in TArgs args
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(gameEvent);
            gameEvent.Publish(in args);
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="count"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ISubscriptionHandle Skip<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            int count,
            EventCallback<TArgs> callback
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(gameEvent);
            ArgumentGuard.ThrowIfNull(callback);
            RangeGuard.ThrowIfNegative(count, nameof(count));

            int remaining = count;

            return gameEvent.Subscribe((in TArgs args) =>
            {
                if (remaining > 0)
                {
                    remaining--;
                    return;
                }

                callback.Invoke(in args);
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="gameEvent"></param>
        /// <param name="predicate"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static ISubscriptionHandle While<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            FilteredEventPredicate<TArgs> predicate,
            EventCallback<TArgs> callback
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(gameEvent);
            ArgumentGuard.ThrowIfNull(predicate);
            ArgumentGuard.ThrowIfNull(callback);

            ISubscriptionHandle? handle = null;

            EventCallback<TArgs> wrapper = (in TArgs args) =>
            {
                if (!predicate.Invoke(in args))
                {
                    handle?.Dispose();
                    return;
                }

                callback.Invoke(in args);
            };

            handle = gameEvent.Subscribe(wrapper);
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
        public static ISubscriptionHandle Until<TArgs>(
            this IGameEvent<TArgs> gameEvent,
            FilteredEventPredicate<TArgs> predicate,
            EventCallback<TArgs> callback
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(gameEvent);
            ArgumentGuard.ThrowIfNull(predicate);
            ArgumentGuard.ThrowIfNull(callback);

            ISubscriptionHandle handle = null;

            EventCallback<TArgs> filter = (in TArgs args) =>
            {
                if (predicate.Invoke(in args))
                {
                    callback.Invoke(in args);
                    handle.Dispose();
                }
            };

            handle = gameEvent.Subscribe(filter);
            return handle;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="subscriptions"></param>
        /// <returns></returns>
        public static ISubscriptionHandle AddTo(
            this ISubscriptionHandle handle,
            ICollection<ISubscriptionHandle> subscriptions
        )
        {
            ArgumentGuard.ThrowIfNull(handle);
            ArgumentGuard.ThrowIfNull(subscriptions);

            subscriptions.Add(handle);
            return handle;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static ISubscriptionHandle ForwardTo<TArgs>(
            this IGameEvent<TArgs> source,
            IGameEvent<TArgs> destination
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(source);
            ArgumentGuard.ThrowIfNull(destination);

            return source.Subscribe((in TArgs args) =>
            {
                destination.Publish(in args);
            });
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static ISubscriptionHandle ForwardTo<TArgs>(
            this IGameEvent<TArgs> source,
            IGameEvent<TArgs> destination,
            FilteredEventPredicate<TArgs> predicate
        )
            where TArgs : struct
        {
            ArgumentGuard.ThrowIfNull(source);
            ArgumentGuard.ThrowIfNull(destination);
            ArgumentGuard.ThrowIfNull(predicate);

            return source.Subscribe((in TArgs args) =>
            {
                if (predicate.Invoke(in args))
                {
                    destination.Publish(in args);
                }
            });
        }
    }
}
