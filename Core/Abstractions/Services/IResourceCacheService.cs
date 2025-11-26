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

using NomadCore.Infrastructure.Events;
using NomadCore.Interfaces;
using NomadCore.Interfaces.EventSystem;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NomadCore.Abstractions.Services {
	/*
	===================================================================================
	
	IResourceCacheService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public interface IResourceCacheService<TResource> : IGameService where TResource : Godot.Resource {
		public IGameEvent<ResourceLoadedEventData<TResource>> ResourceLoaded { get; }
		public IGameEvent<ResourceUnloadedEventData<TResource>> ResourceUnloaded { get; }

		public long CurrentCacheSize { get; }

		public TResource GetCached( string? path );
		public Task<TResource> GetCachedAsync( string? path, IProgress<ResourceLoadProgressEventData>? progress = null, CancellationToken cancellationToken = default );
		public bool TryGetCached( string? path, out TResource resource );
		public void Preload( string? path );
		public void Unload( string? path );
		public void UnloadAll();
		public void ClearUnused();

		public void AddReference( string? path );
		public int GetReferenceCount( string? path );
		public void ReleaseReference( string? path );

		public void Preload( params string[] paths );
		public Task PreloadAsync( params string[] paths );
	};
};