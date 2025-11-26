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
using NomadCore.Abstractions.Services;
using NomadCore.Infrastructure;
using NomadCore.Interfaces.Audio;
using NomadCore.Enums.Audio;
using NomadCore.Systems.EntitySystem.Infrastructure.Physics;
using NomadCore.Systems.EntitySystem.Interfaces;
using NomadCore.Systems.EntitySystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.EntitySystem.Common {
	/*
	===================================================================================
	
	Entity
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	/// <remarks>
	/// 
	/// </remarks>
	/// <param name="id"></param>

	public partial class Entity : IEntity, IDisposable {
		public int Id => _id;
		private readonly int _id;

		public Node2D Node => _node;
		private readonly Node2D _node;

		public IPhysicsBody PhysicsBody => _physicsBody;
		private readonly IPhysicsBody _physicsBody;

		public readonly IAudioSource AudioPlayer;

		protected readonly Dictionary<Type, IComponent> Components = new Dictionary<Type, IComponent>();
		protected readonly EntityComponentSystem? Service = (EntityComponentSystem?)ServiceRegistry.Get<IEntityService>();

		/*
		===============
		Entity
		===============
		*/
		private Entity( Node2D entityNode, int hashCode ) {
			_id = hashCode;

			_node = new Node2D() {
				Name = $"Entity{_id}"
			};
			entityNode.CallDeferred( Node2D.MethodName.AddChild, _node );

			AudioPlayer = ServiceRegistry.Get<IAudioService>().CreateSource( SourceType.Entity );
		}

		/*
		===============
		Entity
		===============
		*/
		internal Entity( Node2D entityNode, Area2D area )
			: this( entityNode, area.GetPath().GetHashCode() )
		{
			_physicsBody = Area.Convert( area );
		}

		/*
		===============
		Entity
		===============
		*/
		internal Entity( Node2D entityNode, CharacterBody2D characterBody2D )
			: this( entityNode, characterBody2D.GetPath().GetHashCode() )
		{
			_physicsBody = Body.Convert( this, characterBody2D );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			Components.Clear();
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
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public T AddComponent<T>() where T : IComponent, new() {
			Type type = typeof( T );
			if ( Components.TryGetValue( type, out var component ) ) {
				return (T)component;
			}
			component = new T();
			Components[ type ] = component;
			return (T)component;
		}

		/*
		===============
		RemoveComponent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <exception cref="KeyNotFoundException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void RemoveComponent<T>() where T : IComponent, new() {
			Type type = typeof( T );
			if ( !Components.ContainsKey( type ) ) {
				throw new KeyNotFoundException( $"Component {type} doesn't exist" );
			}
			Components.Remove( type );
		}

		/*
		===============
		GetComponent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="KeyNotFoundException"></exception>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public T GetComponent<T>() where T : IComponent, new() {
			if ( Components.TryGetValue( typeof( T ), out var component ) ) {
				return (T)component;
			}
			return default;
		}

		/*
		===============
		TryGetComponent
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool TryGetComponent<T>( out T? component ) where T : IComponent, new() {
			if ( Components.TryGetValue( typeof( T ), out var value ) ) {
				component = (T)value;
				return true;
			}
			component = default;
			return false;
		}

		/*
		===============
		GetComponentCount
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public int GetComponentCount() {
			return Components.Count;
		}
		
		/*
		===============
		GetComponentAtIndex
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="index"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public T GetComponentAtIndex<T>( int index ) where T : IComponent, new() {
			return (T)Components.ElementAt( index ).Value;
		}

		/*
		===============
		GetComponents
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T[] GetComponents<T>() where T : IComponent, new() {
			List<T> components = new List<T>();
			Type type = typeof( T );
			foreach ( var component in Components ) {
				if ( component.Key == type ) {
					components.Add( (T)component.Value );
				}
			}
			return [ .. components ];
		}
	};
};