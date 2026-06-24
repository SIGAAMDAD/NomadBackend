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

using System;
using System.Numerics;

namespace Nomad.EngineTemplates.Attributes.Properties
{
    /// <summary>
    /// Declares the shared template metadata for 3D position properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    [TemplateTypeConversion(
        AgnosticType = typeof(Vector3),
        AgnosticToGodotExpression = "new global::Godot.Vector3({{value}}.X, {{value}}.Y, {{value}}.Z)",
        GodotToAgnosticExpression = "new global::System.Numerics.Vector3({{value}}.X, {{value}}.Y, {{value}}.Z)",
        AgnosticToUnityExpression = "new global::UnityEngine.Vector3({{value}}.X, {{value}}.Y, {{value}}.Z)",
        UnityToAgnosticExpression = "new global::System.Numerics.Vector3({{value}}.x, {{value}}.y, {{value}}.z)")]
    [TemplateProperty(
        Name = "Position",
        Type = typeof(Vector3),
        Documentation = "Represents a SceneObject's position in 3D space.",
        GodotGetterExpression = "new global::System.Numerics.Vector3(base.Position.X, base.Position.Y, base.Position.Z)",
        GodotSetterExpression = "base.Position = new global::Godot.Vector3(value.X, value.Y, value.Z)",
        UnityGetterExpression = "new global::System.Numerics.Vector3(transform.localPosition.x, transform.localPosition.y, transform.localPosition.z)",
        UnitySetterExpression = "transform.localPosition = new global::UnityEngine.Vector3(value.X, value.Y, value.Z)")]
    internal class TemplatePosition3DProperty : Attribute
    {
    }
}
