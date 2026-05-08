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
using System.Threading.Tasks;
using System.Threading;
using Nomad.Core.ResourceCache;
using Nomad.Core.Util;
using Godot;

namespace Nomad.EngineUtils.Godot.Private
{
	/*
    ===================================================================================

    GodotLoader

    ===================================================================================
    */
	/// <summary>
	/// A godot-focused resource loader.
	/// </summary>

	internal sealed class GodotLoader : IResourceLoader
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Result<TResource> Load<TResource, TId>( TId id )
		{
			if ( id is not string path ) {
				throw new InvalidCastException();
			}
			if ( ResourceLoader.Load( path, string.Empty, ResourceLoader.CacheMode.ReplaceDeep ) is TResource resource ) {
				if ( resource == null ) {
					return Result<TResource>.Failure( InternalError.Create( $"Error loading godot resource '{path}'" ) );
				}
				return Result<TResource>.Success( resource );
			} else {
				throw new InvalidCastException();
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
		/// <param name="id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<Result<TResource>> LoadAsync<TResource, TId>( TId id, CancellationToken ct = default )
		{
			ct.ThrowIfCancellationRequested();

			if ( id is not string path ) {
				throw new InvalidCastException();
			}

			Error requestError = ResourceLoader.LoadThreadedRequest( path, string.Empty, true, ResourceLoader.CacheMode.ReplaceDeep );
			if ( requestError != Error.Ok ) {
				return Result<TResource>.Failure( InternalError.Create( $"Error loading godot resource '{path}' - {requestError}" ) );
			}

			ResourceLoader.ThreadLoadStatus status = ResourceLoader.ThreadLoadStatus.Failed;
			var sceneTree = (SceneTree)global::Godot.Engine.GetMainLoop();

			do {
				if ( status == ResourceLoader.ThreadLoadStatus.InProgress ) {
					await sceneTree.ToSignal( sceneTree, SceneTree.SignalName.ProcessFrame );
				}
				status = ResourceLoader.LoadThreadedGetStatus( path );
			} while ( status == ResourceLoader.ThreadLoadStatus.InProgress );

			if ( status == ResourceLoader.ThreadLoadStatus.Loaded ) {
				if ( ResourceLoader.LoadThreadedGet( path ) is TResource resource ) {
					return Result<TResource>.Success( resource );
				}
				throw new InvalidCastException();
			}
			return Result<TResource>.Failure( InternalError.Create( $"godot resource '{path}' failed to load with thread status '{status}" ) );
		}
	};
};
