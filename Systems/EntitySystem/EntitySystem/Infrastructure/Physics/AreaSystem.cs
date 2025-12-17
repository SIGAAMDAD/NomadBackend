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

using Godot;
using System.Collections.Generic;
using System;
using NomadCore.Systems.EntitySystem.Domain;
using NomadCore.Systems.EntitySystem.Domain.Events;
using NomadCore.Infrastructure.Memory;

namespace NomadCore.Systems.EntitySystem.Infrastructure.Physics {
	/*
	===================================================================================
	
	AreaSystem
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class AreaSystem( Rid space ) : PhysicsSubSystem( space ) {
		private sealed class OverlapSet : PoolableObject {
			public readonly HashSet<Entity> Data = new HashSet<Entity>();

			public bool Add( Entity item ) => Data.Add( item );
			public bool Remove( Entity item ) => Data.Remove( item );
			public bool Contains( Entity item ) => Data.Contains( item );
		};
		private readonly Dictionary<Entity, Area> _areas = new Dictionary<Entity, Area>();

		private readonly Dictionary<Entity, OverlapSet> _currentOverlaps = new Dictionary<Entity, OverlapSet>();
		private readonly BasicObjectPool<OverlapSet> _overlapPool = new BasicObjectPool<OverlapSet>( () => new OverlapSet() );
		
		/*
		===============
		Update
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="delta"></param>
		public override void Update( float delta ) {
			CheckAreaOverlaps();
		}

		/*
		===============
		CheckAreaOverlaps
		===============
		*/
		private void CheckAreaOverlaps() {
			//FIXME: maybe just cache this?
			PhysicsDirectSpaceState2D spaceState = PhysicsServer2D.SpaceGetDirectState( _physicsSpace );

			foreach ( var ( entity, area ) in _areas ) {
				OverlapSet newOverlaps = QueryAreaOverlaps( spaceState, entity, area );
				OverlapSet previousOverlaps = _currentOverlaps[ entity ];

				foreach ( var bodyEntity in newOverlaps.Data ) {
					if ( !previousOverlaps.Contains( bodyEntity ) ) {
						area.AreaEntered.Publish( new AreaEnteredEventData( entity, bodyEntity ) );
					}
				}
				foreach ( var bodyEntity in previousOverlaps.Data ) {
					if ( !newOverlaps.Contains( bodyEntity ) ) {
						area.AreaExited.Publish( new AreaExitedEventData( entity, bodyEntity ) );
					}
				}

				_overlapPool.Return( previousOverlaps );
				_currentOverlaps[ entity ] = newOverlaps;
			}
		}

		/*
		===============
		QueryAreaOverlaps
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="spaceState"></param>
		/// <param name="entity"></param>
		/// <returns></returns>
		private OverlapSet QueryAreaOverlaps( PhysicsDirectSpaceState2D spaceState, Entity entity, Area area ) {
			var overlaps = _overlapPool.Rent();
			var areaData = _areas[ entity ];

			int count = areaData.Shapes.Count;
			for ( int i = 0; i < count; i++ ) {
				PhysicsShapeQueryParameters2D queryParams = new PhysicsShapeQueryParameters2D {
					ShapeRid = areaData.Shapes[ i ],
					Transform = areaData.Transform,
					CollisionMask = areaData.CollisionMask,
					CollideWithAreas = true,
					CollideWithBodies = true
				};

				var results = spaceState.IntersectShape( queryParams );
				Variant rid;

				foreach ( var result in results ) {
					if ( result.TryGetValue( nameof( rid ), out rid ) && _ridToEntity.TryGetValue( rid.AsRid(), out var bodyEntity ) ) {
						overlaps.Add( bodyEntity );
					}
				}
			}

			return overlaps;
		}

		/*
		===============
		GetAreaShapes
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="areaRid"></param>
		/// <returns></returns>
		private static List<Rid> GetAreaShapes( Rid areaRid ) {
			int shapeCount = PhysicsServer2D.AreaGetShapeCount( areaRid );
			List<Rid> shapes = new List<Rid>( shapeCount );
			
			for ( int i = 0; i < shapeCount; i++ ) {
				shapes.Add( PhysicsServer2D.AreaGetShape( areaRid, i ) );
			}

			return shapes;
		}

		/*
		===============
		CreateShape
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="shape"></param>
		/// <returns></returns>
		private static Rid CreateShape( Shape2D shape ) {
			Rid shapeRid = shape.GetRid();
			if ( !shapeRid.IsValid ) {
				switch ( shape ) {
					case CircleShape2D circleShape:
						shapeRid = PhysicsServer2D.CircleShapeCreate();
						PhysicsServer2D.ShapeSetData( shapeRid, circleShape.Radius );
						break;
					case SegmentShape2D segmentShape:
						shapeRid = PhysicsServer2D.SegmentShapeCreate();
						PhysicsServer2D.ShapeSetData( shapeRid, new Rect2( position: segmentShape.A, size: segmentShape.B ) );
						break;
					case RectangleShape2D rectangleShape:
						shapeRid = PhysicsServer2D.RectangleShapeCreate();
						PhysicsServer2D.ShapeSetData( shapeRid, rectangleShape.Size );
						break;
					case CapsuleShape2D capsuleShape:
						shapeRid = PhysicsServer2D.CapsuleShapeCreate();
						PhysicsServer2D.ShapeSetData( shapeRid, new Vector2( capsuleShape.Radius, capsuleShape.Height ) );
						break;
					case ConcavePolygonShape2D concavePolygonShape:
						shapeRid = PhysicsServer2D.ConcavePolygonShapeCreate();
						PhysicsServer2D.ShapeSetData( shapeRid, concavePolygonShape.Segments );
						break;
					default:
						throw new InvalidCastException( nameof( shape ) );
				}
			}
			return shapeRid;
		}

		/*
		===============
		AddShapesToArea
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="areaRid"></param>
		/// <param name="children"></param>
		private static void AddShapesToArea( Rid areaRid, Godot.Collections.Array<Node> children ) {
			int count = children.Count;
			for ( int i = 0; i < count; i++ ) {
				if ( children[ i ] is CollisionShape2D collisionShape && collisionShape.Shape != null ) {
					PhysicsServer2D.AreaAddShape( areaRid, CreateShape( collisionShape.Shape ) );
				} else if ( children[ i ] is CollisionPolygon2D polygonShape && polygonShape.Polygon != null ) {
					ConcavePolygonShape2D shape = new ConcavePolygonShape2D();
					shape.Segments = polygonShape.Polygon;
					PhysicsServer2D.AreaAddShape( areaRid, CreateShape( shape ) );
				}
			}
		}
	};
};