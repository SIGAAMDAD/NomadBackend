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
using Nomad.Core.Scene.GameObjects;
using Nomad.EngineTemplates.Attributes;
using Nomad.EngineTemplates.Attributes.Events;
using Nomad.EngineTemplates.Attributes.Properties;

namespace Nomad.EngineTemplates.BaseClasses
{
    /// <summary>
    /// Declares the shared template metadata for 3D object engine wrappers.
    /// </summary>
    [TemplateClass(Contract = typeof(IObject3D))]
    [TemplateBaseClass]
    [TemplateDisplayStateChangedEvent]
    [TemplateProperty(
        Name = "Show",
        Type = typeof(bool),
        GodotGetterExpression = "base.Visible",
        GodotSetterExpression = "base.Visible = value",
        UnityGetterExpression = "gameObject.activeSelf",
        UnitySetterExpression = "gameObject.SetActive(value)")]
    [TemplatePosition3DProperty]
    [TemplateScale3DProperty]
    [TemplateRotation3DProperty]
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    internal class TemplateObject3D : Attribute
    {
    }
}
