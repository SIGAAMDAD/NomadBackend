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

using NomadCore.GameServices;
using NomadCore.Infrastructure.ServiceRegistry.Interfaces;
using NomadCore.Systems.Audio.Application.Interfaces;
using NomadCore.Systems.Audio.Domain.Interfaces;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Services;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Startup {
	/*
	===================================================================================
	
	FMODBootstrapper
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	public static class FMODBootstrapper {
		public static void Initialize( IServiceLocator locator, IServiceRegistry registry ) {
			var logger = locator.GetService<ILoggerService>();
			var eventFactory = locator.GetService<IGameEventRegistryService>();
			var cvarSystem = locator.GetService<ICVarSystemService>();

			try {
				var system = new FMODSystemService( locator, registry );
				
				registry.RegisterSingleton<IAudioSystemService>( system );
				registry.RegisterSingleton<IListenerService>( new FMODListenerService( logger, system ) );
			} catch ( FMODException e ) {
				logger.PrintError( $"FMODBootstrapper: error initializing FMOD audio system - {e.Error}\n{e.StackTrace}" );
			}
		}
	};
};