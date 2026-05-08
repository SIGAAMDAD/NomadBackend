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

using Nomad.Core.Engine.Assets;
using Nomad.Core.UI;
using Nomad.EngineTemplates.Attributes;
using Nomad.EngineTemplates.BaseClasses;

namespace Nomad.EngineTemplates.UI
{
    /// <summary>
    /// 
    /// </summary>
    [TemplateClass(Contract = typeof(IImageView), GodotBase = "Godot.TextureRect", UnityBase = "UnityEngine.MonoBehaviour")]
    [TemplateNamespace(Name = "UI")]
    [TemplateUIElement]
    [TemplateProperty(Name = "Texture", Type = typeof(ITexture), Documentation = "The image to be displayed within this UI element.")]
    internal class ImageView
    {
    }
}
