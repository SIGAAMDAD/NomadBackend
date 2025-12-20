/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using NomadCore.Domain.Events;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Interfaces;
using NomadCore.Interfaces.Common;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NomadCore.GameServices
{
    /*
	===================================================================================
	
	IResourceCacheService
	
	===================================================================================
	*/
    /// <summary>
    /// A specialized repository for game/engine assets.
    /// </summary>

    public interface IResourceCacheService<TResource, TId> :
        IReadRepository<ICacheEntry<TResource, TId>, TId>,
        IAsyncReadRepository<ICacheEntry<TResource, TId>, TId>,
        IGameService
        where TResource : notnull, IDisposable
        where TId : IEquatable<TId>
    {
        long CurrentCacheSize { get; }

        IGameEvent<ResourceLoadedEventData<TId>> ResourceLoaded { get; }
        IGameEvent<ResourceUnloadedEventData<TId>> ResourceUnloaded { get; }
        IGameEvent<ResourceLoadFailedEventData<TId>> ResourceLoadFailed { get; }

        ICacheEntry<TResource, TId> GetCached(TId id);
        ValueTask<ICacheEntry<TResource, TId>> GetCachedAsync(TId id, IProgress<ResourceLoadProgressEventData<TId>>? progress = null, CancellationToken ct = default);

        bool TryGetCached(TId id, out ICacheEntry<TResource, TId>? resource);
        void Preload(TId id);
        ValueTask PreloadAsync(TId id, CancellationToken ct = default);
        void Unload(TId id);
        void UnloadAll();
        void ClearUnused();

        void AddReference(TId id);
        int GetReferenceCount(TId id);
        void ReleaseReference(TId id);

        void Preload(IEnumerable<TId> ids);
        Task PreloadAsync(IEnumerable<TId> ids, CancellationToken ct = default);
    };
};