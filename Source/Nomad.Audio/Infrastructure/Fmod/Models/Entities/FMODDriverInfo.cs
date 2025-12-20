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
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using System;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Models.Entities {
	/*
	===================================================================================
	
	FMODDriverInfo
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal record FMODDriverInfo : IValueObject<FMODDriverInfo> {
		public FMODDriverId Id => _driverGuid;
		private readonly FMODDriverId _driverGuid;

		public readonly string Name;
		public readonly FMOD.SPEAKERMODE SpeakerMode;
		public readonly int ChannelCount;

		public DateTime CreatedAt => _createdAt;
		private readonly DateTime _createdAt = DateTime.UtcNow;

		public DateTime? ModifiedAt => throw new NotImplementedException();
		public int Version => throw new NotImplementedException();
		
		public FMODDriverInfo( FMODDriverId id, string name, FMOD.SPEAKERMODE speakerMode, int channelCount ) {
			_driverGuid = id;
			Name = name;
			SpeakerMode = speakerMode;
			ChannelCount = channelCount;
		}
	};
};