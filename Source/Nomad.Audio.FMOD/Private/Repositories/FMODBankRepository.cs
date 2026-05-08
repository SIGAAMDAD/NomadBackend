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

using Nomad.Audio.Fmod.Private.Repositories.Loaders;
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Interfaces;
using Nomad.Core.Events;
using Nomad.Core.Logger;
using Nomad.ResourceCache;

namespace Nomad.Audio.Fmod.Private.Repositories
{
	/*
	===================================================================================

	FMODBankRepository

	===================================================================================
	*/
	/// <summary>
	///
	/// </summary>

	internal sealed class FMODBankRepository : BaseCache<IAudioResource, string>
	{
		public FMODBankRepository( ILoggerService logger, IGameEventRegistryService eventFactory, FMODDevice fmodSystem )
			: base( logger, eventFactory, new FMODBankLoader( fmodSystem, fmodSystem.GuidRepository, logger.CreateCategory( nameof( FMODBankLoader ), LogLevel.Info, true ) ) )
		{
		}
	};
};
