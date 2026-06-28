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

using Nomad.Core.Input;
using System.Text;

namespace Nomad.Input.ValueObjects
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class InputBindingDefinition
    {
        private const string UNBOUND = "Unbound";

        public InputScheme Scheme { get; set; }
        public InputBindingKind Kind { get; set; }
        public int Priority { get; set; }
        public bool ConsumesInput { get; set; }
        public uint ContextMask { get; set; } = uint.MaxValue;

        public ButtonBinding Button = default;
        public Axis1DBinding Axis1D = default;
        public Axis1DCompositeBinding Axis1DComposite = default;
        public Axis2DBinding Axis2D = default;
        public Axis2DCompositeBinding Axis2DComposite = default;
        public Delta2DBinding Delta2D = default;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Kind switch
            {
                InputBindingKind.Button => FormatButton(Button),
                InputBindingKind.Axis1D => FormatAxis1D(Axis1D),
                InputBindingKind.Axis1DComposite => FormatAxis1DComposite(Axis1DComposite),
                InputBindingKind.Axis2D => FormatAxis2D(Axis2D),
                InputBindingKind.Axis2DComposite => FormatAxis2DComposite(Axis2DComposite),
                InputBindingKind.Delta2D => FormatDelta2D(Delta2D),
                _ => UNBOUND
            };
        }

        private static string FormatButton(in ButtonBinding binding)
        {
            if (binding.ControlId == InputControlId.None)
            {
                return UNBOUND;
            }

            string control = FormatControl(binding.ControlId);
            if (binding.Modifiers.IsDefaultOrEmpty)
            {
                return PrefixWithDevice(binding.DeviceId, control);
            }

            string[] parts = new string[binding.Modifiers.Length + 1];
            for (int i = 0; i < binding.Modifiers.Length; i++)
            {
                parts[i] = FormatControl(binding.Modifiers[i]);
            }

            parts[parts.Length - 1] = control;
            return PrefixWithDevice(binding.DeviceId, string.Join(" + ", parts));
        }

        private static string FormatAxis1D(in Axis1DBinding binding)
        {
            if (binding.ControlId == InputControlId.None)
            {
                return UNBOUND;
            }
            return PrefixWithDevice(binding.DeviceId, FormatControl(binding.ControlId));
        }

        private static string FormatAxis1DComposite(in Axis1DCompositeBinding binding)
        {
            if (binding.Negative == InputControlId.None && binding.Positive == InputControlId.None)
            {
                return UNBOUND;
            }
            return $"Negative: {FormatControl(binding.Negative)}, Positive: {FormatControl(binding.Positive)}";
        }

        private static string FormatAxis2D(in Axis2DBinding binding)
        {
            if (binding.ControlId == InputControlId.None)
            {
                return UNBOUND;
            }
            return PrefixWithDevice(binding.DeviceId, FormatControl(binding.ControlId));
        }

        private static string FormatAxis2DComposite(in Axis2DCompositeBinding binding)
        {
            if (binding.Up == InputControlId.None && binding.Down == InputControlId.None && binding.Left == InputControlId.None && binding.Right == InputControlId.None)
            {
                return UNBOUND;
            }
            return $"Up: {FormatControl(binding.Up)}, Down: {FormatControl(binding.Down)}, Left: {FormatControl(binding.Left)}, Right: {FormatControl(binding.Right)}";
        }

        private static string FormatDelta2D(in Delta2DBinding binding)
        {
            if (binding.ControlId == InputControlId.None)
            {
                return UNBOUND;
            }
            return PrefixWithDevice(binding.DeviceId, FormatControl(binding.ControlId));
        }

        private static string PrefixWithDevice(InputDeviceSlot device, string control)
        {
            return $"{FormatDevice(device)}: {control}";
        }

        private static string FormatDevice(InputDeviceSlot device)
        {
            return device switch
            {
                InputDeviceSlot.Keyboard => "Keyboard",
                InputDeviceSlot.Mouse => "Mouse",
                InputDeviceSlot.Gamepad0 => "Gamepad 1",
                InputDeviceSlot.Gamepad1 => "Gamepad 2",
                InputDeviceSlot.Gamepad2 => "Gamepad 3",
                InputDeviceSlot.Gamepad3 => "Gamepad 4",
                _ => SplitPascalCase(device.ToString())
            };
        }

        private static string FormatControl(InputControlId control)
        {
            return control switch
            {
                InputControlId.None => UNBOUND,
                InputControlId.Num0 => "0",
                InputControlId.Num1 => "1",
                InputControlId.Num2 => "2",
                InputControlId.Num3 => "3",
                InputControlId.Num4 => "4",
                InputControlId.Num5 => "5",
                InputControlId.Num6 => "6",
                InputControlId.Num7 => "7",
                InputControlId.Num8 => "8",
                InputControlId.Num9 => "9",
                InputControlId.Period => ".",
                InputControlId.SemiColon => ";",
                InputControlId.Colon => ":",
                InputControlId.Grave => "`",
                InputControlId.BackSpace => "Backspace",
                InputControlId.Ctrl => "Ctrl",
                InputControlId.Left => "Left Button",
                InputControlId.Right => "Right Button",
                InputControlId.Middle => "Middle Button",
                InputControlId.WheelUp => "Wheel Up",
                InputControlId.WheelDown => "Wheel Down",
                InputControlId.X1 => "Button 4",
                InputControlId.X2 => "Button 5",
                InputControlId.DPadUp => "D-Pad Up",
                InputControlId.DPadDown => "D-Pad Down",
                InputControlId.DPadLeft => "D-Pad Left",
                InputControlId.DPadRight => "D-Pad Right",
                InputControlId.LeftShoulder => "Left Shoulder",
                InputControlId.RightShoulder => "Right Shoulder",
                InputControlId.LeftStickButton => "Left Stick Click",
                InputControlId.RightStickButton => "Right Stick Click",
                InputControlId.GamepadA => "A",
                InputControlId.GamepadB => "B",
                InputControlId.GamepadX => "X",
                InputControlId.GamepadY => "Y",
                InputControlId.LeftTrigger => "Left Trigger",
                InputControlId.RightTrigger => "Right Trigger",
                InputControlId.LeftStick => "Left Stick",
                InputControlId.RightStick => "Right Stick",
                _ => SplitPascalCase(control.ToString())
            };
        }

        private static string SplitPascalCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length + 8);
            builder.Append(value[0]);

            for (int i = 1; i < value.Length; i++)
            {
                char current = value[i];
                char previous = value[i - 1];

                if ((char.IsUpper(current) && !char.IsUpper(previous))
                    || (char.IsDigit(current) && !char.IsDigit(previous)))
                {
                    builder.Append(' ');
                }
                builder.Append(current);
            }
            return builder.ToString();
        }
    }
}
