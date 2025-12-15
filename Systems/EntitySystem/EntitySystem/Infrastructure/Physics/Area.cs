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
using NomadCore.GameServices;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Systems.EntitySystem.Domain.Events;
using System;
using System.Collections.Generic;

namespace NomadCore.Systems.EntitySystem.Infrastructure.Physics {
	/*
	===================================================================================
	
	Area
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class Area : PhysicsEntity {
		public IReadOnlyList<Rid> Shapes => _shapes;
		private readonly List<Rid> _shapes;

		private readonly Rid _physicsSpace;

		public IGameEvent<AreaEnteredEventData> AreaEntered => _areaEntered;
		private readonly IGameEvent<AreaEnteredEventData> _areaEntered;

		public IGameEvent<AreaExitedEventData> AreaExited => _areaExited;
		private readonly IGameEvent<AreaExitedEventData> _areaExited;

		/*
		===============
		Area
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="area"></param>
		public Area( IGameEventRegistryService eventFactory, IGameEntity owner, Area2D area )
			: base( owner, PhysicsServer2D.AreaCreate(), area )
		{
			AddShapesToArea( _physicsRid, area.GetChildren() );
			_shapes = GetAreaShapes( _physicsRid );

			_physicsSpace = area.GetWorld2D().Space;
			PhysicsServer2D.AreaSetSpace( _physicsRid, _physicsSpace );

			_areaEntered = eventFactory.GetEvent<AreaEnteredEventData>( nameof( AreaEnteredEventData ) );
			_areaExited = eventFactory.GetEvent<AreaExitedEventData>( nameof( AreaExitedEventData ) );

			area.CallDeferred( Area2D.MethodName.QueueFree );
		}

		/*
		===============
		Updaet
		===============
		*/
		public void Update( float delta ) {
		}

		/*
		===============
		SetCollisionLayer
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="collisionLayer"></param>
		protected override void SetCollisionLayer( uint collisionLayer ) {
			PhysicsServer2D.AreaSetCollisionLayer( _physicsRid, collisionLayer );
		}

		/*
		===============
		SetCollisionMask
		===============
		*/
		protected override void SetCollisionMask( uint collisionMask ) {
			PhysicsServer2D.AreaSetCollisionMask( _physicsRid, collisionMask );
		}

		/*
		===============
		SetCollisionPriority
		===============
		*/
		protected override void SetCollisionPriority( float collisionPriority ) {
			PhysicsServer2D.AreaSetParam( _physicsRid, PhysicsServer2D.AreaParameter.Priority, collisionPriority );
		}

		/*
		===============
		SetTransform
		===============
		*/
		protected override void SetTransform( Transform2D transform ) {
			PhysicsServer2D.AreaSetTransform( _physicsRid, transform );
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