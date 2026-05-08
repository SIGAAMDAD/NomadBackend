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

namespace Nomad.Input.Private
{
	internal static class Constants
	{
		internal static class CVars
		{
			public const string NAMESPACE = "Nomad.Input";

			public const string DEFAULTS_PATH = NAMESPACE + ".DefaultsPath";
			public const string KEYBOARD_MOUSE_MAPPING = NAMESPACE + ".KeyboardMouseMapping";
			public const string GAMEPAD_MAPPING = NAMESPACE + ".GamepadMapping";

			public const string INPUT_DELAY_MS = NAMESPACE + ".InputDelayMS";

			public const string MOUSE_SENSITIVITY = NAMESPACE + ".MouseSensitivity";
			public const string MOUSE_INVERT_X = NAMESPACE + ".MouseInvertX";
			public const string MOUSE_INVERT_Y = NAMESPACE + ".MouseInvertY";

			public const string GAMEPAD_DEADZONE_LEFT = NAMESPACE + ".GamepadDeadzoneLeft";
			public const string GAMEPAD_DEADZONE_RIGHT = NAMESPACE + ".GamepadDeadzoneRight";
			public const string GAMEPAD_LOOK_SENSITIVITY = NAMESPACE + ".GamepadLookSensitivity";
			public const string GAMEPAD_MOVE_SENSITIVITY = NAMESPACE + ".GamepadMoveSensitivity";
			public const string GAMEPAD_TRIGGER_DEADZONE = NAMESPACE + ".GamepadTriggerDeadzone";
			public const string GAMEPAD_RESPONSE_CURVE = NAMESPACE + ".GamepadResponseCurve";

			public const string HAPTICS_ENABLED = NAMESPACE + ".HapticsEnabled";
			public const string HAPTIC_STRENGTH = NAMESPACE + ".HapticStrength";
		};

		public const string KEYBOARD_DEVICE_ID = "Keyboard";
		public const string MOUSE_BUTTON_DEVICE_ID = "MouseButton";
		public const string MOUSE_MOTION_DEVICE_ID = "MouseMotion";
		public const string GAMEPAD_DEVICE_ID = "Gamepad";

		public const string BINDINGS_DIRECTORY = "Assets/Config/Bindings";

		public const int MAX_ACTION_BINDINGS = 3;
	};
};
