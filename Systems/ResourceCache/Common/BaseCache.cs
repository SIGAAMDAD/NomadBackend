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

using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using NomadCore.Systems.ResourceCache.Enums;
using NomadCore.GameServices;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Systems.ResourceCache.Domain.Models.ValueObjects;
using NomadCore.Systems.ResourceCache.Domain.Models.Entities;
using NomadCore.Systems.ResourceCache.Application.Interfaces;
using NomadCore.Domain.Events;
using NomadCore.Systems.ResourceCache.Domain.Entities;
using NomadCore.Infrastructure.Collections;

namespace NomadCore.Systems.ResourceCache.Common {
	/*
	===================================================================================
	
	BaseCache
	
	===================================================================================
	*/
	/// <summary>
	/// The base caching type for all resources.
	/// </summary>
	
	public class BaseCache<TResource, TId> : IResourceCacheService<TResource, TId>
		where TResource : IDisposable
		where TId : IEquatable<TId>
	{
		public CacheStatistics Statistics => new CacheStatistics {
			CacheHits = _cacheHits,
			CacheMisses = _cacheMisses,
			TotalLoaded = _totalLoaded,
			MemoryUsage = _currentMemorySize,
			ActiveReferences = _cache.Values.Count( entry => entry.ReferenceCount > 0 ),
			AverageLoadTime = _totalLoaded > 0 ? TimeSpan.FromTicks( _totalLoadTime.Ticks / _totalLoaded ) : TimeSpan.Zero
		};

		public CachePolicy Policy {
			get => _policy;
			set {
				_policy = value ?? throw new ArgumentNullException( nameof( value ) );
				EvictIfNeeded();
			}
		}
		private CachePolicy _policy = CachePolicy.Default;

		public long CurrentCacheSize => _currentMemorySize;
		public long MaxCacheSize {
			get => _policy.MaxMemorySize;
			set {
				_policy.MaxMemorySize = value;
				EvictIfNeeded();
			}
		}

		public int Count => _cache.Count;

		public IGameEvent<ResourceLoadedEventData<TId>> ResourceLoaded => _resourceLoaded;
		private readonly IGameEvent<ResourceLoadedEventData<TId>> _resourceLoaded;

		public IGameEvent<ResourceUnloadedEventData<TId>> ResourceUnloaded => _resourceUnloaded;
		private readonly IGameEvent<ResourceUnloadedEventData<TId>> _resourceUnloaded;

		public IGameEvent<ResourceLoadFailedEventData<TId>> ResourceLoadFailed => _resourceLoadFailed;
		private readonly IGameEvent<ResourceLoadFailedEventData<TId>> _resourceLoadFailed;

		private readonly ConcurrentDictionary<TId, CacheEntry<TResource, TId>> _cache = new();
		private readonly ReaderWriterLockSlim _cacheLock = new ReaderWriterLockSlim();

		private int _currentMemorySize = 0;
		private int _cacheHits = 0;
		private int _cacheMisses = 0;
		private int _totalLoaded = 0;
		private int _totalUnloaded = 0;
		private TimeSpan _totalLoadTime = TimeSpan.Zero;

		private readonly Timer _cleanupTimer;

		private readonly ILoggerService? _logger;

		private readonly IResourceLoader<TResource, TId> _loader;

		/*
		===============
		BaseCache
		===============
		*/
		public BaseCache( ILoggerService logger, IGameEventRegistryService eventFactory, IResourceLoader<TResource, TId> loader ) {
			ArgumentNullException.ThrowIfNull( logger );
			ArgumentNullException.ThrowIfNull( eventFactory );
			ArgumentNullException.ThrowIfNull( loader );

			_logger = logger;
			_loader = loader;

			_resourceLoaded = eventFactory.GetEvent<ResourceLoadedEventData<TId>>( StringPool.Intern( nameof( ResourceLoaded ) ) );
			_resourceUnloaded = eventFactory.GetEvent<ResourceUnloadedEventData<TId>>( StringPool.Intern( nameof( ResourceUnloaded ) ) );
			_resourceLoadFailed = eventFactory.GetEvent<ResourceLoadFailedEventData<TId>>( StringPool.Intern( nameof( ResourceLoadFailed ) ) );

			_cleanupTimer = new Timer( _ => ClearUnused(), null, TimeSpan.FromMinutes( 1 ), TimeSpan.FromMinutes( 10 ) );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public virtual void Dispose() {
			_cleanupTimer?.Dispose();
			UnloadAll();

			_resourceLoaded?.Dispose();
			_resourceUnloaded?.Dispose();
			_resourceLoadFailed?.Dispose();

			_cacheLock?.Dispose();

			GC.SuppressFinalize( this );
		}

		/*
		===============
		ClearUnushed
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void ClearUnused() {
			_logger?.PrintDebug( $"BaseCache.ClearUnused: removing unused entries ({typeof( TResource )})..." );

			int removedCount  = 0;
			_cacheLock.EnterWriteLock();
			try {
				foreach ( var kvp in _cache ) {
					if ( kvp.Value.ReferenceCount > 0 ) {
						continue;
					}
					if ( _cache.TryRemove( kvp.Key, out CacheEntry<TResource, TId>? entry ) ) {
						removedCount++;
						_currentMemorySize -= entry.MemorySize;
						ResourceUnloaded.Publish( new ResourceUnloadedEventData<TId>( kvp.Key, entry.MemorySize, UnloadReason.ReferenceCountZero ) );
						entry.Dispose();
					}
				}
			}
			finally {
				_cacheLock.ExitWriteLock();
			}

			Interlocked.Add( ref _totalUnloaded, removedCount );
		}

		/*
		===============
		GetCached
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public ICacheEntry<TResource, TId> GetCached( TId id ) {
			_cacheLock.EnterReadLock();
			try {
				if ( _cache.TryGetValue( id, out var entry ) ) {
					Interlocked.Increment( ref _cacheHits );
					entry.UpdateAccessStats();
					return entry;
				}
			}
			finally {
				_cacheLock.ExitReadLock();
			}

			Interlocked.Increment( ref _cacheMisses );
			var cacheEntry = LoadAndCacheResource( id );
			if ( cacheEntry == null ) {
				_logger?.PrintError( $"BaseCache.GetCached: failed to load resource '{id}'" );
			}
			return cacheEntry;
		}

		/*
		===============
		TryGetById
		===============
		*/
		/// <summary>
		/// s
		/// </summary>
		/// <param name="id"></param>
		/// <param name="entity"></param>
		/// <returns></returns>
		public bool TryGetById( TId id, out ICacheEntry<TResource, TId>? entity ) {
			return TryGetCached( id, out entity );
		}

		/*
		===============
		GetById
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public ICacheEntry<TResource, TId>? GetById( TId id ) {
			return GetCached( id );
		}

		/*
		===============
		GetByIdAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async ValueTask<ICacheEntry<TResource, TId>?> GetByIdAsync( TId id, CancellationToken ct = default ) {
			return await GetCachedAsync( id, null, ct );
		}

		/*
		===============
		GetByIds
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
		public IEnumerable<ICacheEntry<TResource, TId>> GetByIds( ReadOnlySpan<TId> ids ) {
			ICacheEntry<TResource, TId>[] cached = new ICacheEntry<TResource, TId>[ ids.Length ];
			for ( int i = 0; i < ids.Length; i++ ) {
				cached[ i ] = GetCached( ids[ i ] );
			}
			return cached;
		}

		/*
		===============
		GetCachedAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="progress"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async ValueTask<ICacheEntry<TResource, TId>> GetCachedAsync( TId id, IProgress<ResourceLoadProgressEventData<TId>>? progress = null, CancellationToken ct = default ) {
			ct.ThrowIfCancellationRequested();

			_cacheLock.EnterReadLock();
			try {
				if ( _cache.TryGetValue( id, out CacheEntry<TResource, TId>? entry ) && entry.LoadState == ResourceLoadState.Complete ) {
					Interlocked.Increment( ref _cacheHits );
					entry.UpdateAccessStats();
					return entry;
				}
			}
			finally {
				_cacheLock.ExitReadLock();
			}

			var cacheEntry = await LoadAndCacheResourceAsync( id, progress, ct );
			if ( cacheEntry == null ) {
				_logger?.PrintError( $"BaseCache.GetCachedAsync: failed to load resource '{id}'!" );
			}
			return cacheEntry;
		}

		/*
		===============
		AddReference
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		public void AddReference( TId id ) {
			_cacheLock.EnterWriteLock();
			try {
				if ( _cache.TryGetValue( id, out CacheEntry<TResource, TId>? entry ) ) {
					entry.ReferenceCount++;
					_logger?.PrintLine( $"BaseCache.AddReference: adding reference to cache entry '{entry.Id}'" );
					entry.UpdateAccessStats();
				}
			}
			finally {
				_cacheLock.ExitWriteLock();
			}
		}

		/*
		===============
		GetReferenceCount
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public int GetReferenceCount( TId id ) {
			_cacheLock.EnterReadLock();
			try {
				return _cache.TryGetValue( id, out CacheEntry<TResource, TId>? entry ) ? entry.ReferenceCount : 0;
			}
			finally {
				_cacheLock.ExitReadLock();
			}
		}

		/*
		===============
		ReleaseReference
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		public void ReleaseReference( TId id ) {
			_cacheLock.EnterReadLock();
			try {
				if ( _cache.TryGetValue( id, out CacheEntry<TResource, TId>? entry ) ) {
					entry.ReferenceCount = Math.Min( 0, entry.ReferenceCount - 1 );
					if ( entry.ReferenceCount == 0 && ( DateTime.UtcNow - entry.AccessStats.LastAccessTime ) > _policy.UnloadUnusedAfter ) {
						_cacheLock.ExitReadLock();
						Unload( id );
						return; // is this necessary?
					}
				}
			}
			finally {
				_cacheLock.ExitReadLock();
			}
		}

		/*
		===============
		Preload
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		public void Preload( TId id ) {
			LoadAndCacheResource( id );
		}

		/*
		===============
		Preload
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ids"></param>
		public void Preload( IEnumerable<TId> ids ) {
			foreach ( var id in ids ) {
				if ( _cache.ContainsKey( id ) ) {
					continue;
				}
				LoadAndCacheResource( id );
			}
		}

		/*
		===============
		PreloadAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
		public async ValueTask PreloadAsync( IEnumerable<TId> ids, CancellationToken ct = default ) {
			ct.ThrowIfCancellationRequested();

			List<Task> tasks = new List<Task>();
			foreach ( var id in ids ) {
				if ( _cache.ContainsKey( id ) ) {
					continue;
				}
				tasks.Add( LoadAndCacheResourceAsync( id, null, ct ) );
			}
			await Task.WhenAll( tasks );
		}

		/*
		===============
		TryGetCached
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="resource"></param>
		/// <returns></returns>
		public bool TryGetCached( TId id, out ICacheEntry<TResource, TId>? resource ) {
			resource = null;

			_cacheLock.EnterWriteLock();
			try {
				if ( _cache.TryGetValue( id, out var entry ) ) {
					Interlocked.Increment( ref _cacheHits );
					entry.UpdateAccessStats();
					resource = entry;
					return true;
				}
			}
			finally {
				_cacheLock.ExitWriteLock();
			}

			Interlocked.Increment( ref _cacheMisses );
			return false;
		}

		/*
		===============
		Unload
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		public void Unload( TId id ) {
			_cacheLock.EnterWriteLock();
			try {
				if ( _cache.TryRemove( id, out var entry ) ) {
					_currentMemorySize -= entry.MemorySize;
					Interlocked.Increment( ref _totalLoaded );
					ResourceUnloaded.Publish( new ResourceUnloadedEventData<TId>( id, entry.MemorySize, UnloadReason.Manual ) );
				}
			}
			finally {
				_cacheLock.ExitWriteLock();
			}
		}

		/*
		===============
		UnloadAll
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void UnloadAll() {
			_cacheLock.EnterWriteLock();
			try {
				foreach ( var kvp in _cache ) {
					ResourceUnloaded.Publish( new ResourceUnloadedEventData<TId>( kvp.Key, kvp.Value.MemorySize, UnloadReason.Manual ) );
					kvp.Value.Dispose();
				}
			}
			finally {
				_cacheLock.ExitWriteLock();
			}
		}

		/*
		===============
		EvictIfNeeded
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void EvictIfNeeded() {
			if ( _policy.EvictionPolicy == EvictionPolicy.Never || _currentMemorySize <= _policy.MaxMemorySize && _cache.Count <= _policy.MaxResourceCount ) {
				return;
			}

			_cacheLock.EnterWriteLock();
			try {
				var candidates = _cache.Where( kvp => kvp.Value.ReferenceCount == 0 ).ToList();
				if ( candidates.Count == 0 ) {
					return;
				}

				IEnumerable<KeyValuePair<TId, CacheEntry<TResource, TId>>> sortedCandidates = _policy.EvictionPolicy switch {
					EvictionPolicy.LRU => candidates.OrderBy( kvp => kvp.Value.AccessStats.LastAccessTime ),
					EvictionPolicy.LFU => candidates.OrderBy( kvp => kvp.Value.AccessStats.AccessCount ),
					EvictionPolicy.SizeBased => candidates.OrderByDescending( kvp => kvp.Value.MemorySize ),
					_ => candidates.OrderByDescending( kvp => kvp.Value.AccessStats.LastAccessTime )
				};

				int removedCount = 0;
				foreach ( var candidate in sortedCandidates ) {
					if ( _currentMemorySize <= _policy.MaxMemorySize && _cache.Count <= _policy.MaxResourceCount ) {
						break;
					}
					if ( _cache.TryRemove( candidate.Key, out var entry ) ) {
						removedCount++;
						_currentMemorySize -= entry.MemorySize;
						ResourceUnloaded.Publish( new ResourceUnloadedEventData<TId>(
							candidate.Key, entry.MemorySize, UnloadReason.CacheFull
						) );
						entry.Dispose();
					}
				}
				Interlocked.Add( ref _totalUnloaded, removedCount );
			}
			finally {
				_cacheLock.ExitWriteLock();
			}
		}

		/*
		===============
		LoadAndCacheResource
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private ICacheEntry<TResource, TId>? LoadAndCacheResource( TId id ) {
			Stopwatch loadTimer = Stopwatch.StartNew();
			try {
				var result = _loader.Load.Invoke( id );
				if ( result.IsFailure ) {
					_logger?.PrintError( $"BaseCache.LoadAndCacheResource: failed to load resource '{id}'" );
					return null;
				}

				int memorySize = CalculateMemorySize( result.Value );
				return CacheResource( id, result.Value, memorySize, loadTimer.Elapsed );
			} catch ( Exception e ) {
				_logger?.PrintError( $"BaseCache.LoadAndCacheResource: exception thrown while loading resource '{id}' - {e}" );
				loadTimer.Stop();
				throw;
			}
		}

		/*
		===============
		LoadAndCacheResourceAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="progress"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		private async Task<ICacheEntry<TResource, TId>?> LoadAndCacheResourceAsync( TId id, IProgress<ResourceLoadProgressEventData<TId>>? progress = null, CancellationToken ct = default ) {
			progress?.Report( new ResourceLoadProgressEventData<TId>( id, 0.0f, ResourceLoadState.Queued ) );

			Stopwatch loadTimer = Stopwatch.StartNew();
			try {
				var result = await _loader.LoadAsync( id, ct );
				ct.ThrowIfCancellationRequested();

				if ( result.IsFailure ) {
					_logger?.PrintError( $"Base_cache.LoadAndCacheResourceAsync: failed to load resource '{id}'" );
					return null;
				}
				progress?.Report( new ResourceLoadProgressEventData<TId>( id, 0.0f, ResourceLoadState.Processing ) );

				int memorySize = CalculateMemorySize( result.Value );
				return CacheResource( id, result.Value, memorySize, loadTimer.Elapsed );
			} catch ( Exception e ) {
				loadTimer.Stop();

				_cache.TryRemove( id, out _ );
				ResourceLoadFailed.Publish( new ResourceLoadFailedEventData<TId>( id, e.Message, e ) );
				throw;
			}
		}

		/*
		===============
		CacheResource
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="resource"></param>
		/// <param name="memorySize"></param>
		/// <param name="loadTime"></param>
		private ICacheEntry<TResource, TId> CacheResource( TId id, TResource resource, int memorySize, TimeSpan loadTime ) {
			_cacheLock.EnterWriteLock();
			try {
				CacheEntry<TResource, TId> entry = new CacheEntry<TResource, TId>(
					this, id, resource, memorySize, loadTime, ResourceLoadState.Complete
				);

				_cache[ id ] = entry;
				_currentMemorySize += memorySize;
				Interlocked.Increment( ref _totalLoaded );
				_totalLoadTime += loadTime;

				ResourceLoaded.Publish( new ResourceLoadedEventData<TId>( id, loadTime, memorySize ) );

				// flush the cache if needed
				EvictIfNeeded();

				return entry;
			}
			finally {
				_cacheLock.ExitWriteLock();
			}
		}

		/*
		===============
		CalculateMemorySize
		===============
		*/
		protected virtual int CalculateMemorySize( TResource resource ) {
			return 0;
		}
	};
};