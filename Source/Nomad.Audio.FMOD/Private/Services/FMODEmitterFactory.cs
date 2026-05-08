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

using Nomad.Audio.Fmod.Private.Entities;
using Nomad.Audio.Fmod.Private.Repositories;
using Nomad.Audio.Fmod.Private.Services;
using Nomad.Audio.Interfaces;

namespace Nomad.Audio.Fmod.Private.Services
{
	internal sealed class FMODEmitterFactory : IEmitterFactory
	{
		private readonly FMODChannelService _channelRepository;
		private readonly FMODBusRepository _busRepository;

		public FMODEmitterFactory( FMODChannelService channelRepository, FMODBusRepository busRepository )
		{
			_channelRepository = channelRepository;
			_busRepository = busRepository;
		}

		public IAudioEmitter CreateEmitter( string category )
		{
			return new FMODEmitter( _channelRepository, _busRepository.GetSoundCategory( category ) );
		}
	};
};
