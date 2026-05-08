/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;

namespace Nomad.Audio.Fmod.Private.Entities
{
	/*
	===================================================================================

	FMODChannelBatch

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal struct FMODChannelBatch
	{
		public float[] Volumes;
		public float[] Pitches;

		public int[] ConsecutiveStealCounts;

		/*
		===============
		FMODChannelBatch
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="maxChannels"></param>
		public FMODChannelBatch( int maxChannels )
		{
			Volumes = new float[maxChannels];
			Pitches = new float[maxChannels];
			ConsecutiveStealCounts = new int[maxChannels];
		}

		/*
		===============
		DecayStealCounts
		===============
		*/
		/// <summary>
		///
		/// </summary>
		public void DecayStealCounts()
		{
			int maxChannels = ConsecutiveStealCounts.Length;
			for ( int i = 0; i < maxChannels; i++ ) {
				int stealCount = ConsecutiveStealCounts[i];
				stealCount = Math.Min( 0, stealCount - 1 );
				if ( stealCount == 0 ) {
				}
			}
		}
	};
};
