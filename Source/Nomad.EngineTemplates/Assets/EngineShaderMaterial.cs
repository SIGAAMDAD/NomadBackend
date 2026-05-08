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

using Nomad.Core.Engine.Assets;
using Nomad.EngineTemplates.Attributes;

namespace Nomad.Engine.Assets
{
    /// <summary>
    /// Represents a shader-backed material asset template.
    /// </summary>
    [TemplateClass(Contract = typeof(IShaderMaterial), IsAsset = true, GodotBase = "Godot.ShaderMaterial", UnityBase = "UnityEngine.Material")]
    [TemplateNamespace(Name = "Assets")]
    [TemplateProperty(
        Name = "ShaderName",
        Type = typeof(string),
        GodotGetterExpression = "base.Shader != null ? base.Shader.ResourcePath : string.Empty",
        GodotSetterExpression = "base.Shader = global::Godot.ResourceLoader.Load<global::Godot.Shader>(value)",
        UnityGetterExpression = "base.shader != null ? base.shader.name : string.Empty",
        UnitySetterExpression = "base.shader = global::UnityEngine.Shader.Find(value)")]
    internal class EngineShaderMaterial
    {
    }
}
