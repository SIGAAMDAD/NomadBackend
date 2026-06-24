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

using Nomad.Core.Scene.GameObjects;
using Nomad.EngineTemplates.Attributes;
using Nomad.EngineTemplates.Attributes.Properties;
using Nomad.EngineTemplates.BaseClasses;

namespace Nomad.EngineTemplates.Scene.GameObjects
{
    /// <summary>
    /// Declares the engine template for 3D light objects.
    /// </summary>
    [TemplateClass(Contract = typeof(ILight3D), GodotBase = "Godot.OmniLight3D", UnityBase = "UnityEngine.MonoBehaviour")]
    [TemplateNamespace(Name = "Scene.GameObjects")]
    [TemplateColorProperty]
    [TemplateProperty(
        Name = "Color",
        Type = typeof(global::System.Drawing.Color),
        GodotGetterExpression = "global::System.Drawing.Color.FromArgb((int)(base.LightColor.A * 255f), (int)(base.LightColor.R * 255f), (int)(base.LightColor.G * 255f), (int)(base.LightColor.B * 255f))",
        GodotSetterExpression = "base.LightColor = new global::Godot.Color(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f)",
        UnityGetterExpression = "GetComponent<global::UnityEngine.Light>() != null ? global::System.Drawing.Color.FromArgb((int)(GetComponent<global::UnityEngine.Light>().color.a * 255f), (int)(GetComponent<global::UnityEngine.Light>().color.r * 255f), (int)(GetComponent<global::UnityEngine.Light>().color.g * 255f), (int)(GetComponent<global::UnityEngine.Light>().color.b * 255f)) : global::System.Drawing.Color.Empty",
        UnitySetterExpression = "(GetComponent<global::UnityEngine.Light>() ?? throw new global::System.InvalidOperationException(\"A UnityEngine.Light component is required.\")).color = new global::UnityEngine.Color(value.R / 255f, value.G / 255f, value.B / 255f, value.A / 255f)")]
    [TemplateProperty(
        Name = "Intensity",
        Type = typeof(float),
        GodotSetterExpression = "base.LightEnergy = value",
        GodotGetterExpression = "base.LightEnergy",
        UnityGetterExpression = "GetComponent<global::UnityEngine.Light>() != null ? GetComponent<global::UnityEngine.Light>().intensity : 0f",
        UnitySetterExpression = "(GetComponent<global::UnityEngine.Light>() ?? throw new global::System.InvalidOperationException(\"A UnityEngine.Light component is required.\")).intensity = value")]
    [TemplateProperty(
        Name = "Range",
        Type = typeof(float),
        GodotSetterExpression = "base.OmniRange = value",
        GodotGetterExpression = "base.OmniRange",
        UnityGetterExpression = "GetComponent<global::UnityEngine.Light>() != null ? GetComponent<global::UnityEngine.Light>().range : 0f",
        UnitySetterExpression = "(GetComponent<global::UnityEngine.Light>() ?? throw new global::System.InvalidOperationException(\"A UnityEngine.Light component is required.\")).range = value")]
    [TemplateProperty(
        Name = "CastShadows",
        Type = typeof(bool),
        GodotGetterExpression = "base.ShadowEnabled",
        GodotSetterExpression = "base.ShadowEnabled = value",
        UnityGetterExpression = "GetComponent<global::UnityEngine.Light>() != null && GetComponent<global::UnityEngine.Light>().shadows != global::UnityEngine.LightShadows.None",
        UnitySetterExpression = "(GetComponent<global::UnityEngine.Light>() ?? throw new global::System.InvalidOperationException(\"A UnityEngine.Light component is required.\")).shadows = value ? global::UnityEngine.LightShadows.Soft : global::UnityEngine.LightShadows.None")]
    [TemplateObject3D]
    internal class EngineLight3D
    {
    }
}
