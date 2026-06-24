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
using UnityEngine;

namespace Nomad.EngineUtils
{
    /// <summary>
    /// Relays Unity 3D trigger callbacks through standard C# events.
    /// </summary>
    public sealed class TriggerEventRelay3D : MonoBehaviour
    {
        /// <summary>
        /// Raised when another collider enters the trigger.
        /// </summary>
        public event Action<Collider>? TriggerEntered;

        /// <summary>
        /// Raised when another collider exits the trigger.
        /// </summary>
        public event Action<Collider>? TriggerExited;

        /// <summary>
        /// Forwards Unity's trigger-enter callback.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            TriggerEntered?.Invoke(other);
        }

        /// <summary>
        /// Forwards Unity's trigger-exit callback.
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            TriggerExited?.Invoke(other);
        }
    }
}
