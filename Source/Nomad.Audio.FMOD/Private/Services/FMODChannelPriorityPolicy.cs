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
using System.Numerics;
using System.Runtime.CompilerServices;
using Nomad.Audio.Interfaces;

namespace Nomad.Audio.Fmod.Private.Services
{
	/// <summary>
	/// Centralizes priority, attenuation, and steal scoring policy for active FMOD channels.
	/// The channel service decides when policy is applied; this type decides the math.
	/// </summary>
	internal unsafe sealed class FMODChannelPriorityPolicy
	{
		private readonly IListenerService _listenerService;
		private readonly FMODPriorityCalculator _priorityCalculator;
		private readonly FMODChannelRegistry _registry;
		private readonly FMODChannelStorage _storage;

		private float _distanceWeight;
		private float _volumeWeight;

		public FMODChannelPriorityPolicy(
			IListenerService listenerService,
			FMODPriorityCalculator priorityCalculator,
			FMODChannelRegistry registry,
			FMODChannelStorage storage )
		{
			_listenerService = listenerService;
			_priorityCalculator = priorityCalculator;
			_registry = registry;
			_storage = storage;
		}

		public float DistanceWeight {
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			get => _distanceWeight;
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			set => _distanceWeight = value;
		}

		public float VolumeWeight {
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			get => _volumeWeight;
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			set => _volumeWeight = value;
		}

		public float CalculateIncomingPriority(
			float now,
			ushort eventId,
			ushort categoryId,
			Vector3 position,
			float basePriority )
		{
			Vector3 listener = _listenerService.ActiveListener;
			float distance = Distance( position.X, position.Y, position.Z, listener.X, listener.Y, listener.Z );
			float distanceFactor = _priorityCalculator.CalculateDistanceFactor( distance );

			float priority = basePriority * _registry.GetPriorityScale( categoryId ) * distanceFactor;

			float timeSinceLastPlay = now - _registry.GetLastPlayTime( eventId );
			if ( timeSinceLastPlay < 0.050f ) {
				priority *= 0.70f;
			} else if ( timeSinceLastPlay < 0.100f ) {
				priority *= 0.85f;
			}

			ushort steals = _registry.GetConsecutiveStealCount( eventId );
			if ( steals > 0 ) {
				priority *= 1.0f + (steals * 0.05f);
			}

			return priority;
		}

		public void UpdatePrioritiesAndVolumes( int denseCount )
		{
			Vector3 listener = _listenerService.ActiveListener;
			float lx = listener.X;
			float ly = listener.Y;
			float lz = listener.Z;

			for ( int dense = 0; dense < denseCount; dense++ ) {
				nint ptr = _storage.InstancePtr[dense];
				if ( !FMODChannelInstanceController.IsPlaying( ptr ) ) {
					continue;
				}

				float distance = Distance( _storage.PosX[dense], _storage.PosY[dense], _storage.PosZ[dense], lx, ly, lz );
				float distanceFactor = _priorityCalculator.CalculateDistanceFactor( distance );

				_storage.Attenuation[dense] = distanceFactor;
				_storage.CurrentPriority[dense] =
					_storage.BasePriority[dense] *
					_registry.GetPriorityScale( _storage.CategoryId[dense] ) *
					distanceFactor;

				FMODChannelInstanceController.SetVolume( ptr, CalculateInstanceVolume( dense ) );
			}
		}

		public float CalculateStealScore( int dense, float now, float incomingPriority, ushort incomingCategoryId )
		{
			float age = now - _storage.StartTime[dense];
			float ageFactor = age >= 5.0f ? 1.0f : age * 0.2f;
			float distanceFactor = 1.0f - _storage.Attenuation[dense];
			float volumeFactor = 1.0f - _storage.Volume[dense];

			float score =
				(incomingPriority - _storage.CurrentPriority[dense]) * 2.0f +
				ageFactor * 0.5f +
				distanceFactor * _distanceWeight +
				volumeFactor * _volumeWeight;

			if ( _storage.CategoryId[dense] == incomingCategoryId ) {
				score *= 0.5f;
			}

			float timeSinceLastStolen = now - _storage.LastStolenTime[dense];
			if ( timeSinceLastStolen < 1.0f ) {
				score *= timeSinceLastStolen;
			}

			return score;
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float CalculateInstanceVolume( int dense )
		{
			float volume = _storage.UserVolume[dense] * _storage.Attenuation[dense];
			_storage.Volume[dense] = Math.Clamp( volume, 0.0f, 1.0f );
			return _storage.Volume[dense];
		}

		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static float Distance( float ax, float ay, float az, float bx, float by, float bz )
		{
			float dx = ax - bx;
			float dy = ay - by;
			float dz = az - bz;
			return MathF.Sqrt( dx * dx + dy * dy + dz * dz );
		}
	}
}
