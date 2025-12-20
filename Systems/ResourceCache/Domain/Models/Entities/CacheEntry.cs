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

using NomadCore.Domain.Models.Interfaces;
using NomadCore.Domain.Models.ValueObjects;
using NomadCore.GameServices;
using NomadCore.Interfaces.Common;
using NomadCore.Systems.ResourceCache.Domain.Models.ValueObjects;
using System;
using System.Threading.Tasks;

namespace NomadCore.Systems.ResourceCache.Domain.Models.Entities {
	/*
	===================================================================================
	
	CacheEntry
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class CacheEntry<TResource, TId>( IResourceCacheService<TResource, TId> owner, TId id, TResource cached, int memorySize, TimeSpan loadTime, ResourceLoadState loadState ) : ICacheEntry<TResource, TId>
		where TResource : notnull, IDisposable
		where TId : IEquatable<TId>
	{
		public TId Id => id;
		public DateTime CreatedAt => _createdAt;
		private readonly DateTime _createdAt = DateTime.UtcNow;

		public DateTime? ModifiedAt => _accessStats.LastAccessTime;
		public int Version => _accessStats.AccessCount;

		public EntryAccessStatistics AccessStats {
			get {
				lock ( _statsLock ) {
					return _accessStats;
				}
			}
		}
		private EntryAccessStatistics _accessStats;

		public int ReferenceCount = 1;
		public TimeSpan LoadTimer = loadTime;
		public ResourceLoadState LoadState { get; } = loadState;

		public readonly int MemorySize = memorySize;

		private readonly object _statsLock = new object();

		/*
		===============
		Get
		===============
		*/
		public void Get( out TResource resource ) {
			UpdateAccessStats();
			resource = cached;
		}

		/*
		===============
		GetAsync
		===============
		*/
		public async ValueTask<TResource> GetAsync() {
			UpdateAccessStats();
			return cached;
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			cached?.Dispose();
			ReferenceCount = 0;
		}

		/*
		===============
		Equals
		===============
		*/
		public bool Equals( IEntity<TId>? other ) {
			return other is not null && other.Id.Equals( Id );
		}

		/*
		===============
		UpdateAccessStats
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void UpdateAccessStats() {
			lock ( _statsLock ) {
				_accessStats = _accessStats with {
					LastAccessTime = DateTime.UtcNow,
					AccessCount = AccessStats.AccessCount + 1
				};
			}
		}
	};
};