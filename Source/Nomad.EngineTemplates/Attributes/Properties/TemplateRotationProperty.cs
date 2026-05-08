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

namespace Nomad.EngineTemplates.Attributes.Properties
{
    /// <summary>
    /// Declares the shared template metadata for rotation properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    [TemplateProperty(
        Name = "Rotation",
        Type = typeof(float),
        Documentation = "Represents a SceneObject's rotation in degrees.",
        GodotSetterExpression = "base.RotationDegrees = value",
        GodotGetterExpression = "base.RotationDegrees",
        UnityGetterExpression = "transform.eulerAngles.z",
        UnitySetterExpression = "transform.rotation = global::UnityEngine.Quaternion.Euler(0f, 0f, value)")]
    internal class TemplateRotationProperty : Attribute
    {
    }
}
