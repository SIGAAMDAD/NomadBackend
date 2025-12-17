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

namespace NomadCore.GameServices {
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
		public long CurrentCacheSize { get; }

		public IGameEvent<ResourceLoadedEventData<TId>> ResourceLoaded { get; }
		public IGameEvent<ResourceUnloadedEventData<TId>> ResourceUnloaded { get; }
		public IGameEvent<ResourceLoadFailedEventData<TId>> ResourceLoadFailed { get; }

		public ICacheEntry<TResource, TId> GetCached( TId id );
		public ValueTask<ICacheEntry<TResource, TId>> GetCachedAsync( TId id, IProgress<ResourceLoadProgressEventData<TId>>? progress = null, CancellationToken ct = default );
		
		public bool TryGetCached( TId id, out ICacheEntry<TResource, TId>? resource );
		public void Preload( TId id );
		public void Unload( TId id );
		public void UnloadAll();
		public void ClearUnused();

		public void AddReference( TId id );
		public int GetReferenceCount( TId id );
		public void ReleaseReference( TId id );

		public void Preload( IEnumerable<TId> ids );
		public ValueTask PreloadAsync( IEnumerable<TId> ids, CancellationToken ct = default );
	};
};