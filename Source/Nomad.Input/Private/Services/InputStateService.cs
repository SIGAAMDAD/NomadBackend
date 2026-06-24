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
using System.Runtime.InteropServices;
using Nomad.Input.Interfaces;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Services
{
	/*
	===================================================================================
	
	InputStateService
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal unsafe sealed class InputStateService : IInputSnapshotService, IDisposable
	{
		private const int DEVICE_SLOT_COUNT = (int)InputDeviceSlot.Count;
		private const int CONTROL_COUNT = (int)InputControlId.Count;
		private const int WORDS_PER_DEVICE = 4;

		private readonly ulong* _pressedBits;
		private readonly float* _axis1D;
		private readonly Vector2* _axis2D;
		private readonly void* _memory;

		private bool _isDisposed;
		private Vector2 _mouseDelta;
		private Vector2 _mousePosition;

		public Vector2 MouseDelta {
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			get => _mouseDelta;
		}

		public Vector2 MousePosition {
			[MethodImpl( MethodImplOptions.AggressiveInlining )]
			get => _mousePosition;
		}

		/*
		===============
		InputStateService
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public InputStateService()
		{
			long totalBytes = 0;
			long pressedBytes = PadBytes( sizeof( ulong ) * DEVICE_SLOT_COUNT * WORDS_PER_DEVICE, Core.Constants.WORDSIZE );
			long axis1DBytes = PadBytes( sizeof( float ) * DEVICE_SLOT_COUNT * CONTROL_COUNT, Core.Constants.WORDSIZE );
			long axis2DBytes = PadBytes( sizeof( float ) * 2 * DEVICE_SLOT_COUNT * CONTROL_COUNT, Core.Constants.WORDSIZE );
			totalBytes = pressedBytes + axis1DBytes + axis2DBytes;

#if NET6_0_OR_GREATER
			_memory = NativeMemory.AlignedAlloc( (nuint)totalBytes, Core.Constants.WORDSIZE );
#else
			_memory = (void*)Marshal.AllocHGlobal( (int)totalBytes );
#endif
			_pressedBits = (ulong*)_memory;
			_axis1D = (float*)((byte*)_memory + pressedBytes);
			_axis2D = (Vector2*)((byte*)_memory + pressedBytes + axis1DBytes);

			new Span<byte>( _memory, (int)totalBytes ).Clear();
		}

		public void Dispose()
		{
			if ( _isDisposed ) {
				return;
			}

			if ( _memory != null ) {
#if NET6_0_OR_GREATER
				NativeMemory.AlignedFree( _memory );
#else
				Marshal.FreeHGlobal( (nint)_memory );
#endif
			}

			_isDisposed = true;
			GC.SuppressFinalize( this );
		}

		public void Clear()
		{
			new Span<byte>( _memory, (int)GetTotalBytes() ).Clear();
			_mouseDelta = Vector2.Zero;
			_mousePosition = Vector2.Zero;
		}

		/*
		===============
		GetPressedWords
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public ulong* GetPressedWords( InputDeviceSlot slot )
		{
			return _pressedBits + ((int)slot * WORDS_PER_DEVICE);
		}

		/*
		===============
		IsPressed
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="control"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public bool IsPressed( InputDeviceSlot slot, InputControlId control )
		{
			int controlIndex = (int)control;
			ulong* words = GetPressedWords( slot );
			return (words[controlIndex >> 6] & (1UL << (controlIndex & 63))) != 0UL;
		}

		/*
		===============
		SetPressed
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="control"></param>
		/// <param name="pressed"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetPressed( InputDeviceSlot slot, InputControlId control, bool pressed )
		{
			int controlIndex = (int)control;
			ulong* words = GetPressedWords( slot );
			ulong mask = 1UL << (controlIndex & 63);

			if ( pressed ) {
				words[controlIndex >> 6] |= mask;
			} else {
				words[controlIndex >> 6] &= ~mask;
			}
		}

		/*
		===============
		GetAxis1D
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="control"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public float GetAxis1D( InputDeviceSlot slot, InputControlId control )
		{
			return _axis1D[GetFlatIndex( slot, control )];
		}

		/*
		===============
		GetAxis2D
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="control"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public Vector2 GetAxis2D( InputDeviceSlot slot, InputControlId control )
		{
			return _axis2D[GetFlatIndex( slot, control )];
		}

		/*
		===============
		SetAxis1D
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="control"></param>
		/// <param name="value"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetAxis1D( InputDeviceSlot slot, InputControlId control, float value )
		{
			_axis1D[GetFlatIndex( slot, control )] = value;
		}

		/*
		===============
		SetAxis2D
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="control"></param>
		/// <param name="value"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetAxis2D( InputDeviceSlot slot, InputControlId control, Vector2 value )
		{
			_axis2D[GetFlatIndex( slot, control )] = value;
		}

		/*
		===============
		SetMousePosition
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void SetMousePosition( Vector2 value )
		{
			_mousePosition = value;
			_axis2D[GetFlatIndex( InputDeviceSlot.Mouse, InputControlId.Position )] = value;
		}

		/*
		===============
		AddMouseDelta
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		public void AddMouseDelta( Vector2 value )
		{
			_mouseDelta = value;
			_axis2D[GetFlatIndex( InputDeviceSlot.Mouse, InputControlId.Delta )] = value;
		}

		/*
		===============
		GetFlatIndex
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="slot"></param>
		/// <param name="control"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static int GetFlatIndex( InputDeviceSlot slot, InputControlId control )
		{
			return ((int)slot * CONTROL_COUNT) + (int)control;
		}

		/*
		===============
		PadBytes
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="size"></param>
		/// <param name="alignment"></param>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		private static long PadBytes( long size, long alignment )
		{
			return (size + alignment - 1) & ~(alignment - 1);
		}

		private static long GetTotalBytes()
		{
			long pressedBytes = PadBytes( sizeof( ulong ) * DEVICE_SLOT_COUNT * WORDS_PER_DEVICE, Core.Constants.WORDSIZE );
			long axis1DBytes = PadBytes( sizeof( float ) * DEVICE_SLOT_COUNT * CONTROL_COUNT, Core.Constants.WORDSIZE );
			long axis2DBytes = PadBytes( sizeof( float ) * 2 * DEVICE_SLOT_COUNT * CONTROL_COUNT, Core.Constants.WORDSIZE );
			return pressedBytes + axis1DBytes + axis2DBytes;
		}
	};
};
