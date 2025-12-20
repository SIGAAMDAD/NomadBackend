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

using NomadCore.Interfaces.Common;
using NomadCore.Systems.Audio.Domain.Interfaces;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using System;

namespace NomadCore.Systems.Audio.Domain.Models.Aggregates {
	/*
	===================================================================================
	
	BankComposite
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class BankComposite( IEventCollection events, IBankResource resource, IBankMetadata metadata ) : IAudioBank {
		public BankId Id => metadata.Id;
		public DateTime CreatedAt => metadata.CreatedAt;
		public DateTime? ModifiedAt => metadata.ModifiedAt;
		public int Version => metadata.Version;

		public BankState Status {
			get => _status;
			set {
				if ( value < BankState.Unloaded || value >= BankState.Count ) {
					return;
				}
				_status = value;
			}
		}
		private BankState _status = BankState.Unloaded;

		/*
		===============
		Dispose
		===============
		*/
		public void Dispose() {
			resource.Dispose();
		}

		/*
		===============
		IsEventInBank
		===============
		*/
		public bool IsEventInBank( EventId id ) {
			return events.ContainsEvent( id );
		}

		/*
		===============
		Equals
		===============
		*/
		public bool Equals( IEntity<BankId>? other ) {
			return other?.Id == Id;
		}
	};
};