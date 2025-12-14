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
using NomadCore.Interfaces;
using System;

namespace NomadCore.Abstractions.Services {
	public interface IEntityComponentService : IGameService {
		public IGameEntity CreateEntity( string name, Area2D area, Sprite2D sprite );
		public IGameEntity CreateEntity( string name, CharacterBody2D body, AnimatedSprite2D animatedSprite );
		public void DestroyEntity( IGameEntity entity, bool immediate = false );
		public IGameEntity FindEntityByName( string name );

		public bool HasComponent<T>( IGameEntity entity ) where T : struct, IComponent;
		public ref T AddComponent<T>( IGameEntity entity ) where T : struct, IComponent;
		public ref T AddComponent<T>( IGameEntity entity, T defaultValue ) where T : struct, IComponent;
		public ref T GetOrAddComponent<T>( IGameEntity entity ) where T : struct, IComponent;
		public ref T GetOrAddComponent<T>( IGameEntity entity, T defaultValue ) where T : struct, IComponent;
		public ref T GetComponent<T>( IGameEntity entity ) where T : struct, IComponent;
		public bool TryGetComponent<T>( IGameEntity entity, out T component ) where T : struct, IComponent;

		public void AddComponents( IGameEntity entity, Span<IComponent> components );

		public void Initialize();
		public void Update( float deltaTime );
	};
};