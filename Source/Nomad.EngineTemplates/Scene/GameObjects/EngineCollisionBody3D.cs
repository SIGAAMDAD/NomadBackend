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
    /// Declares the shared engine template for 3D collision-participating bodies.
    /// </summary>
    [TemplateClass(Contract = typeof(ICollisionBody3D), GodotBase = "Godot.CollisionShape3D", UnityBase = "UnityEngine.Collider")]
    [TemplateNamespace(Name = "Scene.GameObjects")]
    [TemplateObject3D]
    internal class EngineCollisionBody3D
    {
    }
}
