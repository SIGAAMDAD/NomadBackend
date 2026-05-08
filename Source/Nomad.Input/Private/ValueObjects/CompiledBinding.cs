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
using Nomad.Core.Util;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Private.ValueObjects
{
	internal readonly struct CompiledBinding
	{
		public readonly InternString ActionName;
		public readonly InternString ActionId;
		public readonly int ActionIndex;

		public readonly InputValueType ValueType;
		public readonly InputBindingKind Kind;
		public readonly InputScheme Scheme;

		public readonly int Priority;
		public readonly int ScoreBase;
		public readonly bool ConsumesInput;
		public readonly uint ContextMask;

		public readonly ulong ModifierMask0;
		public readonly ulong ModifierMask1;
		public readonly ulong ModifierMask2;
		public readonly ulong ModifierMask3;

		public readonly ButtonBinding Button;
		public readonly Axis1DBinding Axis1D;
		public readonly Axis1DCompositeBinding Axis1DComposite;
		public readonly Axis2DBinding Axis2D;
		public readonly Axis2DCompositeBinding Axis2DComposite;
		public readonly Delta2DBinding Delta2D;

		public CompiledBinding(
			InternString actionName,
			InternString actionId,
			int actionIndex,
			InputValueType valueType,
			InputBindingKind kind,
			InputScheme scheme,
			int priority,
			int scoreBase,
			bool consumesInput,
			uint contextMask,
			ulong modifierMask0,
			ulong modifierMask1,
			ulong modifierMask2,
			ulong modifierMask3,
			ButtonBinding button,
			Axis1DBinding axis1D,
			Axis1DCompositeBinding axis1DComposite,
			Axis2DBinding axis2D,
			Axis2DCompositeBinding axis2DComposite,
			Delta2DBinding delta2D
		)
		{
			ActionName = actionName;
			ActionId = actionId;
			ActionIndex = actionIndex;
			ValueType = valueType;
			Kind = kind;
			Scheme = scheme;
			Priority = priority;
			ScoreBase = scoreBase;
			ConsumesInput = consumesInput;
			ContextMask = contextMask;
			ModifierMask0 = modifierMask0;
			ModifierMask1 = modifierMask1;
			ModifierMask2 = modifierMask2;
			ModifierMask3 = modifierMask3;
			Button = button;
			Axis1D = axis1D;
			Axis1DComposite = axis1DComposite;
			Axis2D = axis2D;
			Axis2DComposite = axis2DComposite;
			Delta2D = delta2D;
		}
	};
};
