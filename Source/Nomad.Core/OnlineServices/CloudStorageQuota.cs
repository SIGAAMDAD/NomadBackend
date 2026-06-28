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

namespace Nomad.Core.OnlineServices
{
    /// <summary>
    /// Describes the storage quota reported by an online cloud storage provider.
    /// </summary>
    public readonly struct CloudStorageQuota
    {
        /// <summary>
        /// Total bytes available to the application.
        /// </summary>
        public long TotalBytes { get; }

        /// <summary>
        /// Remaining bytes available to the application.
        /// </summary>
        public long AvailableBytes { get; }

        public CloudStorageQuota(long totalBytes, long availableBytes)
        {
            TotalBytes = totalBytes;
            AvailableBytes = availableBytes;
        }
    }
}
