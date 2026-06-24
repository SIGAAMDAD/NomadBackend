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

using System.Numerics;
using Nomad.Core.Scene.GameObjects;
using Nomad.EngineTemplates.Attributes;
using Nomad.EngineTemplates.BaseClasses;

namespace Nomad.EngineTemplates.Scene.GameObjects
{
    /// <summary>
    /// Declares shared template metadata for 3D sprite wrappers.
    /// </summary>
    [TemplateClass(
        Contract = typeof(ISprite3D),
        GodotBase = "global::Godot.Sprite3D",
        UnityBase = "global::UnityEngine.SpriteRenderer",
        Documentation = "Represents a rendered 3D sprite.")]
    [TemplateProperty(
        Name = "Color",
        Type = typeof(Vector4),
        Documentation = "The sprite tint color in RGBA form.",
        GodotGetterExpression = "new global::System.Numerics.Vector4(base.Modulate.R, base.Modulate.G, base.Modulate.B, base.Modulate.A)",
        GodotSetterExpression = "base.Modulate = new global::Godot.Color(value.X, value.Y, value.Z, value.W)",
        UnityGetterExpression = "new global::System.Numerics.Vector4(GetComponent<global::UnityEngine.SpriteRenderer>().color.r, GetComponent<global::UnityEngine.SpriteRenderer>().color.g, GetComponent<global::UnityEngine.SpriteRenderer>().color.b, GetComponent<global::UnityEngine.SpriteRenderer>().color.a)",
        UnitySetterExpression = "GetComponent<global::UnityEngine.SpriteRenderer>().color = new global::UnityEngine.Color(value.X, value.Y, value.Z, value.W)")]
    [TemplateProperty(
        Name = "FlipHorizontal",
        Type = typeof(bool),
        Documentation = "Whether the sprite is flipped horizontally.",
        GodotGetterExpression = "base.FlipH",
        GodotSetterExpression = "base.FlipH = value",
        UnityGetterExpression = "GetComponent<global::UnityEngine.SpriteRenderer>().flipX",
        UnitySetterExpression = "GetComponent<global::UnityEngine.SpriteRenderer>().flipX = value")]
    [TemplateProperty(
        Name = "FlipVertical",
        Type = typeof(bool),
        Documentation = "Whether the sprite is flipped vertically.",
        GodotGetterExpression = "base.FlipV",
        GodotSetterExpression = "base.FlipV = value",
        UnityGetterExpression = "GetComponent<global::UnityEngine.SpriteRenderer>().flipY",
        UnitySetterExpression = "GetComponent<global::UnityEngine.SpriteRenderer>().flipY = value")]
    [TemplateObject3D]
    internal class EngineSprite3D
    {
    }
}
