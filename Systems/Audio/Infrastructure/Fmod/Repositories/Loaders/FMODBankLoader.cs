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

using NomadCore.Domain.Models.ValueObjects;
using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.Audio.Domain.Models.Aggregates;
using NomadCore.Systems.Audio.Domain.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.Entities;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Models.ValueObjects;
using NomadCore.Systems.Audio.Infrastructure.Fmod.Services;
using NomadCore.Systems.ResourceCache.Application.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace NomadCore.Systems.Audio.Infrastructure.Fmod.Repositories.Loaders {
	/*
	===================================================================================
	
	FMODBankLoader
	
	===================================================================================
	*/
	/// <summary>
	/// Loads FMOD banks from disk.
	/// </summary>
	
	internal sealed class FMODBankLoader : IResourceLoader<BankComposite, BankId> {
		public IResourceLoader<BankComposite, BankId>.LoadCallback Load => LoadBank;
		public IResourceLoader<BankComposite, BankId>.LoadAsyncCallback LoadAsync => LoadBankAsync;

		private readonly FMODSystemService _fmodSystem;
		private readonly FMODGuidRepository _guidRepository;

		public FMODBankLoader( FMODSystemService fmodSystem, FMODGuidRepository guidRepository ) {
			_fmodSystem = fmodSystem;
			_guidRepository = guidRepository;
		}

		/*
		===============
		LoadBank
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		private Result<BankComposite> LoadBank( BankId id ) {
			var filePath = FilePath.FromResourcePath( $"res://Assets/Audio/{StringPool.FromInterned( id.Name )}" );

			FMODValidator.ValidateCall( _fmodSystem.StudioSystem.loadBankFile( filePath.OSPath, FMOD.Studio.LOAD_BANK_FLAGS.NORMAL, out var bank ) );
			FMODValidator.ValidateCall( bank.getEventList( out var events ) );
			FMODValidator.ValidateCall( bank.getPath( out var path ) );
			FMODValidator.ValidateCall( bank.getID( out var bankId ) );

			var eventCollection = new FMODEventCollection( events, _guidRepository );
			var resource = new FMODBankResource( bank );
			var metadata = new FMODBankMetadata( id );

			_guidRepository.AddBankId( path, new FMODBankId( bankId ) );

			return Result<BankComposite>.Success( new BankComposite( eventCollection, resource, metadata ) );
		}

		/*
		===============
		LoadBankAsync
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<Result<BankComposite>> LoadBankAsync( BankId id, CancellationToken ct = default ) {
			ct.ThrowIfCancellationRequested();

			var filePath = FilePath.FromResourcePath( $"res://Assets/Audio/{StringPool.FromInterned( id.Name )}" );

			FMODValidator.ValidateCall( _fmodSystem.StudioSystem.loadBankFile( filePath.OSPath, FMOD.Studio.LOAD_BANK_FLAGS.NONBLOCKING, out var bank ) );
			FMODValidator.ValidateCall( bank.getEventList( out var events ) );
			FMODValidator.ValidateCall( bank.getPath( out var path ) );
			FMODValidator.ValidateCall( bank.getID( out var bankId ) );

			var eventCollection = new FMODEventCollection( events, _guidRepository );
			var resource = new FMODBankResource( bank );
			var metadata = new FMODBankMetadata( id );

			_guidRepository.AddBankId( path, new FMODBankId( bankId ) );

			return Result<BankComposite>.Success( new BankComposite( eventCollection, resource, metadata ) );
		}
	};
};