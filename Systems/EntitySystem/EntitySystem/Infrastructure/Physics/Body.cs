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
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects.Components;
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
	/// <param name="owner"></param>
	/// <param name="system"></param>
	/// <param name="characterBody"></param>

	internal sealed class Body : PhysicsEntity {
		public IReadOnlyList<Rid> Shapes => _shapes;
		private readonly List<Rid> _shapes;

		private readonly int _maxSteps;

		private bool _isOnFloor;
		private bool _isOnWall;
		private bool _isOnCeiling;

		/*
		===============
		Body
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="characterBody"></param>
		public Body( IGameEntity owner, CharacterBody2D characterBody )
			: base( owner, PhysicsServer2D.BodyCreate(), characterBody )
		{
			AddShapesToBody( _physicsRid, characterBody.GetChildren() );
			_shapes = GetBodyShapes( _physicsRid );
			_maxSteps = (int)PhysicsServer2D.SpaceGetParam( characterBody.GetWorld2D().Space, PhysicsServer2D.SpaceParameter.SolverIterations );
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
		public void Update( float deltaTime ) {
			if ( !_owner.TryGetTarget( out var owner ) ) {
				return;
			}

			ref var velocityComponent = ref owner.GetComponent<VelocityComponent>();
			ref var transformComponent = ref owner.GetComponent<TransformComponent>();

			PhysicsTestMotionParameters2D motionTest = new PhysicsTestMotionParameters2D() {
				From = new Transform2D( transformComponent.Rotation, transformComponent.Scale, 0.0f, transformComponent.Position ),
				Motion = velocityComponent.Velocity,
			};
			PhysicsTestMotionResult2D result = new PhysicsTestMotionResult2D();
			if ( PhysicsServer2D.BodyTestMotion( _physicsRid, motionTest, result ) ) {

			}
			MoveAndSlide( deltaTime, owner );
		}

		protected override void SetCollisionLayer( uint collisionLayer ) {
			PhysicsServer2D.BodySetCollisionLayer( _physicsRid, collisionLayer );
		}

		protected override void SetCollisionMask( uint collisionMask ) {
			PhysicsServer2D.BodySetCollisionMask( _physicsRid, collisionMask );
		}

		protected override void SetCollisionPriority( float collisionPriority ) {
			PhysicsServer2D.BodySetCollisionPriority( _physicsRid, collisionPriority );
		}

		protected override void SetTransform( Transform2D transform ) {
			for ( int i = 0; i < _shapes.Count; i++ ) {
				PhysicsServer2D.BodySetShapeTransform( _shapes[ i ], i, transform );
			}
		}

		private void MoveAndSlide( float deltaTime, IGameEntity owner ) {
			_isOnFloor = false;
			_isOnWall = false;
			_isOnCeiling = false;

			ref var velocityComponent = ref owner.GetComponent<VelocityComponent>();
			Vector2 motion = velocityComponent.Velocity * deltaTime;

			ref var transformComponent = ref owner.GetComponent<TransformComponent>();

			for ( int i = 0; i < _maxSteps; i++ ) {
				if ( motion.Length() < 0.001f ) {
					break;
				}

				PhysicsTestMotionParameters2D parameters = new PhysicsTestMotionParameters2D {
					From = new Transform2D( transformComponent.Rotation, transformComponent.Scale, 0.0f, transformComponent.Position ),
					Motion = motion,
					Margin = 0.001f,
					CollideSeparationRay = true,
				};

				PhysicsTestMotionResult2D result = new PhysicsTestMotionResult2D();

				bool collision = PhysicsServer2D.BodyTestMotion( _physicsRid, parameters, result );
				if ( !collision ) {
					transformComponent.Position += motion;
					break;
				}

				transformComponent.Position += result.GetTravel();

				Vector2 collisionNormal = result.GetCollisionNormal();
				Vector2 collisionPoint = result.GetCollisionPoint();
				ulong colliderId = result.GetColliderId();
			}
		}

		private void UpdateCollisionStates( Vector2 normal ) {
			float floorAngle = MathF.Acos( normal.Dot( Vector2.Up ) );
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