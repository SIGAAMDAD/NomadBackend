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
using Nomad.Input.Interfaces;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.Services
{
	internal sealed class InputDeviceSlotService : IInputDeviceSlotService
	{
		private readonly int[] _localSlotByDevice = new int[(int)InputDeviceSlot.Count];
		private readonly List<InputDeviceSlot>[] _devicesByLocalSlot = new List<InputDeviceSlot>[IInputDeviceSlotService.MaxLocalSlots];

		public event Action<InputDeviceSlot, int>? DeviceSlotChanged;

		public InputDeviceSlotService()
		{
			for ( int i = 0; i < _localSlotByDevice.Length; i++ ) {
				_localSlotByDevice[i] = -1;
			}

			for ( int i = 0; i < _devicesByLocalSlot.Length; i++ ) {
				_devicesByLocalSlot[i] = new List<InputDeviceSlot>( 2 );
			}

			AssignDefaultDevices();
		}

		public void AssignDevice( InputDeviceSlot deviceSlot, int localSlot )
		{
			ThrowIfInvalidDevice( deviceSlot );
			ThrowIfInvalidLocalSlot( localSlot );

			int deviceIndex = (int)deviceSlot;
			int previousLocalSlot = _localSlotByDevice[deviceIndex];
			if ( previousLocalSlot == localSlot ) {
				return;
			}

			if ( previousLocalSlot >= 0 ) {
				_devicesByLocalSlot[previousLocalSlot].Remove( deviceSlot );
			}

			_localSlotByDevice[deviceIndex] = localSlot;
			_devicesByLocalSlot[localSlot].Add( deviceSlot );
			DeviceSlotChanged?.Invoke( deviceSlot, localSlot );
		}

		public void AssignDeviceExclusive( InputDeviceSlot deviceSlot, int localSlot )
		{
			ThrowIfInvalidDevice( deviceSlot );
			ThrowIfInvalidLocalSlot( localSlot );

			var devices = _devicesByLocalSlot[localSlot].ToArray();
			for ( int i = 0; i < devices.Length; i++ ) {
				if ( devices[i] != deviceSlot ) {
					UnassignDevice( devices[i] );
				}
			}

			AssignDevice( deviceSlot, localSlot );
		}

		public bool UnassignDevice( InputDeviceSlot deviceSlot )
		{
			ThrowIfInvalidDevice( deviceSlot );

			int deviceIndex = (int)deviceSlot;
			int previousLocalSlot = _localSlotByDevice[deviceIndex];
			if ( previousLocalSlot < 0 ) {
				return false;
			}

			_localSlotByDevice[deviceIndex] = -1;
			_devicesByLocalSlot[previousLocalSlot].Remove( deviceSlot );
			DeviceSlotChanged?.Invoke( deviceSlot, -1 );
			return true;
		}

		public void SwapLocalSlots( int firstLocalSlot, int secondLocalSlot )
		{
			ThrowIfInvalidLocalSlot( firstLocalSlot );
			ThrowIfInvalidLocalSlot( secondLocalSlot );

			if ( firstLocalSlot == secondLocalSlot ) {
				return;
			}

			var firstDevices = _devicesByLocalSlot[firstLocalSlot].ToArray();
			var secondDevices = _devicesByLocalSlot[secondLocalSlot].ToArray();

			for ( int i = 0; i < firstDevices.Length; i++ ) {
				SetAssignmentNoEvent( firstDevices[i], secondLocalSlot );
			}
			for ( int i = 0; i < secondDevices.Length; i++ ) {
				SetAssignmentNoEvent( secondDevices[i], firstLocalSlot );
			}

			for ( int i = 0; i < firstDevices.Length; i++ ) {
				DeviceSlotChanged?.Invoke( firstDevices[i], secondLocalSlot );
			}
			for ( int i = 0; i < secondDevices.Length; i++ ) {
				DeviceSlotChanged?.Invoke( secondDevices[i], firstLocalSlot );
			}
		}

		public void ClearAssignments()
		{
			for ( int i = 0; i < _localSlotByDevice.Length; i++ ) {
				_localSlotByDevice[i] = -1;
			}

			for ( int i = 0; i < _devicesByLocalSlot.Length; i++ ) {
				_devicesByLocalSlot[i].Clear();
			}

			for ( int i = 0; i < (int)InputDeviceSlot.Count; i++ ) {
				DeviceSlotChanged?.Invoke( (InputDeviceSlot)i, -1 );
			}
		}

		public void ResetToDefaultAssignments()
		{
			ClearAssignments();
			AssignDefaultDevices();
		}

		public bool TryGetLocalSlot( InputDeviceSlot deviceSlot, out int localSlot )
		{
			ThrowIfInvalidDevice( deviceSlot );

			localSlot = _localSlotByDevice[(int)deviceSlot];
			return localSlot >= 0;
		}

		public bool IsAssignedToLocalSlot( InputDeviceSlot deviceSlot, int localSlot )
		{
			ThrowIfInvalidDevice( deviceSlot );
			ThrowIfInvalidLocalSlot( localSlot );

			return _localSlotByDevice[(int)deviceSlot] == localSlot;
		}

		public IReadOnlyList<InputDeviceSlot> GetDevicesForLocalSlot( int localSlot )
		{
			ThrowIfInvalidLocalSlot( localSlot );
			return _devicesByLocalSlot[localSlot];
		}

		private void AssignDefaultDevices()
		{
			AssignDevice( InputDeviceSlot.Keyboard, 0 );
			AssignDevice( InputDeviceSlot.Mouse, 0 );

			for ( int i = 0; i < IInputDeviceSlotService.MaxLocalSlots; i++ ) {
				AssignDevice( (InputDeviceSlot)((int)InputDeviceSlot.Gamepad0 + i), i );
			}
		}

		private void SetAssignmentNoEvent( InputDeviceSlot deviceSlot, int localSlot )
		{
			int deviceIndex = (int)deviceSlot;
			int previousLocalSlot = _localSlotByDevice[deviceIndex];
			if ( previousLocalSlot == localSlot ) {
				return;
			}

			if ( previousLocalSlot >= 0 ) {
				_devicesByLocalSlot[previousLocalSlot].Remove( deviceSlot );
			}

			_localSlotByDevice[deviceIndex] = localSlot;
			_devicesByLocalSlot[localSlot].Add( deviceSlot );
		}

		private static void ThrowIfInvalidDevice( InputDeviceSlot deviceSlot )
		{
			if ( deviceSlot >= InputDeviceSlot.Count ) {
				throw new ArgumentOutOfRangeException( nameof( deviceSlot ) );
			}
		}

		private static void ThrowIfInvalidLocalSlot( int localSlot )
		{
			if ( localSlot < 0 || localSlot >= IInputDeviceSlotService.MaxLocalSlots ) {
				throw new ArgumentOutOfRangeException( nameof( localSlot ) );
			}
		}
	}
}
