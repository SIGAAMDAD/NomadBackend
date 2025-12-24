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

namespace Nomad.Audio.ValueObjects
{
    /// <summary>
    /// The result of an IAudioDevice or IAudioSystem call.
    /// </summary>
    public enum AudioResult : byte
    {
        Success,
        Error_SystemNotInitialized,
        Error_InvalidParameter,
        Error_ResourceNotFound,
        Error_DeviceError,
        Error_OutOfMemory,
        Error_NotImplemented
    }
}
