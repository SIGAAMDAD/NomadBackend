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
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.EventSystem.Domain {
	/*
	===================================================================================
	
	DisposableSubscription
	
	===================================================================================
	*/
	/// <summary>
	/// An automated disposable subscription.
	/// </summary>
	
	public class DisposableSubscription<TArgs>
		where TArgs : struct
	{
		private IGameEvent<TArgs> _event;
		private IGameEvent<TArgs>.EventCallback _callback;

		/*
		===============
		DisposableSubscription
		===============
		*/
		public DisposableSubscription( IGameEvent<TArgs> handler, IGameEvent<TArgs>.EventCallback callback ) {
			handler.Subscribe( this, callback );
			_event = handler;
			_callback = callback;
		}

		~DisposableSubscription() {
			_event.Unsubscribe( this, _callback );
			_event = null;
			_callback = null;
		}

		/*
		===============
		Publish
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void Publish( in TArgs eventArgs ) {
			_event.Publish( in eventArgs );
		}
	};
};