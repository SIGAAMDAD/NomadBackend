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

#if false
using Godot;
using NomadCore.Abstractions.Services;
using NomadCore.Enums.Audio;
using NomadCore.Infrastructure;
using NomadCore.Infrastructure.Events;
using NomadCore.Interfaces.Audio;
using NomadCore.Systems.Audio.Common.Events;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NomadCore.Systems.Audio.Infrastructure.Godot {
	/*
	===================================================================================
	
	GodotAudioSource
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class GodotAudioSource : AudioSourceBase {
		private readonly AudioStreamPlayer _streamNode;
		private readonly List<AudioStreamPlayback> _streamPlaybacks = new List<AudioStreamPlayback>();
		private AudioServer.PlaybackType _playbackType = AudioServer.PlaybackType.Default;

		public readonly StreamFinished StreamFinished = new StreamFinished();


		public GodotAudioSource( SourceType type )
			: base( type )
		{
			_streamNode = new AudioStreamPlayer() {
				Name = nameof( StreamNode ),
				VolumeDb = Mathf.LinearToDb( _volume )
			};
		}

		/*
		===============
		PlaySound
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="looping"></param>
		public override void PlaySound( IAudioStream stream, bool looping = false ) {
			base.PlaySound( stream, looping );

			_streamNode.Stream = (AudioStream)stream.StreamedResource;
			if ( looping ) {
				var eventBus = ServiceRegistry.Get<IGameEventBusService>();
				eventBus.ConnectSignal( _streamNode, AudioStreamPlayer.SignalName.Finished, _streamNode, OnLoopStream );
			}
			_streamNode.Play();
		}

		/*
		===============
		Stop
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		public override void Stop() {
			base.Stop();

			_streamNode.Stop();
		}

		public void Update( float deltaTime ) {
			int removed = 0;
			for ( int i = 0; i < _streamPlaybacks.Count; i++ ) {
				AudioStreamPlayback playback = _streamPlaybacks[ i ];
				if ( RefCounted.IsInstanceValid( playback ) && !playback.IsPlaying() ) {
					_streamPlaybacks.RemoveAt( i );
					removed++;
				}
			}
			if ( removed > 0 ) {
				Finished.Publish( new AudioStreamFinished() );
			}
		}

		/*
		===============
		GetStreamNode
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		[MethodImpl( MethodImplOptions.AggressiveInlining )]
		protected override Node GetStreamNode() => _streamNode;

		/*
		===============
		OnLoopStream
		===============
		*/
		private void OnLoopStream() {
			PlaySound( _stream );
		}
	};
};
#endif