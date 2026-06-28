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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.FileSystem.Streams;
using Nomad.Core.Memory.Buffers;

namespace Nomad.Core.OnlineServices
{
    /// <summary>
    /// Interface for cloud storage services.
    /// </summary>
    public interface ICloudStorageService : IDisposable
    {
        /// <summary>
        /// Indicates whether the service supports cloud storage.
        /// </summary>
        bool SupportsCloudStorage { get; }

        /// <summary>
        /// Indicates whether cloud storage is currently enabled and usable for the active user/application.
        /// </summary>
        bool IsCloudStorageEnabled { get; }

        /// <summary>
        /// Gets the current provider quota when the provider exposes one.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>The reported quota, or null when unavailable.</returns>
        Task<CloudStorageQuota?> GetQuotaAsync(CancellationToken ct = default);

        /// <summary>
        /// Lists known cloud storage files.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns>Provider-relative file metadata.</returns>
        Task<IReadOnlyList<CloudStorageFileInfo>> ListFilesAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets metadata for a cloud storage file.
        /// </summary>
        /// <param name="path">Provider-relative path of the file.</param>
        /// <param name="ct"></param>
        /// <returns>The file metadata, or null when it does not exist.</returns>
        Task<CloudStorageFileInfo?> GetFileInfoAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// Checks if a file exists in cloud storage.
        /// </summary>
        /// <param name="path">The name of the file to check.</param>
        /// <param name="ct"></param>
        /// <returns>True if the file exists, false otherwise.</returns>
        Task<bool> FileExistsAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IFileReadStream> OpenReadAsync(string path, CancellationToken ct = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task WriteAsync(string path, IBufferHandle data, CancellationToken ct = default);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> DeleteAsync(string path, CancellationToken ct = default);

        Task SyncAsync(CancellationToken ct = default);
    }
}
