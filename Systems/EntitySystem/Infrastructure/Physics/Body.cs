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
using NomadCore.Systems.EntitySystem.Common;
using NomadCore.Systems.EntitySystem.Components;
using NomadCore.Systems.EntitySystem.Interfaces;
using System;
using System.Collections.Generic;

namespace NomadCore.Systems.EntitySystem.Infrastructure.Physics {
	/*
	===================================================================================
	
	Body
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class Body : IPhysicsBody {
		public uint CollisionLayer {
			get => _collisionLayer;
			set {
				if ( _collisionLayer == value ) {
					return;
				}
				_collisionLayer = value;
				PhysicsServer2D.BodySetCollisionLayer( _bodyRID, _collisionLayer );
			}
		}
		private uint _collisionLayer;

		public uint CollisionMask {
			get => _collisionMask;
			set {
				if ( _collisionMask == value ) {
					return;
				}
				_collisionMask = value;
				PhysicsServer2D.BodySetCollisionMask( _bodyRID, _collisionMask );
			}
		}
		private uint _collisionMask;

		public Rid Rid => _bodyRID;
		private readonly Rid _bodyRID;

		public IReadOnlyList<Rid> Shapes => _shapes;
		private readonly List<Rid> _shapes = new List<Rid>();

		public Transform2D Transform => _transform;
		private Transform2D _transform;

		private readonly Entity Owner;

		/*
		===============
		Body
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="transform"></param>
		/// <param name="bodyRid"></param>
		public Body( Entity? owner, Transform2D transform, Rid bodyRid ) {
			ArgumentNullException.ThrowIfNull( owner );
			
			Owner = owner;
			_bodyRID = bodyRid;

			_collisionLayer = PhysicsServer2D.BodyGetCollisionLayer( bodyRid );
			_collisionMask = PhysicsServer2D.BodyGetCollisionMask( bodyRid );
			_shapes = GetBodyShapes( bodyRid );
			for ( int i = 0; i < _shapes.Count; i++ ) {
				PhysicsServer2D.BodySetShapeTransform( _bodyRID, i, transform );
			}

			PhysicsServer2D.BodySetStateSyncCallback( _bodyRID, Callable.From<float>( Update ) );
		}

		/*
		===============
		Update
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="delta"></param>
		public void Update( float delta ) {
			VelocityComponent velocity = Owner.GetComponent<VelocityComponent>();
			PhysicsServer2D.BodySetAxisVelocity( _bodyRID, velocity.Velocity );

			var state = PhysicsServer2D.BodyGetState( _bodyRID, PhysicsServer2D.BodyState.Transform );
			var transform = state.AsTransform2D();

			TransformComponent component = Owner.GetComponent<TransformComponent>();
			component.Position = transform.Origin;
			component.Scale = transform.Scale;
			component.Rotation = transform.Rotation;
		}

		/*
		===============
		Convert
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="characterBody2D"></param>
		/// <returns></returns>
		public static Body Convert( Entity owner, CharacterBody2D characterBody2D ) {
			Rid bodyRid = PhysicsServer2D.BodyCreate();

			PhysicsServer2D.BodySetSpace( bodyRid, characterBody2D.GetWorld2D().Space );
			PhysicsServer2D.BodySetCollisionLayer( bodyRid, characterBody2D.CollisionLayer );
			PhysicsServer2D.BodySetCollisionMask( bodyRid, characterBody2D.CollisionMask );
			PhysicsServer2D.BodySetMode( bodyRid, PhysicsServer2D.BodyMode.Kinematic );

			Godot.Collections.Array<Node> children = characterBody2D.GetChildren();
			AddShapesToBody( bodyRid, children );

			characterBody2D.CallDeferred( CharacterBody2D.MethodName.QueueFree );

			return new Body( owner, characterBody2D.Transform, bodyRid );
		}

		/*
		===============
		GetBodyShapes
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="areaRid"></param>
		/// <returns></returns>
		private static List<Rid> GetBodyShapes( Rid bodyRid ) {
			int shapeCount = PhysicsServer2D.BodyGetShapeCount( bodyRid );
			List<Rid> shapes = new List<Rid>( shapeCount );
			
			for ( int i = 0; i < shapeCount; i++ ) {
				shapes.Add( PhysicsServer2D.BodyGetShape( bodyRid, i ) );
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
		AddShapesToBody
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bodyRid"></param>
		/// <param name="children"></param>
		private static void AddShapesToBody( Rid bodyRid, Godot.Collections.Array<Node> children ) {
			int count = children.Count;
			for ( int i = 0; i < count; i++ ) {
				if ( children[ i ] is CollisionShape2D collisionShape && collisionShape.Shape != null ) {
					PhysicsServer2D.BodyAddShape( bodyRid, CreateShape( collisionShape.Shape ) );
				} else if ( children[ i ] is CollisionPolygon2D polygonShape && polygonShape.Polygon != null ) {
					ConcavePolygonShape2D shape = new ConcavePolygonShape2D();
					shape.Segments = polygonShape.Polygon;
					PhysicsServer2D.BodyAddShape( bodyRid, CreateShape( shape ) );
				}
			}
		}
	};
};