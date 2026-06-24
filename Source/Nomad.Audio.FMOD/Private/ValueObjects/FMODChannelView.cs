/*
===========================================================================
The Nomad MPLv2 Source Code
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System.Numerics;
using Nomad.Audio.Fmod.Private.Entities;

namespace Nomad.Audio.Fmod.Private.ValueObjects
{
	internal readonly struct FMODChannelView
	{
		public readonly FMODChannelHandle Handle;
		public readonly string EventId;
		public readonly ushort EventNumericId;
		public readonly ushort CategoryId;
		public readonly Vector3 Position;
		public readonly float BasePriority;
		public readonly float CurrentPriority;
		public readonly float StartTimeSeconds;
		public readonly float LastStolenTime;
		public readonly float Volume;
		public readonly bool IsEssential;
		public readonly bool IsPlaying;

		public FMODChannelView(
			FMODChannelHandle handle,
			string eventId,
			ushort eventNumericId,
			ushort categoryId,
			Vector3 position,
			float basePriority,
			float currentPriority,
			float startTimeSeconds,
			float lastStolenTime,
			float volume,
			bool isEssential,
			bool isPlaying
		)
		{
			Handle = handle;
			EventId = eventId;
			EventNumericId = eventNumericId;
			CategoryId = categoryId;
			Position = position;
			BasePriority = basePriority;
			CurrentPriority = currentPriority;
			StartTimeSeconds = startTimeSeconds;
			LastStolenTime = lastStolenTime;
			Volume = volume;
			IsEssential = isEssential;
			IsPlaying = isPlaying;
		}
	}
};
