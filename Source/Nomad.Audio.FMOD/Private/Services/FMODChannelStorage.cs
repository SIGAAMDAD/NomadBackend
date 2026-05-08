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
using System.Runtime.InteropServices;

namespace Nomad.Audio.Fmod.Private.Services
{
	/*
	===================================================================================

	FMODChannelStorage

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal unsafe sealed class FMODChannelStorage : IDisposable
	{
		private byte* _base;
		private readonly nuint _bytes;

		public readonly int Capacity;

		public readonly int* SlotToDense;
		public readonly int* DenseToSlot;
		public readonly int* FreeSlots;
		public readonly uint* Generation;

		public readonly nint* InstancePtr;
		public readonly float* PosX;
		public readonly float* PosY;
		public readonly float* BasePriority;
		public readonly float* CurrentPriority;
		public readonly float* StartTime;
		public readonly float* LastStolenTime;
		public readonly float* Volume;
		public readonly float* UserVolume;
		public readonly float* Attenuation;
		public readonly float* Pitch;

		public readonly ushort* EventId;
		public readonly ushort* CategoryId;
		public readonly byte* Flags;

		public readonly int* SlotNextInCategory;
		public readonly int* SlotPrevInCategory;

		public FMODChannelStorage( int capacity )
		{
			Capacity = capacity;

			long totalBytes = 0;
			long alignment = Core.Constants.WORDSIZE;
			totalBytes += PadBytes( sizeof( int ) * capacity, alignment ); // SlotToDense
			totalBytes += PadBytes( sizeof( int ) * capacity, alignment ); // DenseToSlot
			totalBytes += PadBytes( sizeof( int ) * capacity, alignment ); // FreeSlots
			totalBytes += PadBytes( sizeof( uint ) * capacity, alignment ); // Generation
			totalBytes += PadBytes( sizeof( nint ) * capacity, alignment ); // InstancePtr
			totalBytes += PadBytes( sizeof( float ) * capacity, alignment ); // PosX
			totalBytes += PadBytes( sizeof( float ) * capacity, alignment ); // PosY
			totalBytes += PadBytes( sizeof( float ) * capacity, alignment ); // BasePriority
			totalBytes += PadBytes( sizeof( float ) * capacity, alignment ); // CurrentPriority
			totalBytes += PadBytes( sizeof( float ) * capacity, alignment ); // StartTime
			totalBytes += PadBytes( sizeof( float ) * capacity, alignment ); // LastStolenTime
			totalBytes += PadBytes( sizeof( float ) * capacity, alignment ); // Volume
			totalBytes += PadBytes( sizeof( float ) * capacity, alignment ); // UserVolume
			totalBytes += PadBytes( sizeof( float ) * capacity, alignment ); // Attenuation
			totalBytes += PadBytes( sizeof( float ) * capacity, alignment ); // Pitch
			totalBytes += PadBytes( sizeof( ushort ) * capacity, alignment ); // EventId
			totalBytes += PadBytes( sizeof( ushort ) * capacity, alignment ); // Category
			totalBytes += PadBytes( sizeof( byte ) * capacity, alignment ); // Flags
			totalBytes += PadBytes( sizeof( int ) * capacity, alignment ); // SlotNextInCategory
			totalBytes += PadBytes( sizeof( int ) * capacity, alignment ); // SlotPrevInCategory

			_bytes = (nuint)totalBytes;
#if NET6_0_OR_GREATER
			_base = (byte*)NativeMemory.AlignedAlloc( _bytes, Core.Constants.WORDSIZE );
#else
			_base = (byte*)Marshal.AllocHGlobal( (int)_bytes );
#endif
			SlotToDense = (int*)_base;
			DenseToSlot = (int*)((byte*)SlotToDense + PadBytes( sizeof( int ) * capacity, alignment ));
			FreeSlots = (int*)((byte*)DenseToSlot + PadBytes( sizeof( int ) * capacity, alignment ));
			Generation = (uint*)((byte*)FreeSlots + PadBytes( sizeof( int ) * capacity, alignment ));
			InstancePtr = (nint*)((byte*)Generation + PadBytes( sizeof( uint ) * capacity, alignment ));
			PosX = (float*)((byte*)InstancePtr + PadBytes( sizeof( nint ) * capacity, alignment ));
			PosY = (float*)((byte*)PosX + PadBytes( sizeof( float ) * capacity, alignment ));
			BasePriority = (float*)((byte*)PosY + PadBytes( sizeof( float ) * capacity, alignment ));
			CurrentPriority = (float*)((byte*)BasePriority + PadBytes( sizeof( float ) * capacity, alignment ));
			StartTime = (float*)((byte*)CurrentPriority + PadBytes( sizeof( float ) * capacity, alignment ));
			LastStolenTime = (float*)((byte*)StartTime + PadBytes( sizeof( float ) * capacity, alignment ));
			Volume = (float*)((byte*)LastStolenTime + PadBytes( sizeof( float ) * capacity, alignment ));
			UserVolume = (float*)((byte*)Volume + PadBytes( sizeof( float ) * capacity, alignment ));
			Attenuation = (float*)((byte*)UserVolume + PadBytes( sizeof( float ) * capacity, alignment ));
			Pitch = (float*)((byte*)Attenuation + PadBytes( sizeof( float ) * capacity, alignment ));
			EventId = (ushort*)((byte*)Pitch + PadBytes( sizeof( float ) * capacity, alignment ));
			CategoryId = (ushort*)((byte*)EventId + PadBytes( sizeof( ushort ) * capacity, alignment ));
			Flags = (byte*)((byte*)CategoryId + PadBytes( sizeof( ushort ) * capacity, alignment ));
			SlotNextInCategory = (int*)((byte*)Flags + PadBytes( sizeof( byte ) * capacity, alignment ));
			SlotPrevInCategory = (int*)((byte*)SlotNextInCategory + PadBytes( sizeof( int ) * capacity, alignment ));

			new Span<byte>( _base, (int)totalBytes ).Clear();
		}

		public void Dispose()
		{
			if ( _base != null ) {
#if NET6_0_OR_GREATER
				NativeMemory.Free( _base );
#else
				Marshal.FreeHGlobal( (nint)_base );
#endif
				_base = null;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="size"></param>
		/// <param name="alignment"></param>
		/// <returns></returns>
		private static long PadBytes( long size, long alignment )
		{
			return (size + alignment - 1) & ~(alignment - 1);
		}
	};
};
