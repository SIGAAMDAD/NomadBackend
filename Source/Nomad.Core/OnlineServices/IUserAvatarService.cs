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
using System.Threading;
using System.Threading.Tasks;
using Nomad.Core.Util;

namespace Nomad.Core.OnlineServices
{
    public interface IUserAvatarService : IDisposable
    {
        bool SupportsAvatars { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="size"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [ResultObject("UserAvatarResult", isRecord: true)]
        [ResultObjectPayload("Status", typeof(AvatarStatus), order: 1)]
        [ResultObjectPayload("Source", typeof(AvatarSource), order: 2)]
        [ResultObjectPayload("Image", typeof(IDisposable), order: 3, IsOptional = true)]
        [ResultObjectSuccess("Status", "Image", MethodName = "Loaded")]
        ValueTask<UserAvatarResult> QueryAvatarAsync(PeerId userId, AvatarSize size, CancellationToken ct = default);
    }
}
