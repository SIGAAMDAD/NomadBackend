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

using System.Collections.Immutable;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Extensions
{
    /// <summary>
    /// 
    /// </summary>
    public static class InputBindingDefinitionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bindings"></param>
        /// <returns></returns>
        public static ImmutableArray<InputBindingDefinition> Clone(this ImmutableArray<InputBindingDefinition> bindings)
        {
            if (bindings.IsDefault)
            {
                return default;
            }

            var builder = ImmutableArray.CreateBuilder<InputBindingDefinition>(bindings.Length);
            for (int i = 0; i < bindings.Length; i++)
            {
                builder.Add(bindings[i].Clone());
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="binding"></param>
        /// <returns></returns>
        public static InputBindingDefinition Clone(this InputBindingDefinition binding)
        {
            return new InputBindingDefinition
            {
                Scheme = binding.Scheme,
                Kind = binding.Kind,
                Priority = binding.Priority,
                ConsumesInput = binding.ConsumesInput,
                ContextMask = binding.ContextMask,
                Button = binding.Button.Clone(),
                Axis1D = binding.Axis1D.Clone(),
                Axis1DComposite = binding.Axis1DComposite.Clone(),
                Axis2D = binding.Axis2D.Clone(),
                Axis2DComposite = binding.Axis2DComposite.Clone(),
                Delta2D = binding.Delta2D.Clone()
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public static bool ContentEquals(this InputBindingDefinition left, InputBindingDefinition right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            if (left.Scheme != right.Scheme
                || left.Kind != right.Kind
                || left.Priority != right.Priority
                || left.ConsumesInput != right.ConsumesInput
                || left.ContextMask != right.ContextMask)
            {
                return false;
            }

            return left.Kind switch
            {
                InputBindingKind.Button => ButtonEquals(left.Button, right.Button),
                InputBindingKind.Axis1D => Axis1DEquals(left.Axis1D, right.Axis1D),
                InputBindingKind.Axis1DComposite => Axis1DCompositeEquals(left.Axis1DComposite, right.Axis1DComposite),
                InputBindingKind.Axis2D => Axis2DEquals(left.Axis2D, right.Axis2D),
                InputBindingKind.Axis2DComposite => Axis2DCompositeEquals(left.Axis2DComposite, right.Axis2DComposite),
                InputBindingKind.Delta2D => Delta2DEquals(left.Delta2D, right.Delta2D),
                _ => false
            };
        }

        private static ButtonBinding Clone(this in ButtonBinding binding)
        {
            return new ButtonBinding(
                binding.DeviceId,
                binding.ControlId,
                binding.Modifiers.IsDefault ? default : binding.Modifiers.ToImmutableArray()
            );
        }

        private static Axis1DBinding Clone(this in Axis1DBinding binding)
        {
            return new Axis1DBinding(
                binding.DeviceId,
                binding.ControlId,
                binding.Deadzone,
                binding.Sensitivity,
                binding.Scale,
                binding.Invert,
                binding.ResponseCurve
            );
        }

        private static Axis1DCompositeBinding Clone(this in Axis1DCompositeBinding binding)
        {
            return new Axis1DCompositeBinding(
                binding.Negative,
                binding.Positive,
                binding.Scale,
                binding.Normalize
            );
        }

        private static Axis2DBinding Clone(this in Axis2DBinding binding)
        {
            return new Axis2DBinding(
                binding.DeviceId,
                binding.ControlId,
                binding.Deadzone,
                binding.Sensitivity,
                binding.ScaleX,
                binding.ScaleY,
                binding.InvertX,
                binding.InvertY,
                binding.ResponseCurve
            );
        }

        private static Axis2DCompositeBinding Clone(this in Axis2DCompositeBinding binding)
        {
            return new Axis2DCompositeBinding(
                binding.Up,
                binding.Down,
                binding.Left,
                binding.Right,
                binding.Normalize,
                binding.ScaleX,
                binding.ScaleY
            );
        }

        private static Delta2DBinding Clone(this in Delta2DBinding binding)
        {
            return new Delta2DBinding(
                binding.DeviceId,
                binding.ControlId,
                binding.Sensitivity,
                binding.ScaleX,
                binding.ScaleY,
                binding.InvertX,
                binding.InvertY
            );
        }

        private static bool ButtonEquals(in ButtonBinding left, in ButtonBinding right)
        {
            if (left.DeviceId != right.DeviceId || left.ControlId != right.ControlId)
            {
                return false;
            }

            int leftLength = left.Modifiers.IsDefault ? 0 : left.Modifiers.Length;
            int rightLength = right.Modifiers.IsDefault ? 0 : right.Modifiers.Length;
            if (leftLength != rightLength)
            {
                return false;
            }

            for (int i = 0; i < leftLength; i++)
            {
                if (left.Modifiers[i] != right.Modifiers[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Axis1DEquals(in Axis1DBinding left, in Axis1DBinding right)
        {
            return left.DeviceId == right.DeviceId
                && left.ControlId == right.ControlId
                && left.Deadzone == right.Deadzone
                && left.Sensitivity == right.Sensitivity
                && left.Scale == right.Scale
                && left.Invert == right.Invert
                && left.ResponseCurve == right.ResponseCurve;
        }

        private static bool Axis1DCompositeEquals(in Axis1DCompositeBinding left, in Axis1DCompositeBinding right)
        {
            return left.Negative == right.Negative
                && left.Positive == right.Positive
                && left.Scale == right.Scale
                && left.Normalize == right.Normalize;
        }

        private static bool Axis2DEquals(in Axis2DBinding left, in Axis2DBinding right)
        {
            return left.DeviceId == right.DeviceId
                && left.ControlId == right.ControlId
                && left.Deadzone == right.Deadzone
                && left.Sensitivity == right.Sensitivity
                && left.ScaleX == right.ScaleX
                && left.ScaleY == right.ScaleY
                && left.InvertX == right.InvertX
                && left.InvertY == right.InvertY
                && left.ResponseCurve == right.ResponseCurve;
        }

        private static bool Axis2DCompositeEquals(in Axis2DCompositeBinding left, in Axis2DCompositeBinding right)
        {
            return left.Up == right.Up
                && left.Down == right.Down
                && left.Left == right.Left
                && left.Right == right.Right
                && left.Normalize == right.Normalize
                && left.ScaleX == right.ScaleX
                && left.ScaleY == right.ScaleY;
        }

        private static bool Delta2DEquals(in Delta2DBinding left, in Delta2DBinding right)
        {
            return left.DeviceId == right.DeviceId
                && left.ControlId == right.ControlId
                && left.Sensitivity == right.Sensitivity
                && left.ScaleX == right.ScaleX
                && left.ScaleY == right.ScaleY
                && left.InvertX == right.InvertX
                && left.InvertY == right.InvertY;
        }
    }
}
