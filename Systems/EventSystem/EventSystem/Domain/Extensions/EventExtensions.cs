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
using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace NomadCore.Systems.EventSystem.Domain.Extensions {
	/*
	===================================================================================
	
	EventExtensions
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public static class EventExtensions {
		/*
		===============
		Subscribe
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static IDisposable Subscribe<TArgs>( this IGameEvent<TArgs> gameEvent, object subscriber, IGameEvent<TArgs>.EventCallback callback )
			where TArgs : struct
		{
			void Unsubscribe() {
				gameEvent.Unsubscribe( subscriber, callback );
			}
			gameEvent.Subscribe( subscriber, callback );
			return new EventUnsubscriber( Unsubscribe );
		}

		/*
		===============
		SubscribeOnce
		===============
		*/
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public static void SubscribeOnce<TArgs>( this IGameEvent<TArgs> gameEvent, object subscriber, IGameEvent<TArgs>.EventCallback callback )
			where TArgs : struct
		{
			void Wrapper( in TArgs args ) {
				callback( args );
				gameEvent.Unsubscribe( subscriber, Wrapper );
			}
			gameEvent.Subscribe( subscriber, Wrapper );
		}

		// TODO: perhaps pool this
		private record EventUnsubscriber( Action unsubscribe ) : IDisposable { public void Dispose() => unsubscribe?.Invoke(); };
		private record EventUnsubscriberAsync( Action unsubscribe ) : IAsyncDisposable {
			public ValueTask DisposeAsync() {
				return new ValueTask();
			}
		};
	};
};