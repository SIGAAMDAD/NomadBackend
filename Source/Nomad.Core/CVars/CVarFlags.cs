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

using System;

namespace Nomad.Core.CVars
{
    /// <summary>
    /// Various flags that can be applied to a <see cref="CVar"/>.
    /// </summary>
    [Flags]
    public enum CVarFlags : uint
    {
        /// <summary>
        /// The default state for a CVar to be in, nothing fancy happens here.
        /// </summary>
        None = 0,

        /// <summary>
        /// Cannot change after initialization.
        /// </summary>
        ReadOnly = 1 << 0,

        /// <summary>
        /// Created in the console by the user.
        /// </summary>
        UserCreated = 1 << 2,

        /// <summary>
        /// Saves the CVar's value to the configuration file (usually settings.ini).
        /// </summary>
        Archive = 1 << 3,

        /// <summary>
        /// Hidden from console auto-completion.
        /// </summary>
        Hidden = 1 << 4,

        /// <summary>
        /// Can only be modified in debug/developer mode.
        /// </summary>
        Developer = 1 << 5,

        /// <summary>
        /// Cannot change from the console at all.
        /// </summary>
        Init = 1 << 6
    }
}
