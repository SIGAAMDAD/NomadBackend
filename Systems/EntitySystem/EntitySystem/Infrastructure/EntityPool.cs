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
using NomadCore.Interfaces.Common;
using NomadCore.Systems.EntitySystem.Application.Interfaces;
using NomadCore.Systems.EntitySystem.Common;
using NomadCore.Systems.EntitySystem.Domain;
using System;
using System.Collections.Concurrent;

namespace NomadCore.Systems.EntitySystem.Infrastructure {
	/*
	===================================================================================

	EntityPool

	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	/// <param name="eventFactory"></param>
	/// <param name="ecs"></param>
	/// <param name="maxSize"></param>

	internal sealed class EntityPool( IGameEventRegistryService eventFactory, int maxSize = int.MaxValue ) {
		public int AvailableCount => _availableObjects.Count;

		public int TotalCount => _currentSize;
		private int _currentSize = 0;

		public int ActiveObjectCount => _currentSize - _availableObjects.Count;

		private readonly ConcurrentBag<Entity> _availableObjects = new ConcurrentBag<Entity>();

		private readonly int _maxSize = maxSize;
		private bool _isDisposed;

		private readonly IGameEventRegistryService _eventFactory = eventFactory;

		/*
		===============
		Rent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="entityNode"></param>
		/// <param name="area"></param>
		/// <param name="sprite"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public Entity Rent( Node2D entityNode, Area2D area, Sprite2D sprite ) {
			ObjectDisposedException.ThrowIf( _isDisposed, this );

			if ( _availableObjects.TryTake( out Entity? obj ) ) {
				ArgumentNullException.ThrowIfNull( obj );
				return obj;
			}

			if ( _currentSize < _maxSize ) {
				_currentSize++;
				return new Entity( _eventFactory, entityNode, area, sprite );
			}
			throw new InvalidOperationException( "Object pool exhausted and maximum size reached" );
		}

		/*
		===============
		Rent
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="entityNode"></param>
		/// <param name="body"></param>
		/// <param name="animatedSprite"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public Entity Rent( Node2D entityNode, CharacterBody2D body, AnimatedSprite2D animatedSprite ) {
			ObjectDisposedException.ThrowIf( _isDisposed, this );

			if ( _availableObjects.TryTake( out Entity? obj ) ) {
				ArgumentNullException.ThrowIfNull( obj );
				return obj;
			}

			if ( _currentSize < _maxSize ) {
				_currentSize++;
				return new Entity( _eventFactory, entityNode, body, animatedSprite );
			}
			throw new InvalidOperationException( "Object pool exhausted and maximum size reached" );
		}

		/*
		===============
		Return
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		public void Return( in Entity obj ) {
			if ( _isDisposed ) {
				obj.Dispose();
				return;
			}

			ArgumentNullException.ThrowIfNull( obj );

			if ( _availableObjects.Count < _maxSize ) {
				_availableObjects.Add( obj );
			} else {
				obj.Dispose();
				_currentSize--;
			}
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Dispose() {
			if ( _isDisposed ) {
				return;
			}

			_isDisposed = true;
			while ( _availableObjects.TryTake( out Entity? obj ) ) {
				ArgumentNullException.ThrowIfNull( obj );
				obj.Dispose();
			}
			_currentSize = 0;
		}
	};
};