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

using NomadCore.Systems.ResourceCache.Application.Interfaces;
using NomadCore.Domain.Models.ValueObjects;
using System;
using System.Threading.Tasks;
using NomadCore.Systems.ResourceCache.Domain.Models.ValueObjects;
using System.Threading;

namespace NomadCore.Systems.ResourceCache.Infrastructure.Godot {
	/*
	===================================================================================
	
	GodotLoader
	
	===================================================================================
	*/
	/// <summary>
	/// A godot-focused resource loader.
	/// </summary>
	
	public sealed class GodotLoader<Resource> : IResourceLoader<Resource, FilePath>
		where Resource : global::Godot.Resource
	{
		public IResourceLoader<Resource, FilePath>.LoadCallback Load => LoadResource;
		public IResourceLoader<Resource, FilePath>.LoadAsyncCallback LoadAsync => LoadResourceAsync;

		/*
		===============
		LoadResource
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private Result<Resource> LoadResource( FilePath path ) {
			var resource = global::Godot.ResourceLoader.Load( path.GodotPath, String.Empty, global::Godot.ResourceLoader.CacheMode.ReplaceDeep );
			if ( resource == null ) {
				return Result<Resource>.Failure( LoadError.Create( $"Error loading godot resource '{path}'" ) );
			}
			return Result<Resource>.Success( resource as Resource );
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
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<Result<Resource>> LoadResourceAsync( FilePath path, CancellationToken ct = default ) {
			global::Godot.Error requestError = global::Godot.ResourceLoader.LoadThreadedRequest( path, String.Empty, true, global::Godot.ResourceLoader.CacheMode.ReplaceDeep );
			if ( requestError != global::Godot.Error.Ok ) {
				return Result<Resource>.Failure( LoadError.Create( $"Error loading godot resource '{path}' - {requestError}" ) );
			}

			global::Godot.ResourceLoader.ThreadLoadStatus status = global::Godot.ResourceLoader.ThreadLoadStatus.Failed;
			global::Godot.SceneTree sceneTree = (global::Godot.SceneTree)global::Godot.Engine.GetMainLoop();

			do {
				if ( status == global::Godot.ResourceLoader.ThreadLoadStatus.InProgress ) {
					await sceneTree.ToSignal( sceneTree, global::Godot.SceneTree.SignalName.ProcessFrame );
				}
				status = global::Godot.ResourceLoader.LoadThreadedGetStatus( path );
			} while ( status == global::Godot.ResourceLoader.ThreadLoadStatus.InProgress );

			if ( status == global::Godot.ResourceLoader.ThreadLoadStatus.Loaded ) {
				return Result<Resource>.Success( global::Godot.ResourceLoader.LoadThreadedGet( path ) as Resource );
			}
			return Result<Resource>.Failure( LoadError.Create( $"godot resource '{path}' failed to load with thread status '{status}" ) );
		}
	};
};