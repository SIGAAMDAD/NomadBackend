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
using Nomad.Core.Events;
using Nomad.Core.Scene.GameObjects;
using Nomad.EngineTemplates.Attributes;
using Nomad.EngineTemplates.BaseClasses;

namespace Nomad.EngineTemplates.Scene.GameObjects
{
    /// <summary>
    /// Declares shared template metadata for animated 3D sprite wrappers.
    /// </summary>
    [TemplateClass(
        Contract = typeof(IAnimatedSprite3D),
        GodotBase = "global::Godot.AnimatedSprite3D",
        UnityBase = "global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter",
        Documentation = "Represents a frame-based animated 3D sprite.")]
    [TemplateEvent(
        Name = "AnimationFinished",
        PayloadType = typeof(EmptyEventArgs),
        Documentation = "Fired when the current animation finishes.",
        GodotHookExpression = "base.AnimationFinished += () => {{field}}.Publish(default);",
        UnityHookExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().AnimationFinished += animationName => {{field}}.Publish(default);")]
    [TemplateEvent(
        Name = "AnimationLooped",
        PayloadType = typeof(EmptyEventArgs),
        Documentation = "Fired when the current animation loops.",
        GodotHookExpression = "base.AnimationLooped += () => {{field}}.Publish(default);",
        UnityHookExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().AnimationFinished += animationName => {{field}}.Publish(default);")]
    [TemplateProperty(
        Name = "Color",
        Type = typeof(Vector4),
        Documentation = "The sprite tint color in RGBA form.",
        GodotGetterExpression = "new global::System.Numerics.Vector4(base.Modulate.R, base.Modulate.G, base.Modulate.B, base.Modulate.A)",
        GodotSetterExpression = "base.Modulate = new global::Godot.Color(value.X, value.Y, value.Z, value.W)",
        UnityGetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().Color",
        UnitySetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().Color = value")]
    [TemplateProperty(
        Name = "FlipHorizontal",
        Type = typeof(bool),
        Documentation = "Whether the sprite is flipped horizontally.",
        GodotGetterExpression = "base.FlipH",
        GodotSetterExpression = "base.FlipH = value",
        UnityGetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().FlipHorizontal",
        UnitySetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().FlipHorizontal = value")]
    [TemplateProperty(
        Name = "FlipVertical",
        Type = typeof(bool),
        Documentation = "Whether the sprite is flipped vertically.",
        GodotGetterExpression = "base.FlipV",
        GodotSetterExpression = "base.FlipV = value",
        UnityGetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().FlipVertical",
        UnitySetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().FlipVertical = value")]
    [TemplateProperty(
        Name = "Animation",
        Type = typeof(string),
        Documentation = "The current animation name.",
        GodotGetterExpression = "base.Animation",
        GodotSetterExpression = "base.Animation = value",
        UnityGetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().Animation",
        UnitySetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().Animation = value")]
    [TemplateProperty(
        Name = "Frame",
        Type = typeof(int),
        Documentation = "The current frame index.",
        GodotGetterExpression = "base.Frame",
        GodotSetterExpression = "base.Frame = value",
        UnityGetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().Frame",
        UnitySetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().Frame = value")]
    [TemplateProperty(
        Name = "SpeedScale",
        Type = typeof(float),
        Documentation = "Playback speed multiplier.",
        GodotGetterExpression = "base.SpeedScale",
        GodotSetterExpression = "base.SpeedScale = value",
        UnityGetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().SpeedScale",
        UnitySetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().SpeedScale = value")]
    [TemplateProperty(
        Name = "IsPlaying",
        Type = typeof(bool),
        IsReadOnly = true,
        Documentation = "Whether the current animation is playing.",
        GodotGetterExpression = "base.IsPlaying()",
        UnityGetterExpression = "GetComponent<global::Nomad.Unity.Scene.GameObjects.AnimatedSprite2DAdapter>().IsPlaying")]
    [TemplateMethod(
        Name = "Play",
        Documentation = "Starts playback of the current animation.")]
    [TemplateMethod(
        Name = "Pause",
        Documentation = "Pauses playback.")]
    [TemplateMethod(
        Name = "Stop",
        Documentation = "Stops playback.")]
    [TemplateObject3D]
    internal class EngineAnimatedSprite3D
    {
    }
}
