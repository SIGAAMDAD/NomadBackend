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

namespace Nomad.EngineTemplates.Scene.GameObjects
{
    /// <summary>
    /// Declares the engine template for 3D camera objects.
    /// </summary>
    [TemplateClass(Contract = typeof(ICamera3D), GodotBase = "Godot.Camera3D", UnityBase = "UnityEngine.MonoBehaviour")]
    [TemplateNamespace(Name = "Scene.GameObjects")]
    [TemplateProperty(
        Name = "IsVisible",
        Type = typeof(bool),
        GodotGetterExpression = "base.Visible",
        GodotSetterExpression = "base.Visible = value",
        UnityGetterExpression = "GetComponent<global::UnityEngine.Camera>() != null && GetComponent<global::UnityEngine.Camera>().enabled",
        UnitySetterExpression = "(GetComponent<global::UnityEngine.Camera>() ?? throw new global::System.InvalidOperationException(\"A UnityEngine.Camera component is required.\")).enabled = value")]
    [TemplatePosition3DProperty]
    [TemplateScale3DProperty]
    [TemplateRotation3DProperty]
    [TemplateProperty(
        Name = "FieldOfView",
        Type = typeof(float),
        GodotGetterExpression = "base.Fov",
        GodotSetterExpression = "base.Fov = value",
        UnityGetterExpression = "GetComponent<global::UnityEngine.Camera>() != null ? GetComponent<global::UnityEngine.Camera>().fieldOfView : 0f",
        UnitySetterExpression = "(GetComponent<global::UnityEngine.Camera>() ?? throw new global::System.InvalidOperationException(\"A UnityEngine.Camera component is required.\")).fieldOfView = value")]
    [TemplateProperty(
        Name = "NearClip",
        Type = typeof(float),
        GodotGetterExpression = "base.Near",
        GodotSetterExpression = "base.Near = value",
        UnityGetterExpression = "GetComponent<global::UnityEngine.Camera>() != null ? GetComponent<global::UnityEngine.Camera>().nearClipPlane : 0f",
        UnitySetterExpression = "(GetComponent<global::UnityEngine.Camera>() ?? throw new global::System.InvalidOperationException(\"A UnityEngine.Camera component is required.\")).nearClipPlane = value")]
    [TemplateProperty(
        Name = "FarClip",
        Type = typeof(float),
        GodotGetterExpression = "base.Far",
        GodotSetterExpression = "base.Far = value",
        UnityGetterExpression = "GetComponent<global::UnityEngine.Camera>() != null ? GetComponent<global::UnityEngine.Camera>().farClipPlane : 0f",
        UnitySetterExpression = "(GetComponent<global::UnityEngine.Camera>() ?? throw new global::System.InvalidOperationException(\"A UnityEngine.Camera component is required.\")).farClipPlane = value")]
    internal class Camera3D
    {
    }
}
