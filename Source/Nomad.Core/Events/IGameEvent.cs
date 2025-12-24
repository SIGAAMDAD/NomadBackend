/*
===========================================================================
The Nomad Framework
Copyright (C) 2025 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Nomad.Core.Events
{
    /// <summary>
    ///
    /// </summary>
    public interface IGameEvent : IDisposable
    {
        string DebugName { get; }
        int Id { get; }
    }

    /// <summary>
    /// The base game event type.
    /// </summary>
    /// <typeparam name="TArgs"></typeparam>
    public interface IGameEvent<TArgs> : IGameEvent
        where TArgs : struct
    {
        event EventCallback<TArgs> OnPublished;
        event AsyncEventCallback<TArgs> OnPublishedAsync;

        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// The subscription's lifetime is determined by the owning event's lifetime.
        /// </remarks>
        /// <param name="asyncCallback"></param>
        /// <returns></returns>
        IDisposable SubscribeAsync(AsyncEventCallback<TArgs> asyncCallback);

        /// <summary>
        ///
        /// </summary>
        /// <remarks>
        /// The subscription's lifetime is determined by the owning event's lifetime.
        /// </remarks>
        /// <param name="callback"></param>
        /// <returns></returns>
        IDisposable Subscribe(EventCallback<TArgs> callback);

        /// <summary>
        ///
        /// </summary>
        /// <param name="eventArgs"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task PublishAsync(TArgs eventArgs, CancellationToken ct = default);

        /// <summary>
        ///
        /// </summary>
        /// <param name="eventArgs"></param>
        void Publish(in TArgs eventArgs);

        /// <summary>
        ///
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="asyncCallback"></param>
        void SubscribeAsync(object owner, AsyncEventCallback<TArgs> asyncCallback);

        /// <summary>
        ///
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="callback"></param>
        void Subscribe(object owner, EventCallback<TArgs> callback);

        /// <summary>
        ///
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="asyncCallback"></param>
        void UnsubscribeAsync(object owner, AsyncEventCallback<TArgs> asyncCallback);

        /// <summary>
        ///
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="callback"></param>
        void Unsubscribe(object owner, EventCallback<TArgs> callback);

        /// <summary>
        ///
        /// </summary>
        /// <param name="owner"></param>
        void UnsubscribeAll(object owner);

        /// <summary>
        /// Event subscription operator.
        /// </summary>
        /// <param name="callback"></param>
        void operator +=(EventCallback<TArgs> other);

        /// <summary>
        /// Event subscription operator.
        /// </summary>
        /// <param name="callback"></param>
        void operator +=(AsyncEventCallback<TArgs> other);

        /// <summary>
        /// Event subscription operator.
        /// </summary>
        /// <param name="callback"></param>
        void operator -=(EventCallback<TArgs> other);

        /// <summary>
        /// Event subscription operator.
        /// </summary>
        /// <param name="callback"></param>
        void operator -=(AsyncEventCallback<TArgs> other);
    }
}
