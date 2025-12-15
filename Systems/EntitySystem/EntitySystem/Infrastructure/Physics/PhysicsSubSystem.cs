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
using NomadCore.Systems.EntitySystem.Domain;
using NomadCore.Systems.EntitySystem.Interfaces;
using System.Collections.Generic;

namespace NomadCore.Systems.EntitySystem.Infrastructure.Physics {
	/*
	===================================================================================
	
	PhysicsSubSystem
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal abstract class PhysicsSubSystem( Rid space ) : IPhysicsSubSystem {
		protected readonly Rid _physicsSpace = space;
		protected readonly Dictionary<Entity, Rid> _entityToRid = new Dictionary<Entity, Rid>();
		protected readonly Dictionary<Rid, Entity> _ridToEntity = new Dictionary<Rid, Entity>();

		/*
		===============
		Update
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="delta"></param>
		public abstract void Update( float delta );
		
		/*
		===============
		Shutdown
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public virtual void Shutdown() {
			foreach ( var rid in _entityToRid ) {
				PhysicsServer2D.FreeRid( rid.Value );
			}
			_entityToRid.Clear();
			_ridToEntity.Clear();
		}

		/*
		===============
		RegisterEntity
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="rid"></param>
		protected void RegisterEntity( Entity entity, Rid rid ) {
			_entityToRid[ entity ] = rid;
			_ridToEntity[ rid ] = entity;
		}

		/*
		===============
		UnregisterEntity
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="entity"></param>
		protected void UnregisterEntity( Entity entity ) {
			if ( _entityToRid.TryGetValue( entity, out Rid rid ) ) {
				_entityToRid.Remove( entity );
				_ridToEntity.Remove( rid );
				PhysicsServer2D.FreeRid( rid );
			}
		}
	};
};