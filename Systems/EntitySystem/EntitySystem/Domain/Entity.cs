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
using NomadCore.Systems.EntitySystem.Infrastructure.Physics;
using NomadCore.Systems.EntitySystem.Interfaces;
using NomadCore.Systems.EntitySystem.Services;
using System;
using System.Collections.Generic;
using NomadCore.Infrastructure.Rendering;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Interfaces.Common;
using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.EntitySystem.Domain.Models.Interfaces;
using NomadCore.GameServices;
using NomadCore.Systems.EntitySystem.Infrastructure.Rendering;
using NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects;
using NomadCore.Systems.EntitySystem.Application.Interfaces;
using NomadCore.Systems.EntitySystem.Infrastructure;
using Steamworks;

namespace NomadCore.Systems.EntitySystem.Domain {
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

	public class Entity : IGameEntity {
		public EntityId Id => _id;
		private readonly EntityId _id;

		public IRenderEntity RenderEntity => _renderEntity;
		protected readonly IRenderEntity _renderEntity;

		public IPhysicsEntity PhysicsBody => _body;
		protected readonly IPhysicsEntity _body;

		public DateTime CreatedAt => _createdAt;
		private readonly DateTime _createdAt = DateTime.UtcNow;

		public DateTime? ModifiedAt => _modifiedAt;
		private DateTime _modifiedAt;

		public int Version => _version;
		private int _version = 0;

		public int ComponentCount => 0;

		protected readonly IEntityComponentService _ecs;
		protected readonly IStatService _stats;

		private readonly ComponentManager _componentManager;

		/*
		===============
		Entity
		===============
		*/
		private Entity( IGameEventRegistryService eventFactory, Node2D entityNode, int hashCode ) {
			_id = new EntityId( hashCode, SceneStringPool.Intern( $"Entity{hashCode}" ) );
			_stats = new StatManager( this, eventFactory, new Dictionary<InternString, IEntityStat>() );
		}

		/*
		===============
		Entity
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="entityNode"></param>
		/// <param name="area"></param>
		/// <param name="sprite"></param>
		internal Entity( IGameEventRegistryService eventFactory, Node2D entityNode, Area2D area, Sprite2D sprite )
			: this( eventFactory, entityNode, area.GetPath().GetHashCode() )
		{
			_body = new Area( eventFactory, this, area );
			_renderEntity = new ServerSprite( eventFactory, this, sprite );
		}

		/*
		===============
		Entity
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="entityNode"></param>
		/// <param name="characterBody"></param>
		/// <param name="animatedSprite"></param>
		internal Entity( IGameEventRegistryService eventFactory, Node2D entityNode, CharacterBody2D characterBody, AnimatedSprite2D animatedSprite )
			: this( eventFactory, entityNode, characterBody.GetPath().GetHashCode() )
		{
			_body = new Body( this, characterBody );
			_renderEntity = new AnimationEntity( eventFactory, this, animatedSprite );
		}

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			GC.SuppressFinalize( this );
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
		/// <returns></returns>
		public bool HasComponent<T>() where T : struct, IComponent, IValueObject<T> {
			return _componentManager.HasComponent<T>( this );
		}

		/*
		===============
		AddComponent
		===============
		*/
		public ref T AddComponent<T>() where T : struct, IComponent, IValueObject<T> {
			if ( !_componentManager.HasComponent<T>( this ) ) {
				_componentManager.AddComponent( this, new T() );
			}
			return ref _componentManager.GetComponent<T>( this );
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
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public ref T AddComponent<T>( T defaultValue ) where T : struct, IComponent, IValueObject<T> {
			if ( !_componentManager.HasComponent<T>( this ) ) {
				_componentManager.AddComponent( this, defaultValue );
			}
			return ref _componentManager.GetComponent<T>( this );
		}

		/*
		===============
		GetOrAddComponent
		===============
		*/
		public ref T GetOrAddComponent<T>() where T : struct, IComponent, IValueObject<T> {
			return ref AddComponent<T>();
		}

		/*
		===============
		GetOrAddComponent
		===============
		*/
		public ref T GetOrAddComponent<T>( T defaultValue ) where T : struct, IComponent, IValueObject<T> {
			return ref AddComponent( defaultValue );
		}

		/*
		===============
		GetComponent
		===============
		*/
		public ref T GetComponent<T>() where T : struct, IComponent, IValueObject<T> {
			return ref _componentManager.GetComponent<T>( this );
		}

		/*
		===============
		TryGetComponent
		===============
		*/
		public bool TryGetComponent<T>( out T component ) where T : struct, IComponent, IValueObject<T> {
			if ( !_componentManager.HasComponent<T>( this ) ) {
				component = default;
				return false;
			}
			component = _componentManager.GetComponent<T>( this );
			return true;
		}

		/*
		===============
		AddComponents
		===============
		*/
		public void AddComponents( ReadOnlySpan<IComponent> components ) {
			throw new NotSupportedException();
		}

		/*
		===============
		Equals
		===============
		*/
		public bool Equals( IEntity<EntityId>? other ) {
			return other?.Id == Id;
		}
	};
};