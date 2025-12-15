/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using Godot;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Interfaces.Common;
using NomadCore.Systems.EntitySystem.Domain.Events;
using NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects;
using System;

namespace NomadCore.Systems.EntitySystem.Domain.Models.Interfaces {
	public interface IRenderEntity : IDisposable, IEntity<RenderEntityId> {
		public bool Visible { get; set; }
		public uint VisibilityLayer { get; set; }
		public int ZIndex { get; set; }
		public int LightMask { get; set; }
		public Color Modulate { get; set; }

		public IGameEvent<RenderEntityVisibilityChangedEventArgs> VisibilityChanged { get; }
	};
};