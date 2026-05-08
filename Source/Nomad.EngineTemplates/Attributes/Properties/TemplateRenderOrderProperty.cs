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
    /// Declares the shared template metadata for render-order properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    [TemplateProperty(
        Name = "RenderOrder",
        Type = typeof(int),
        GodotSetterExpression = "base.ZIndex = value",
        GodotGetterExpression = "base.ZIndex",
        UnityGetterExpression = "GetComponent<global::UnityEngine.Renderer>() != null ? GetComponent<global::UnityEngine.Renderer>().sortingOrder : 0",
        UnitySetterExpression = "(GetComponent<global::UnityEngine.Renderer>() ?? throw new global::System.InvalidOperationException(\"A UnityEngine.Renderer component is required.\")).sortingOrder = value")]
    internal class TemplateRenderOrderProperty : Attribute
    {
    }
}
