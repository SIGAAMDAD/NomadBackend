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

using Nomad.Core.UI;
using Nomad.EngineTemplates.Attributes;
using Nomad.EngineTemplates.BaseClasses;

namespace Nomad.EngineTemplates.UI
{
    /// <summary>
    /// Declares the engine template for progress bar UI elements.
    /// </summary>
    [TemplateClass(Contract = typeof(IProgressBar), GodotBase = "Godot.ProgressBar", UnityBase = "UnityEngine.MonoBehaviour")]
    [TemplateNamespace(Name = "UI")]
    [TemplateEvent(Name = "ValueSet", PayloadType = typeof(float))]
    [TemplateProperty(Name = "Minimum", Type = typeof(float), GodotGetterExpression = "(float)base.MinValue", GodotSetterExpression = "base.MinValue = value")]
    [TemplateProperty(Name = "Maximum", Type = typeof(float), GodotGetterExpression = "(float)base.MaxValue", GodotSetterExpression = "base.MaxValue = value")]
    [TemplateUIElement]
    internal class EngineProgressBar
    {
    }
}
