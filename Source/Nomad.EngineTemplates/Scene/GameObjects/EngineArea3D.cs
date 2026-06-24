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
using Nomad.EngineTemplates.BaseClasses;

namespace Nomad.EngineTemplates.Scene.GameObjects
{
    /// <summary>
    /// Declares the engine template for 3D trigger and overlap areas.
    /// </summary>
    [TemplateClass(Contract = typeof(IArea3D), GodotBase = "Godot.Area3D", UnityBase = "UnityEngine.Collider")]
    [TemplateNamespace(Name = "Scene.GameObjects")]
    [TemplateObject3D]
    [TemplateEvent(
        Name = "AreaEntered",
        PayloadType = typeof(Area3DEventArgs),
        GodotHookExpression = "base.AreaEntered += (area) => {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Area3DEventArgs(area as global::Nomad.Core.Scene.GameObjects.IArea3D ?? throw new global::System.InvalidCastException(\"The entered area does not implement IArea3D.\")));",
        UnityHookExpression = "(GetUnityComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>() ?? gameObject.AddComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>()).TriggerEntered += (other) => { var area = other as global::Nomad.Core.Scene.GameObjects.IArea3D; if (area != null) { {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Area3DEventArgs(area)); } };")]
    [TemplateEvent(
        Name = "AreaExited",
        PayloadType = typeof(Area3DEventArgs),
        GodotHookExpression = "base.AreaExited += (area) => {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Area3DEventArgs(area as global::Nomad.Core.Scene.GameObjects.IArea3D ?? throw new global::System.InvalidCastException(\"The exited area does not implement IArea3D.\")));",
        UnityHookExpression = "(GetUnityComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>() ?? gameObject.AddComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>()).TriggerExited += (other) => { var area = other as global::Nomad.Core.Scene.GameObjects.IArea3D; if (area != null) { {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Area3DEventArgs(area)); } };")]
    [TemplateEvent(
        Name = "AreaShapeEntered",
        PayloadType = typeof(Area3DShapeEventArgs),
        GodotHookExpression = "base.AreaShapeEntered += (_, area, areaShapeIndex, localShapeIndex) => {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Area3DShapeEventArgs(area as global::Nomad.Core.Scene.GameObjects.IArea3D ?? throw new global::System.InvalidCastException(\"The entered area does not implement IArea3D.\"), areaShapeIndex, localShapeIndex));",
        UnityHookExpression = "(GetUnityComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>() ?? gameObject.AddComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>()).TriggerEntered += (other) => { var area = other as global::Nomad.Core.Scene.GameObjects.IArea3D; if (area != null) { {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Area3DShapeEventArgs(area, -1, -1)); } };")]
    [TemplateEvent(
        Name = "AreaShapeExited",
        PayloadType = typeof(Area3DShapeEventArgs),
        GodotHookExpression = "base.AreaShapeExited += (_, area, areaShapeIndex, localShapeIndex) => {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Area3DShapeEventArgs(area as global::Nomad.Core.Scene.GameObjects.IArea3D ?? throw new global::System.InvalidCastException(\"The exited area does not implement IArea3D.\"), areaShapeIndex, localShapeIndex));",
        UnityHookExpression = "(GetUnityComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>() ?? gameObject.AddComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>()).TriggerExited += (other) => { var area = other as global::Nomad.Core.Scene.GameObjects.IArea3D; if (area != null) { {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Area3DShapeEventArgs(area, -1, -1)); } };")]
    [TemplateEvent(
        Name = "BodyEntered",
        PayloadType = typeof(Body3DEventArgs),
        GodotHookExpression = "base.BodyEntered += (body) => {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Body3DEventArgs(body as global::Nomad.Core.Scene.GameObjects.ICollisionBody3D ?? throw new global::System.InvalidCastException(\"The entered body does not implement ICollisionBody3D.\")));",
        UnityHookExpression = "(GetUnityComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>() ?? gameObject.AddComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>()).TriggerEntered += (other) => { if (other is global::Nomad.Core.Scene.GameObjects.IArea3D) { return; } var body = other as global::Nomad.Core.Scene.GameObjects.ICollisionBody3D; if (body != null) { {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Body3DEventArgs(body)); } };")]
    [TemplateEvent(
        Name = "BodyExited",
        PayloadType = typeof(Body3DEventArgs),
        GodotHookExpression = "base.BodyExited += (body) => {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Body3DEventArgs(body as global::Nomad.Core.Scene.GameObjects.ICollisionBody3D ?? throw new global::System.InvalidCastException(\"The exited body does not implement ICollisionBody3D.\")));",
        UnityHookExpression = "(GetUnityComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>() ?? gameObject.AddComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>()).TriggerExited += (other) => { if (other is global::Nomad.Core.Scene.GameObjects.IArea3D) { return; } var body = other as global::Nomad.Core.Scene.GameObjects.ICollisionBody3D; if (body != null) { {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Body3DEventArgs(body)); } };")]
    [TemplateEvent(
        Name = "BodyShapeEntered",
        PayloadType = typeof(Body3DShapeEventArgs),
        GodotHookExpression = "base.BodyShapeEntered += (_, body, bodyShapeIndex, localShapeIndex) => {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Body3DShapeEventArgs(body as global::Nomad.Core.Scene.GameObjects.ICollisionBody3D ?? throw new global::System.InvalidCastException(\"The entered body does not implement ICollisionBody3D.\"), bodyShapeIndex, localShapeIndex));",
        UnityHookExpression = "(GetUnityComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>() ?? gameObject.AddComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>()).TriggerEntered += (other) => { if (other is global::Nomad.Core.Scene.GameObjects.IArea3D) { return; } var body = other as global::Nomad.Core.Scene.GameObjects.ICollisionBody3D; if (body != null) { {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Body3DShapeEventArgs(body, -1, -1)); } };")]
    [TemplateEvent(
        Name = "BodyShapeExited",
        PayloadType = typeof(Body3DShapeEventArgs),
        GodotHookExpression = "base.BodyShapeExited += (_, body, bodyShapeIndex, localShapeIndex) => {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Body3DShapeEventArgs(body as global::Nomad.Core.Scene.GameObjects.ICollisionBody3D ?? throw new global::System.InvalidCastException(\"The exited body does not implement ICollisionBody3D.\"), bodyShapeIndex, localShapeIndex));",
        UnityHookExpression = "(GetUnityComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>() ?? gameObject.AddComponent<global::Nomad.EngineUtils.TriggerEventRelay3D>()).TriggerExited += (other) => { if (other is global::Nomad.Core.Scene.GameObjects.IArea3D) { return; } var body = other as global::Nomad.Core.Scene.GameObjects.ICollisionBody3D; if (body != null) { {{field}}.Publish(new global::Nomad.Core.Scene.GameObjects.Body3DShapeEventArgs(body, -1, -1)); } };")]
    [TemplateProperty(
        Name = "Monitoring",
        Type = typeof(bool),
        GodotGetterExpression = "base.Monitoring",
        GodotSetterExpression = "base.Monitoring = value",
        UnityGetterExpression = "base.enabled",
        UnitySetterExpression = "base.enabled = value")]
    [TemplateProperty(
        Name = "IsTrigger",
        Type = typeof(bool),
        GodotGetterExpression = "true",
        GodotSetterExpression = "_ = value",
        UnityGetterExpression = "base.isTrigger",
        UnitySetterExpression = "base.isTrigger = value")]
    internal class EngineArea3D
    {
    }
}
