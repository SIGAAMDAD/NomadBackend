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
using NomadCore.Interfaces;
using System;

namespace NomadCore.Abstractions.Services {
	/*
	===================================================================================
	
	IGameEventBusService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public interface IGameEventBusService : IGameService {
		public void Subscribe( object? subscriber, IGameEvent? eventHandler, IGameEvent.EventCallback? callback );
		public void Subscribe<TArgs>( object? subscriber, IGameEvent<TArgs>? eventHandler, IGameEvent<TArgs>.GenericEventCallback? callback ) where TArgs : IEventArgs;

		public void Unsubscribe( object? subscriber, IGameEvent? eventHandler, IGameEvent.EventCallback? callback );
		public void Unsubscribe<TArgs>( object? subscriber, IGameEvent<TArgs>? eventHandler, IGameEvent<TArgs>.GenericEventCallback? callback ) where TArgs : IEventArgs;

		public void Publish( IGameEvent? eventHandler, in IEventArgs args );
		public void Publish<TArgs>( IGameEvent<TArgs>? eventHandler, in TArgs args ) where TArgs : IEventArgs;

		public void ConnectSignal( GodotObject? source, StringName? signalName, GodotObject? target, Action? method );
		public void ConnectSignal( GodotObject? source, StringName? signalName, GodotObject? target, Callable? method );

		public void CleanupSubscriber( object? subscriber );

		public IGameEvent CreateEvent( string? name );
		public IGameEvent<TArgs> CreateEvent<TArgs>( string? name ) where TArgs : IEventArgs;
	};
};