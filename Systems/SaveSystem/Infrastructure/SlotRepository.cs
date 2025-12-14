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

using NomadCore.Systems.SaveSystem.Domain.Models.Aggregates;
using NomadCore.Systems.SaveSystem.Domain.Models.ValueObjects;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NomadCore.Systems.SaveSystem.Infrastructure {
	/*
	===================================================================================
	
	SlotRepository
	
	Description
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class SlotRepository : ISlotRepository, IDisposable {
		public int SlotCount => _slots.Count;

		private readonly List<ISaveSlot> _slots = new List<ISaveSlot>();
		private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

		/*
		===============
		GetByIdAsync
		===============
		*/
		public async ValueTask<ISaveSlot?> GetByIdAsync( SaveFileId id, CancellationToken ct = default ) {
			ISaveSlot? slot = null;

			_lock.EnterUpgradeableReadLock();
			try {
				ArgumentOutOfRangeException.ThrowIfNegative( id.Value );

				if ( id.Value >= _slots.Count ) {
					await LoadSlotAsync( id );
				}
				slot = _slots[ id.Value ];
			}
			finally {
				_lock.ExitUpgradeableReadLock();
			}
			return slot;
		}

		/*
		===============
		LoadSlotAsync
		===============
		*/
		private async Task LoadSlotAsync( SaveFileId slot ) {
			ArgumentOutOfRangeException.ThrowIfNegative( slot.Value );

			
//			_slots.Add( new SaveFile( slot ) );
		}

		/*
		===============
		LoadSlot
		===============
		*/
		private void LoadSlot( SaveFileId slot ) {
			
		}

		public void Dispose() {
			throw new NotImplementedException();
		}
	}
};