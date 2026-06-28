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

namespace Nomad.Core.OnlineServices
{
    /// <summary>
    /// Provider-neutral metadata for a cloud storage object.
    /// </summary>
    public readonly struct CloudStorageFileInfo
    {
        /// <summary>
        /// Provider-relative path for the cloud storage object.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Size of the object in bytes.
        /// </summary>
        public long SizeBytes { get; }

        /// <summary>
        /// Last remote modification time when the provider reports one.
        /// </summary>
        public DateTimeOffset? LastModified { get; }

        /// <summary>
        /// Whether the provider confirms the object is persisted remotely.
        /// </summary>
        public bool IsPersisted { get; }

        public CloudStorageFileInfo(string path, long sizeBytes, DateTimeOffset? lastModified = null, bool isPersisted = true)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            SizeBytes = sizeBytes;
            LastModified = lastModified;
            IsPersisted = isPersisted;
        }
    }
}
