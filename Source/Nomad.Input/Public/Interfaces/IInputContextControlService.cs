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

using Nomad.Core.Input;

namespace Nomad.Input.Interfaces
{
    /// <summary>
    /// Controls the active input scheme and context mask used to resolve bindings.
    /// </summary>
    public interface IInputContextControlService
    {
        /// <summary>
        /// The active input scheme. A null scheme allows all schemes.
        /// </summary>
        InputScheme? ActiveScheme { get; }

        /// <summary>
        /// The active context mask.
        /// </summary>
        uint ContextMask { get; }

        /// <summary>
        /// Sets the active input scheme. Pass null to allow all schemes.
        /// </summary>
        /// <param name="scheme"></param>
        void SetActiveScheme(InputScheme? scheme);

        /// <summary>
        /// Replaces the active context mask.
        /// </summary>
        /// <param name="contextMask"></param>
        void SetContextMask(uint contextMask);

        /// <summary>
        /// Enables a context bit or mask.
        /// </summary>
        /// <param name="contextMask"></param>
        void EnableContext(uint contextMask);

        /// <summary>
        /// Disables a context bit or mask.
        /// </summary>
        /// <param name="contextMask"></param>
        void DisableContext(uint contextMask);

        /// <summary>
        /// Restores the default context mask.
        /// </summary>
        void ResetContexts();
    }
}
