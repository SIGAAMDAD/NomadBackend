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

using NomadCore.Domain.Models.Interfaces;
using NomadCore.Interfaces.Common;
using NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects;
using NomadCore.Systems.EntitySystem.Interfaces;
using System;
using System.Collections.Generic;

namespace NomadCore.Systems.EntitySystem.Infrastructure {
	/*
	===================================================================================
	
	ComponentStore
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class ComponentStore<T>( int initialCapacity = 2048 ) : IComponentStore
		where T : struct, IComponent, IValueObject<T>
	{
		private T[] _components = new T[ initialCapacity ];
		private readonly Dictionary<EntityId, int> _entityToIndex = new Dictionary<EntityId, int>( initialCapacity );
		private readonly List<EntityId> _indexToEntity = new List<EntityId>( initialCapacity );

		/*
		===============
		GetComponent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="entityId"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public ref T GetComponent( EntityId entityId ) {
			if ( _entityToIndex.TryGetValue( entityId, out int index ) ) {
				return ref _components[ index ];
			}
			throw new Exception( $"Component {typeof( T ).Name} not found for entity {entityId}" );
		}

		/*
		===============
		AddComponent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="entityId"></param>
		/// <param name="component"></param>
		/// <exception cref="Exception"></exception>
		public void AddComponent( EntityId entityId, T component ) {
			if ( _entityToIndex.ContainsKey( entityId ) ) {
				throw new Exception( $"Entity {entityId} already has component {typeof( T ).Name}" );
			}

			int newIndex = _indexToEntity.Count;
			_components[ newIndex ] = component;
			_entityToIndex[ entityId ] = newIndex;
			_indexToEntity.Add( entityId );

			if ( newIndex > _components.Length ) {
				Array.Resize( ref _components, _components.Length * 2 );
			}
		}

		/*
		===============
		RemoveComponent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="entityId"></param>
		public void RemoveComponent( EntityId entityId ) {
			if ( !_entityToIndex.TryGetValue( entityId, out int index ) ) {
				return;
			}
			int lastIndex = _indexToEntity.Count - 1;
			if ( index != lastIndex ) {
				EntityId lastEntityId = _indexToEntity[ lastIndex ];
				_components[ index ] = _components[ lastIndex ];
				_entityToIndex[ lastEntityId ] = index;
				_indexToEntity[ index ] = lastEntityId;
			}

			_entityToIndex.Remove( entityId );
			_indexToEntity.RemoveAt( lastIndex );
		}

		/*
		===============
		HasComponent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="entityId"></param>
		/// <returns></returns>
		public bool HasComponent( EntityId entityId ) {
			return _entityToIndex.ContainsKey( entityId );
		}
	};
};