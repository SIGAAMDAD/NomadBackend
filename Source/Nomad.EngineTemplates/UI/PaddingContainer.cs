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
    [TemplateClass(Contract = typeof(IPaddingContainer), GodotBase = "Godot.MarginContainer", UnityBase = "UnityEngine.MonoBehaviour")]
    [TemplateNamespace(Name = "UI")]
    [TemplateUIElement]
    [TemplateProperty(
        Name = "Padding",
        Type = typeof(Thickness),
        GodotGetterExpression =
            "new global::Nomad.Core.UI.Thickness(GetThemeConstant(\"margin_left\"), GetThemeConstant(\"margin_top\"), GetConstant(\"margin_right\"), GetThemeConstant(\"margin_bottom\"))",
        IsReadOnly = true
    )]
    internal class PaddingContainer
    {
    }
}
