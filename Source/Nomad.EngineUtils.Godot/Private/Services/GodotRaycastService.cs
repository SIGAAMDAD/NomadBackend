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
using System.Numerics;
using Nomad.Core.Physics;
using Nomad.Core.Physics.Services;
using Nomad.Core.Physics.ValueObjects;

namespace Nomad.EngineUtils.Godot.Private.Services
{
	/*
	===================================================================================
	
	GodotRaycastService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class GodotRaycastService : IRaycastService
	{
		private readonly global::Godot.World2D _world;

		public GodotRaycastService( global::Godot.World2D world )
		{
			_world = world ?? throw new ArgumentNullException( nameof( world ) );
		}

		public bool TryRaycast( in RaycastQuery query, out RaycastHit hit )
		{
			global::Godot.PhysicsRayQueryParameters2D parameters = global::Godot.PhysicsRayQueryParameters2D.Create(
				query.Origin.ToGodot(),
				query.GetEndPoint().ToGodot(),
				query.LayerMask.Value
			);

			parameters.CollideWithAreas = query.Flags.HasFlag( RaycastFlags.HitTriggers );
			parameters.CollideWithBodies = query.Flags.HasFlag( RaycastFlags.HitBackfaces );
			parameters.HitFromInside = query.Flags.HasFlag( RaycastFlags.StartInsideColliders );

			global::Godot.Collections.Dictionary result = _world.DirectSpaceState.IntersectRay( parameters );
			if ( result.Count == 0 ) {
				hit = default;
				return false;
			}

			Vector2 point = result["position"].AsVector2().ToSystem();
			Vector2 normal = result["normal"].AsVector2().ToSystem();
			global::Godot.CollisionObject2D collider = (global::Godot.CollisionObject2D)result["collider"].AsGodotObject();
			PhysicsObjectHandle colliderHandle = new( (long)collider.GetInstanceId() );

			int layer = ExtractFirstLayerBit( collider.CollisionLayer );
			float distance = Vector2.Distance( query.Origin, point );

			hit = new RaycastHit(
				point: point,
				normal: normal,
				distance: distance,
				layer: layer,
				collider: colliderHandle,
				body: colliderHandle
			);

			return true;
		}

		private static int ExtractFirstLayerBit( uint mask )
		{
			for ( int i = 0; i < 32; ++i ) {
				if ( ((mask >> i) & 1u) != 0u ) {
					return i;
				}
			}

			return -1;
		}
	};
};
