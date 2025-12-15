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
using NomadCore.Systems.EntitySystem.Interfaces;
using System;
using System.Collections.Concurrent;

namespace NomadCore.Systems.EntitySystem.Infrastructure {
	/*
	===================================================================================
	
	ComponentManager
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class ComponentManager {
		private readonly ConcurrentDictionary<Type, IComponentStore> _stores = new ConcurrentDictionary<Type, IComponentStore>();

		/*
		===============
		GetComponent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="entity"></param>
		/// <returns></returns>
		public ref T GetComponent<T>( IGameEntity entity ) where T : struct, IComponent, IValueObject<T> {
			var store = GetStore<T>();
			return ref store.GetComponent( entity.Id );
		}

		/*
		===============
		AddComponent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="entity"></param>
		/// <param name="component"></param>
		public void AddComponent<T>( IGameEntity entity, T component ) where T : struct, IComponent, IValueObject<T> {
			var store = GetStore<T>();
			store.AddComponent( entity.Id, component );
		}

		/*
		===============
		HasComponent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="entity"></param>
		/// <returns></returns>
		public bool HasComponent<T>( IGameEntity entity ) where T : struct, IComponent, IValueObject<T> {
			if ( _stores.TryGetValue( typeof( T ), out var store ) ) {
				return ( (ComponentStore<T>)store ).HasComponent( entity.Id );
			}
			return false;
		}

		/*
		===============
		GetStore
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		private ComponentStore<T> GetStore<T>() where T : struct, IComponent, IValueObject<T> {
			Type type = typeof( T );
			if ( _stores.TryGetValue( type, out var store ) ) {
				store = new ComponentStore<T>();
				_stores[ type ] = store;
			}
			return (ComponentStore<T>)store;
		}
	};
};