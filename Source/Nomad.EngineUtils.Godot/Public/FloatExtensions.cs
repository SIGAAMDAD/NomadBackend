/*
===========================================================================
The Nomad MPLv2 Source Code
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using Nomad.Core.Engine.Windowing;

namespace Nomad.EngineUtils
{
    /// <summary>
    /// 
    /// </summary>
    public static class FloatExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        public static AspectRatio ToAspectRatio(this float ratio)
        {
            if (ratio == 4.0f / 3.0f)
            {
                return AspectRatio.Aspect_4_3;
            }
            else if (ratio == 16.0f / 9.0f)
            {
                return AspectRatio.Aspect_16_9;
            }
            else if (ratio == 16.0f / 10.0f)
            {
                return AspectRatio.Aspect_16_10;
            }
            else if (ratio == 21.0f / 9.0f)
            {
                return AspectRatio.Aspect_21_9;
            }

            // just fallback on auto if nothing else is found to be valid
            return AspectRatio.Aspect_Automatic;
        }
    }
}
