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

using NomadCore.Abstractions.Services;
using NomadCore.Systems.SaveSystem.Infrastructure;
using NomadCore.Systems.SaveSystem.Interfaces;
using System;
using System.Collections.Generic;

namespace NomadCore.Systems.SaveSystem.Services {
	/*
	===================================================================================
	
	SaveManager
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	public sealed class SaveManager : ISaveService {
		private readonly List<Slot> Slots = new List<Slot>();

		/*
		===============
		SaveManager
		===============
		*/
		public SaveManager() {
		}

		/*
		===============
		Initialize
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Initialize() {
		}

		/*
		===============
		Shutdown
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Shutdown() {
		}

		/*
		===============
		GetSlot
		===============
		*/
		public ISaveSlot GetSlot( int slot ) {
			if ( slot < 0 || slot > Slots.Count ) {
				throw new ArgumentOutOfRangeException( nameof( slot ) );
			}

			ISaveSlot current = Slots[ slot ];
			return current;
		}

		/*
		===============
		Load
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		public void Load( int slot ) {
		}

		/*
		===============
		Save
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		public void Save( int slot ) {
		}

		/*
		===============
		SlotExists
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		public bool SlotExists( int slot ) {
			return GetSlot( slot ) != null;
		}
	};
};