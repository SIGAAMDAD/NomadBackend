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

using NomadCore.Abstractions.Services;
using NomadCore.Interfaces;
using NomadCore.Systems.ResourceCache.Infrastructure;
using NomadCore.Systems.ResourceCache.Enums;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using NomadCore.Infrastructure.Events;
using NomadCore.Systems.EventSystem.Common;
using NomadCore.Enums;
using NomadCore.Infrastructure;

namespace NomadCore.Systems.ResourceCache.Common {
	/*
	===================================================================================
	
	BaseCache
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public class BaseCache<TResource> : IResourceCacheService<TResource> where TResource : Godot.Resource {
		public static BaseCache<TResource> Instance => _instance.Value;
		private static readonly Lazy<BaseCache<TResource>> _instance = new Lazy<BaseCache<TResource>>( () => new BaseCache<TResource>() );

		public CacheStatistics Statistics => new CacheStatistics {
			CacheHits = CacheHits,
			CacheMisses = CacheMisses,
			TotalLoaded = TotalLoaded,
			MemoryUsage = CurrentMemorySize,
			ActiveReferences = Cache.Values.Count( entry => entry.ReferenceCount > 0 ),
			AverageLoadTime = TotalLoaded > 0 ? TimeSpan.FromTicks( TotalLoadTime.Ticks / TotalLoaded ) : TimeSpan.Zero
		};

		public CachePolicy Policy {
			get => _policy;
			set {
				_policy = value ?? throw new ArgumentNullException( nameof( value ) );
				EvictIfNeeded();
			}
		}
		private CachePolicy _policy = CachePolicy.Default;

		public long CurrentCacheSize => CurrentMemorySize;
		public long MaxCacheSize {
			get => _policy.MaxMemorySize;
			set {
				_policy.MaxMemorySize = value;
				EvictIfNeeded();
			}
		}

		public int Count => Cache.Count;

		public IGameEvent<ResourceLoadedEventData<TResource>> ResourceLoaded => _resourceLoaded;
		private readonly GameEvent<ResourceLoadedEventData<TResource>> _resourceLoaded = new GameEvent<ResourceLoadedEventData<TResource>>( nameof( _resourceLoaded ) );

		public IGameEvent<ResourceUnloadedEventData<TResource>> ResourceUnloaded => _resourceUnloaded;
		private readonly GameEvent<ResourceUnloadedEventData<TResource>> _resourceUnloaded = new GameEvent<ResourceUnloadedEventData<TResource>>( nameof( _resourceUnloaded ) );

		public IGameEvent<ResourceLoadFailedEventData> ResourceLoadFailed => _resourceLoadFailed;
		private readonly GameEvent<ResourceLoadFailedEventData> _resourceLoadFailed = new GameEvent<ResourceLoadFailedEventData>( nameof( _resourceLoadFailed ) );

		private readonly ConcurrentDictionary<string, CacheEntry<TResource>> Cache = new ConcurrentDictionary<string, CacheEntry<TResource>>();
		private readonly ReaderWriterLockSlim CacheLock = new ReaderWriterLockSlim();

		private long CurrentMemorySize = 0;
		private long CacheHits = 0;
		private long CacheMisses = 0;
		private long TotalLoaded = 0;
		private TimeSpan TotalLoadTime = TimeSpan.Zero;

		private readonly Timer CleanupTimer;

		private readonly ILoggerService? Logger = ServiceRegistry.Get<ILoggerService>();

		/*
		===============
		BaseCache
		===============
		*/
		protected BaseCache() {
			CleanupTimer = new Timer( _ => ClearUnused(), null, TimeSpan.FromMinutes( 1 ), TimeSpan.FromMinutes( 1 ) );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Dispose() {
			CleanupTimer?.Dispose();
			UnloadAll();

			ResourceLoaded?.Dispose();
			ResourceUnloaded?.Dispose();
			ResourceLoadFailed?.Dispose();

			CacheLock?.Dispose();
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
			CacheLock.EnterWriteLock();
			try {
				var toRemove = Cache.Where( kvp => kvp.Value.ReferenceCount == 0 ).ToList();

				foreach ( var kvp in toRemove ) {
					if ( Cache.TryRemove( kvp.Key, out CacheEntry<TResource>? entry ) ) {
						CurrentMemorySize -= entry.MemorySize;
						ResourceUnloaded.Publish( new ResourceUnloadedEventData<TResource>( kvp.Key, entry.Resource, entry.MemorySize, UnloadReason.ReferenceCountZero ) );
						entry.Resource.Dispose();
					}
				}
			}
			finally {
				CacheLock.ExitWriteLock();
			}
		}

		/*
		===============
		GetCached
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public TResource GetCached( string? path ) {
			if ( string.IsNullOrEmpty( path ) ) {
				throw new ArgumentException( "Path cannot be null or empty", nameof( path ) );
			}

			CacheLock.EnterReadLock();
			try {
				if ( Cache.TryGetValue( path, out var entry ) ) {
					Interlocked.Increment( ref CacheHits );
					UpdateAccessStats( entry );
					return entry.Resource;
				}
			}
			finally {
				CacheLock.ExitReadLock();
			}

			Interlocked.Increment( ref CacheMisses );
			return LoadAndCacheResource( path );
		}

		/*
		===============
		GetCachedAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="progress"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public async Task<TResource> GetCachedAsync( string? path, IProgress<ResourceLoadProgressEventData>? progress = null, CancellationToken cancellationToken = default ) {
			if ( string.IsNullOrEmpty( path ) ) {
				throw new ArgumentException( "Path cannot be null or empty", nameof( path ) );
			}

			CacheLock.EnterReadLock();
			try {
				if ( Cache.TryGetValue( path, out CacheEntry<TResource>? entry ) && entry.LoadState == ResourceLoadState.Complete ) {
					Interlocked.Increment( ref CacheHits );
					UpdateAccessStats( entry );
					return entry.Resource;
				}
			}
			finally {
				CacheLock.ExitReadLock();
			}

			return await LoadAndCacheResourceAsync( path, progress, cancellationToken );
		}

		/*
		===============
		AddReference
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		public void AddReference( string? path ) {
			if ( string.IsNullOrEmpty( path ) ) {
				return;
			}

			CacheLock.EnterWriteLock();
			try {
				if ( Cache.TryGetValue( path, out CacheEntry<TResource>? entry ) ) {
					entry.ReferenceCount++;
					UpdateAccessStats( entry );
				}
			}
			finally {
				CacheLock.ExitWriteLock();
			}
		}

		/*
		===============
		GetReferenceCount
		===============
		*/
		public int GetReferenceCount( string? path ) {
			if ( string.IsNullOrEmpty( path ) ) {
				return 0;
			}

			CacheLock.EnterReadLock();
			try {
				return Cache.TryGetValue( path, out CacheEntry<TResource>? entry ) ? entry.ReferenceCount : 0;
			}
			finally {
				CacheLock.ExitReadLock();
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
		/// <param name="path"></param>
		public void ReleaseReference( string? path ) {
			if ( string.IsNullOrEmpty( path ) ) {
				return;
			}

			CacheLock.EnterReadLock();
			try {
				if ( Cache.TryGetValue( path, out CacheEntry<TResource>? entry ) ) {
					entry.ReferenceCount = Math.Min( 0, entry.ReferenceCount - 1 );
					if ( entry.ReferenceCount == 0 && ( DateTime.UtcNow - entry.LastAccessTime ) > _policy.UnloadUnusedAfter ) {
						CacheLock.ExitReadLock();
						Unload( path );
						return; // is this necessary?
					}
				}
			}
			finally {
				CacheLock.ExitReadLock();
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
		/// <param name="path"></param>
		/// <exception cref="ArgumentException"></exception>
		public void Preload( string? path ) {
			if ( string.IsNullOrEmpty( path ) ) {
				throw new ArgumentException( "Path cannot be null or empty", nameof( path ) );
			}
			LoadAndCacheResource( path );
		}

		/*
		===============
		Preload
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="paths"></param>
		public void Preload( params string[] paths ) {
			for ( int i = 0; i < paths.Length; i++ ) {
				if ( !string.IsNullOrEmpty( paths[ i ] ) && !Cache.ContainsKey( paths[ i ] ) ) {
					_ = Task.Run( () => LoadAndCacheResourceAsync( paths[ i ] ) );
				}
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
		/// <param name="paths"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public async Task PreloadAsync( params string[] paths ) {
			var loadTasks = paths
				.Where( path => !string.IsNullOrEmpty( path ) && !Cache.ContainsKey( path ) )
				.Select( path => LoadAndCacheResourceAsync( path ) );

			await Task.WhenAll( loadTasks );
		}

		/*
		===============
		TryGetCached
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="resource"></param>
		/// <returns></returns>
		public bool TryGetCached( string? path, out TResource resource ) {
			resource = null;

			if ( string.IsNullOrEmpty( path ) ) {
				return false;
			}

			CacheLock.EnterWriteLock();
			try {
				if ( Cache.TryGetValue( path, out var entry ) ) {
					Interlocked.Increment( ref CacheHits );
					UpdateAccessStats( entry );
					resource = entry.Resource;
					return true;
				}
			}
			finally {
				CacheLock.ExitWriteLock();
			}

			Interlocked.Increment( ref CacheMisses );
			return false;
		}

		/*
		===============
		Unload
		===============
		*/
		public void Unload( string? path ) {
			if ( string.IsNullOrEmpty( path ) ) {
				return;
			}

			CacheLock.EnterWriteLock();
			try {
				if ( Cache.TryRemove( path, out var entry ) ) {
					CurrentMemorySize -= entry.MemorySize;
					Interlocked.Increment( ref TotalLoaded );
					ResourceUnloaded.Publish( new ResourceUnloadedEventData<TResource>( path, entry.Resource, entry.MemorySize, UnloadReason.Manual ) );
				}
			}
			finally {
				CacheLock.ExitWriteLock();
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
			CacheLock.EnterWriteLock();
			try {
				foreach ( var kvp in Cache ) {
					ResourceUnloaded.Publish( new ResourceUnloadedEventData<TResource>( kvp.Key, kvp.Value.Resource, kvp.Value.MemorySize, UnloadReason.Manual ) );
					kvp.Value.Resource.Dispose();
				}
			}
			finally {
				CacheLock.ExitWriteLock();
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
			if ( CurrentMemorySize <= _policy.MaxMemorySize && Cache.Count <= _policy.MaxResourceCount ) {
				return;
			}

			CacheLock.EnterWriteLock();
			try {
				var candidates = Cache.Where( kvp => kvp.Value.ReferenceCount == 0 ).ToList();
				if ( !candidates.Any() ) {
					return;
				}

				IEnumerable<KeyValuePair<string, CacheEntry<TResource>>> sortedCandidates = _policy.EvictionPolicy switch {
					EvictionPolicy.LRU => candidates.OrderBy( kvp => kvp.Value.LastAccessTime ),
					EvictionPolicy.LFU => candidates.OrderBy( kvp => kvp.Value.AccessCount ),
					EvictionPolicy.SizeBased => candidates.OrderByDescending( kvp => kvp.Value.MemorySize ),
					_ => candidates.OrderByDescending( kvp => kvp.Value.LastAccessTime )
				};

				foreach ( var candidate in sortedCandidates ) {
					if ( CurrentMemorySize <= _policy.MaxMemorySize && Cache.Count <= _policy.MaxResourceCount ) {
						break;
					}
					if ( Cache.TryRemove( candidate.Key, out var entry ) ) {
						CurrentMemorySize -= entry.MemorySize;
						ResourceUnloaded.Publish( new ResourceUnloadedEventData<TResource>(
							candidate.Key, entry.Resource, entry.MemorySize, UnloadReason.CacheFull
						) );
						entry.Resource.Dispose();
					}
				}
			}
			finally {
				CacheLock.ExitWriteLock();
			}
		}

		/*
		===============
		LoadResourceAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="typeHint"></param>
		/// <param name="useSubThreads"></param>
		/// <param name="cacheMode"></param>
		/// <returns></returns>
		private async Task<TResource> LoadResourceAsync( string path, string typeHint = "", bool useSubThreads = false, Godot.ResourceLoader.CacheMode cacheMode = Godot.ResourceLoader.CacheMode.Reuse ) {
			Godot.Error requestError = Godot.ResourceLoader.LoadThreadedRequest( path, typeHint, useSubThreads, cacheMode );
			if ( requestError != Godot.Error.Ok ) {
				Logger?.PrintError( $"BaseCache.LoadResourceAsync: load_threaded_request failed with error code '{requestError}'" );
				return default;
			}

			Godot.ResourceLoader.ThreadLoadStatus status = Godot.ResourceLoader.ThreadLoadStatus.Failed;
			Godot.SceneTree sceneTree = (Godot.SceneTree)Godot.Engine.GetMainLoop();

			do {
				if ( status == Godot.ResourceLoader.ThreadLoadStatus.InProgress ) {
					await sceneTree.ToSignal( sceneTree, Godot.SceneTree.SignalName.ProcessFrame );
				}
				status = Godot.ResourceLoader.LoadThreadedGetStatus( path );
			} while ( status == Godot.ResourceLoader.ThreadLoadStatus.InProgress );

			if ( status != Godot.ResourceLoader.ThreadLoadStatus.Loaded ) {
				Logger?.PrintError( $"BaseCache.LoadResourceAsync: resource '{path}' failed to load with status '{status}'" );
			}
			return status == Godot.ResourceLoader.ThreadLoadStatus.Loaded ? (TResource?)Godot.ResourceLoader.LoadThreadedGet( path ) : default;
		}

		/*
		===============
		LoadAndCacheResource
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private TResource? LoadAndCacheResource( string path ) {
			Stopwatch loadTimer = Stopwatch.StartNew();

			try {
				TResource resource = Godot.ResourceLoader.Load<TResource>( path );
				if ( resource == null ) {
					Logger?.PrintError( $"BaseCache.LoadAndCacheResource: failed to load resource '{path}'" );
					return null;
				}

				long memorySize = CalculateMemorySize( resource );
				CacheResource( path, resource, memorySize, loadTimer.Elapsed );

				return resource;
			} catch ( Exception e ) {
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
		/// <param name="path"></param>
		/// <param name="progress"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		private async Task<TResource> LoadAndCacheResourceAsync( string path, IProgress<ResourceLoadProgressEventData> progress = null, CancellationToken cancellationToken = default ) {
			CacheEntry<TResource> entry = new CacheEntry<TResource> { LoadState = ResourceLoadState.Loading };
			Cache[ path ] = entry;

			progress?.Report( new ResourceLoadProgressEventData( path, 0.0f, ResourceLoadState.Queued ) );

			Stopwatch loadTimer = Stopwatch.StartNew();
			try {
				TResource resource = await LoadResourceAsync( path );
				cancellationToken.ThrowIfCancellationRequested();

				if ( resource == null ) {
					Logger?.PrintError( $"BaseCache.LoadAndCacheResourceAsync: failed to load resource '{path}'" );
					return null;
				}
				progress?.Report( new ResourceLoadProgressEventData( path, 0.0f, ResourceLoadState.Processing ) );

				long memorySize = CalculateMemorySize( resource );
				CacheResource( path, resource, memorySize, loadTimer.Elapsed );

				return resource;
			} catch ( Exception e ) {
				loadTimer.Stop();

				Cache.TryRemove( path, out _ );
				ResourceLoadFailed.Publish( new ResourceLoadFailedEventData( path, e.Message, e ) );

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
		/// <param name="path"></param>
		/// <param name="resource"></param>
		/// <param name="memorySize"></param>
		/// <param name="loadTime"></param>
		private void CacheResource( string path, TResource resource, long memorySize, TimeSpan loadTime ) {
			CacheLock.EnterWriteLock();
			try {
				CacheEntry<TResource> entry = new CacheEntry<TResource> {
					Resource = resource,
					ReferenceCount = 1,
					MemorySize = memorySize,
					LastAccessTime = DateTime.UtcNow,
					AccessCount = 1,
					LoadState = ResourceLoadState.Complete
				};

				Cache[ path ] = entry;
				CurrentMemorySize += memorySize;
				Interlocked.Increment( ref TotalLoaded );
				TotalLoadTime += loadTime;

				ResourceLoaded.Publish( new ResourceLoadedEventData<TResource>( path, resource, loadTime, memorySize ) );

				// flush the cache if needed
				EvictIfNeeded();
			}
			finally {
				CacheLock.ExitWriteLock();
			}
		}

		/*
		===============
		CalculateMemorySize
		===============
		*/
		protected virtual long CalculateMemorySize( TResource resource ) {
			return 0;
		}

		/*
		===============
		UpdateAccessStats
		===============
		*/
		private void UpdateAccessStats( CacheEntry<TResource> entry ) {
			entry.LastAccessTime = DateTime.UtcNow;
			entry.AccessCount++;
		}
	};
};