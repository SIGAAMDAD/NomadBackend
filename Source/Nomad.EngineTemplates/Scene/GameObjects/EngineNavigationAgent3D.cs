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

namespace Nomad.EngineTemplates.Scene.GameObjects
{
    /// <summary>
    /// Declares the engine template for 3D navigation agents.
    /// </summary>
    [TemplateClass(Contract = typeof(INavigationAgent3D), GodotBase = "Godot.NavigationAgent3D", UnityBase = "UnityEngine.AI.NavMeshAgent")]
    [TemplateNamespace(Name = "Scene.GameObjects")]
    [TemplateProperty(
        Name = "TargetPosition",
        Type = typeof(Vector3),
        GodotGetterExpression = "new global::System.Numerics.Vector3(base.TargetPosition.X, base.TargetPosition.Y, base.TargetPosition.Z)",
        GodotSetterExpression = "base.TargetPosition = new global::Godot.Vector3(value.X, value.Y, value.Z)",
        UnityGetterExpression = "new global::System.Numerics.Vector3(base.destination.x, base.destination.y, base.destination.z)",
        UnitySetterExpression = "base.destination = new global::UnityEngine.Vector3(value.X, value.Y, value.Z)")]
    [TemplateProperty(
        Name = "Velocity",
        Type = typeof(Vector3),
        GodotGetterExpression = "new global::System.Numerics.Vector3(base.Velocity.X, base.Velocity.Y, base.Velocity.Z)",
        GodotSetterExpression = "base.Velocity = new global::Godot.Vector3(value.X, value.Y, value.Z)",
        UnityGetterExpression = "new global::System.Numerics.Vector3(base.velocity.x, base.velocity.y, base.velocity.z)",
        UnitySetterExpression = "base.velocity = new global::UnityEngine.Vector3(value.X, value.Y, value.Z)")]
    [TemplateProperty(
        Name = "NextPathPosition",
        Type = typeof(Vector3),
        GodotGetterExpression = "new global::System.Numerics.Vector3(base.GetNextPathPosition().X, base.GetNextPathPosition().Y, base.GetNextPathPosition().Z)",
        UnityGetterExpression = "new global::System.Numerics.Vector3(base.steeringTarget.x, base.steeringTarget.y, base.steeringTarget.z)",
        IsReadOnly = true)]
    [TemplateProperty(
        Name = "StoppingDistance",
        Type = typeof(float),
        GodotGetterExpression = "base.TargetDesiredDistance",
        GodotSetterExpression = "base.TargetDesiredDistance = value",
        UnityGetterExpression = "base.stoppingDistance",
        UnitySetterExpression = "base.stoppingDistance = value")]
    [TemplateProperty(
        Name = "AvoidanceEnabled",
        Type = typeof(bool),
        GodotGetterExpression = "base.AvoidanceEnabled",
        GodotSetterExpression = "base.AvoidanceEnabled = value",
        UnityGetterExpression = "base.obstacleAvoidanceType != global::UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance",
        UnitySetterExpression = "base.obstacleAvoidanceType = value ? global::UnityEngine.AI.ObstacleAvoidanceType.HighQualityObstacleAvoidance : global::UnityEngine.AI.ObstacleAvoidanceType.NoObstacleAvoidance")]
    [TemplateProperty(
        Name = "IsNavigationFinished",
        Type = typeof(bool),
        GodotGetterExpression = "base.IsNavigationFinished()",
        UnityGetterExpression = "!base.pathPending && ( !base.hasPath || base.remainingDistance <= base.stoppingDistance )",
        IsReadOnly = true)]
    internal class EngineNavigationAgent3D
    {
    }
}
