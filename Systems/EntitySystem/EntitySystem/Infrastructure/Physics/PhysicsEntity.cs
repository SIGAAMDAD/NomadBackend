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
using System;
using NomadCore.Systems.EntitySystem.Domain.Models.Interfaces;
using NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects;
using NomadCore.Interfaces.Common;

namespace NomadCore.Systems.EntitySystem.Infrastructure.Physics {
	/*
	===================================================================================
	
	PhysicsEntity
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal abstract class PhysicsEntity : IPhysicsEntity {
		public uint CollisionLayer {
			get => _collisionLayer;
			set {
				if ( _collisionLayer == value ) {
					return;
				}
				_collisionLayer = value;
				SetCollisionLayer( _collisionLayer );
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
				SetCollisionMask( _collisionMask );
			}
		}
		private uint _collisionMask;

		public float CollisionPriority {
			get => _collisionPriority;
			set {
				if ( _collisionPriority == value ) {
					return;
				}
				_collisionPriority = value;
				SetCollisionPriority( _collisionPriority );
			}
		}
		private float _collisionPriority;

		public Transform2D Transform {
			get => _transform;
			set {
				if ( _transform == value ) {
					return;
				}
				_transform = value;
				SetTransform( _transform );
			}
		}
		private Transform2D _transform;

		public bool Enabled => _active;
		protected bool _active = true;

		public Rid BodyRid => _physicsRid;

		public PhysicsEntityId Id => throw new NotImplementedException();

		public DateTime CreatedAt => throw new NotImplementedException();

		public DateTime? ModifiedAt => throw new NotImplementedException();

		public int Version => throw new NotImplementedException();

		protected readonly Rid _physicsRid;

		protected WeakReference<IGameEntity> _owner;

		/*
		===============
		PhysicsEntity
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="body"></param>
		/// <param name="collisionObject"></param>
		public PhysicsEntity( IGameEntity owner, Rid body, CollisionObject2D collisionObject ) {
			_owner = new WeakReference<IGameEntity>( owner );

			_physicsRid = body;

			_collisionLayer = collisionObject.CollisionLayer;
			_collisionMask = collisionObject.CollisionMask;
			_collisionPriority = collisionObject.CollisionPriority;
			_transform = collisionObject.Transform;

			SetCollisionLayer( _collisionLayer );
			SetCollisionMask( _collisionMask );
			SetCollisionPriority( _collisionPriority );
			SetTransform( _transform );
		}

		/*
		===============
		Dispose
		===============
		*/
		public virtual void Dispose() {
			if ( _physicsRid.IsValid ) {
				PhysicsServer2D.FreeRid( _physicsRid );
			}
			GC.SuppressFinalize( this );
		}

		protected abstract void SetCollisionLayer( uint collisionLayer );
		protected abstract void SetCollisionMask( uint collisionMask );
		protected abstract void SetCollisionPriority( float collisionPriority );
		protected abstract void SetTransform( Transform2D transform );

		public bool Equals( IEntity<PhysicsEntityId>? other ) {
			return other?.Id == Id;
		}
	};
};