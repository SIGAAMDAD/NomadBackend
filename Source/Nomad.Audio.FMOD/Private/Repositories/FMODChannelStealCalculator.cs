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
using System.Collections.Generic;
using System.Numerics;
using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Fmod.ValueObjects;
using Nomad.Audio.Interfaces;
using Nomad.Core;
using Nomad.Core.CVars;
using Nomad.Core.Events;
using Nomad.Core.Util;
using Nomad.CVars;

namespace Nomad.Audio.Fmod.Private.Services
{
	/*
	===================================================================================
	
	FMODChannelStealCalculator
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class FMODChannelStealCalculator : IDisposable
	{
		private float _distanceWeight;
		private float _volumeWeight = 0.2f;

		private readonly FMODPriorityCalculator _priorityCalculator;

		private readonly ISubscriptionHandle _distanceWeightEvent;
		private readonly ISubscriptionHandle _volumeWeightEvent;

		private bool _isDisposed = false;

		/*
		===============
		FMODChannelStealCalculator
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="cvarSystem"></param>
		/// <param name="priorityCalculator"></param>
		/// <exception cref="ArgumentNullException"></exception>
		public FMODChannelStealCalculator( ICVarSystemService cvarSystem, FMODPriorityCalculator priorityCalculator )
		{
			_priorityCalculator = priorityCalculator ?? throw new ArgumentNullException( nameof( priorityCalculator ) );

			var distanceWeight = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.DISTANCE_WEIGHT );
			_distanceWeight = distanceWeight.Value;
			_distanceWeightEvent = distanceWeight.ValueChanged.Subscribe( OnDistanceWeightValueChanged );

			var volumeWeight = cvarSystem.GetCVarOrThrow<float>( Constants.CVars.EngineUtils.Audio.VOLUME_WEIGHT );
			_volumeWeight = volumeWeight.Value;
			_volumeWeightEvent = volumeWeight.ValueChanged.Subscribe( OnVolumeWeightValueChanged );
		}

		/*
		===============
		Dispose
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public void Dispose()
		{
			if ( !_isDisposed ) {
				_distanceWeightEvent?.Dispose();
			}
			GC.SuppressFinalize( this );
			_isDisposed = true;
		}

		/*
		===============
		CalculateStealScore
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="currentTime"></param>
		/// <param name="listenerPos"></param>
		/// <param name="candidate"></param>
		/// <param name="newPriority"></param>
		/// <param name="category"></param>
		/// <returns></returns>
		public float CalculateStealScore( float currentTime, in Vector2 listenerPos, FMODChannel candidate, float newPriority, SoundCategory category )
		{
			Vector2 pos = candidate.Instance.Position;
			float dx = pos.X - listenerPos.X;
			float dy = pos.Y - listenerPos.Y;
			float distance = MathF.Sqrt( dx * dx + dy * dy );

			float priorityDiff = newPriority - candidate.CurrentPriority;
			float age = currentTime - candidate.StartTimeSeconds;
			float ageFactor = age >= 5.0f ? 1.0f : age * 0.2f;
			float distanceFactor = 1.0f - _priorityCalculator.CalculateDistanceFactor( distance );
			float volumeFactor = 1.0f - candidate.Volume;

			float score =
				priorityDiff * 2.0f +
				ageFactor * 0.5f +
				distanceFactor * _distanceWeight +
				volumeFactor * _volumeWeight;

			if ( ReferenceEquals( candidate.Category, category ) ) {
				score *= 0.5f;
			}

			float timeSinceLastStolen = currentTime - candidate.LastStolenTime;
			if ( timeSinceLastStolen < 1.0f ) {
				score *= timeSinceLastStolen;
			}

			return score;
		}

		/*
		===============
		OnDistanceWeightValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnDistanceWeightValueChanged( in CVarValueChangedEventArgs<float> args )
		{
			_distanceWeight = args.NewValue;
		}

		/*
		===============
		OnVolumeWeightValueChanged
		===============
		*/
		/// <summary>
		///
		/// </summary>
		/// <param name="args"></param>
		private void OnVolumeWeightValueChanged( in CVarValueChangedEventArgs<float> args )
		{
			_volumeWeight = args.NewValue;
		}
	};
};
