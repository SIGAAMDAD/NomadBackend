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
using System.Linq;
using System.Reflection;
using Nomad.Core.Events;
using Nomad.Core.Logger;

namespace Nomad.Events.Extensions
{
    /// <summary>
    ///
    /// </summary>
    public sealed class EventBindingService
    {
        private readonly IGameEventRegistryService _events;
        private readonly ILoggerCategory _logger;

        /// <summary>
        ///
        /// </summary>
        /// <param name="events"></param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public EventBindingService(IGameEventRegistryService events, ILoggerCategory logger)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public SubscriptionScope Bind(object target)
        {
            ArgumentNullException.ThrowIfNull(target);

            var bag = new SubscriptionScope();
            Type targetType = target.GetType();
            BindingFlags flags =
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic;

            foreach (MethodInfo method in targetType.GetMethods(flags))
            {
                EventHandlerAttribute[] attributes =
                    method.GetCustomAttributes<EventHandlerAttribute>(inherit: true).ToArray();

                if (attributes.Length == 0)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length != 1)
                {
                    throw new InvalidOperationException(
                        $"Event handler '{targetType.FullName}.{method.Name}' must have exactly one parameter."
                    );
                }

                Type payloadType = parameters[0].ParameterType;

                if (payloadType.IsByRef)
                {
                    payloadType = payloadType.GetElementType()
                        ?? throw new InvalidOperationException(
                            $"Could not resolve by-ref payload type for '{method.Name}'."
                        );
                }

                if (!payloadType.IsValueType)
                {
                    throw new InvalidOperationException(
                        $"Event handler '{targetType.FullName}.{method.Name}' payload must be a struct."
                    );
                }

                foreach (EventHandlerAttribute attribute in attributes)
                {
                    ISubscriptionHandle handle = BindMethod(
                        target,
                        method,
                        payloadType,
                        attribute);

                    bag.Add(handle);
                }
            }

            return bag;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="payloadType"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        private ISubscriptionHandle BindMethod(
            object target,
            MethodInfo method,
            Type payloadType,
            EventHandlerAttribute attribute
        )
        {
            MethodInfo generic = typeof(EventBindingService)
                .GetMethod(
                    nameof(BindMethodGeneric),
                    BindingFlags.Instance | BindingFlags.NonPublic
                )
                .MakeGenericMethod(payloadType);

            return (ISubscriptionHandle)generic.Invoke(
                this,
                new object[] { target, method, attribute })!;
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="TArgs"></typeparam>
        /// <param name="target"></param>
        /// <param name="method"></param>
        /// <param name="attribute"></param>
        /// <returns></returns>
        private ISubscriptionHandle BindMethodGeneric<TArgs>(
            object target,
            MethodInfo method,
            EventHandlerAttribute attribute
        )
            where TArgs : struct
        {
            IGameEvent<TArgs> gameEvent = _events.GetEvent<TArgs>(
                attribute.Name,
                attribute.NameSpace,
                attribute.Flags);

            EventCallback<TArgs> callback = (in TArgs args) =>
            {
                try
                {
                    method.Invoke(target, new object[] { args });
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    throw ex.InnerException;
                }
            };

            if (!attribute.Safe)
            {
                return gameEvent.Subscribe(callback);
            }

            return gameEvent.OnSafe(callback, _logger);
        }
    }
}
