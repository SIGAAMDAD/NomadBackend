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
using NomadCore.Systems.SaveSystem.Domain.Events;
using NomadCore.Systems.SaveSystem.Domain.Models.Aggregates;
using NomadCore.Systems.SaveSystem.Domain.Models.ValueObjects;
using NomadCore.Systems.SaveSystem.Errors;
using NomadCore.Systems.SaveSystem.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace NomadCore.Systems.SaveSystem.Services {
	/*
	===================================================================================
	
	SlotService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public sealed class SlotService {
		public int CurrentSlot => _activeSlot;
		private int _activeSlot = 0;

		public int SlotCount => _slotRepository.SlotCount;

		private readonly ISlotRepository _slotRepository;

		public static readonly IGameEvent<SaveStartedEventData> SaveStarted;
		public static readonly IGameEvent<SaveCompletedEventData> SaveCompleted;
		public static readonly IGameEvent<LoadStartedEventData> LoadStarted;

		public SlotService() {
			_slotRepository = new SlotRepository();
		}

		/*
		===============
		GetSlot
		===============
		*/
		public async ValueTask<ISaveSlot> GetSlot( SaveFileId slot, CancellationToken ct = default ) {
			ct.ThrowIfCancellationRequested();
			var entity = await _slotRepository.GetByIdAsync( slot, ct ) ?? throw new SaveLoadException( slot, new System.Exception( $"Failed to load save slot {slot}" ) );
			return entity;
		}

		/*
		===============
		SaveSlot
		===============
		*/
		public async ValueTask SaveSlot( SaveFileId slot, CancellationToken ct = default ) {
			ct.ThrowIfCancellationRequested();

			var entity = await _slotRepository.GetByIdAsync( slot, ct );
			if ( entity == null || entity is not SaveFile slotData ) {
				return;
			}
			slotData.Save();
		}
	};
};