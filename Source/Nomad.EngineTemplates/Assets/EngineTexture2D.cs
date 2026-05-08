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
using Nomad.Core.Engine.Assets;
using Nomad.EngineTemplates.Attributes;

namespace Nomad.Engine.Assets
{
    /// <summary>
    /// Represents a 2D texture asset template.
    /// </summary>
    [TemplateClass(Contract = typeof(ITexture), GodotBase = "Godot.Texture2D", UnityBase = "UnityEngine.Texture", IsAsset = true)]
    [TemplateNamespace(Name = "Assets")]
    [TemplateProperty(Name = "Width", Type = typeof(int), GodotGetterExpression = "base.GetWidth()", UnityGetterExpression = "(this as global::UnityEngine.Texture2D).width", IsReadOnly = true)]
    [TemplateProperty(Name = "Height", Type = typeof(int), GodotGetterExpression = "base.GetHeight()", UnityGetterExpression = "(this as global::UnityEngine.Texture2D).height", IsReadOnly = true)]
    [TemplateProperty(Name = "EngineObject", Type = typeof(object), GodotGetterExpression = "this")]
    [TemplateProperty(
        Name = "Image",
        Type = typeof(ReadOnlyMemory<byte>),
        UnityGetterExpression = "new global::System.ReadOnlyMemory<byte>(base.GetRawTextureData<byte>().ToArray())",
        IsReadOnly = true
    )]
    internal class EngineTexture2D
    {
    }
}
