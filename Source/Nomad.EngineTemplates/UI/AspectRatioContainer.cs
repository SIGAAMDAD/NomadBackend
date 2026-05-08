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

using Nomad.EngineTemplates.Attributes;
using Nomad.Core.UI;
using Nomad.Core.Engine.Windowing;
using Nomad.EngineTemplates.BaseClasses;

namespace Nomad.EngineTemplates.UI
{
    /// <summary>
    /// 
    /// </summary>
    [TemplateClass(Contract = typeof(IAspectRatioContainer), GodotBase = "Godot.AspectRatioContainer", UnityBase = "UnityEngine.UI.AspectRatioFitter")]
    [TemplateUIElement]
    [TemplateProperty(Name = "Ratio", Type = typeof(AspectRatioValue), GodotGetterExpression = "new global::Nomad.Core.Engine.Windowing.AspectRatioValue(base.Ratio)", GodotSetterExpression = "base.Ratio = (float)value")]
    internal sealed class AspectRatioContainer
    {
    }
}
