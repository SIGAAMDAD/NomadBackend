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
using NomadCore.GameServices;
using NomadCore.Domain.Models.Interfaces;
using NomadCore.Infrastructure.Collections;
using NomadCore.Systems.EntitySystem.Domain.Events;
using System;
using System.Collections.Generic;
using NomadCore.Systems.EntitySystem.Domain.Models.ValueObjects.Components;

namespace NomadCore.Systems.EntitySystem.Infrastructure.Rendering {
	/*
	===================================================================================
	
	AnimationEntity
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	internal sealed class AnimationEntity : ServerRenderEntity {
		private record FrameData( Texture2D Texture, float Duration );
		private record AnimationData( bool Loop, float Speed, int FrameCount, FrameData[] Frames );

		public bool Playing {
			get => _playing;
			set => _playing = value;
		}
		private bool _playing;

		public InternString Animation {
			get => _animation;
			set {
				if ( _animation == value ) {
					return;
				}
				_animation = value;
				Play( _animation );
			}
		}
		private InternString _animation;

		public float SpeedScale {
			get => _speedScale;
			set => _speedScale = value;
		}
		private float _speedScale = 0.0f;

		private readonly Dictionary<InternString, AnimationData> _animations;

		private int _currentFrame = 0;
		private float _customSpeedScale = 1.0f;
		private float _frameProgress = 0.0f;

		private Vector2 _offset = Vector2.Zero;

		private float _frameSpeedScale = 0.0f;

		public IGameEvent<AnimationChangedEventData> AnimationChanged => _animationChanged;
		private readonly IGameEvent<AnimationChangedEventData> _animationChanged;

		public IGameEvent<AnimationFinishedEventData> AnimationFinished => _animationFinished;
		private readonly IGameEvent<AnimationFinishedEventData> _animationFinished;

		public IGameEvent<AnimationLoopedEventData> AnimationLooped => _animationLooped;
		private readonly IGameEvent<AnimationLoopedEventData> _animationLooped;

		public IGameEvent<FrameChangedEventData> FrameChanged => _frameChanged;
		private readonly IGameEvent<FrameChangedEventData> _frameChanged;

		/*
		===============
		AnimationEntity
		===============
		*/
		public AnimationEntity( IGameEventRegistryService eventFactory, IGameEntity owner, AnimatedSprite2D animatedSprite )
			: base( eventFactory, owner, animatedSprite )
		{
			_animation = SceneStringPool.Intern( animatedSprite.Animation );

			_animationChanged = eventFactory.GetEvent<AnimationChangedEventData>( nameof( AnimationChanged ) );
			_animationFinished = eventFactory.GetEvent<AnimationFinishedEventData>( nameof( AnimationFinished ) );
			_animationLooped = eventFactory.GetEvent<AnimationLoopedEventData>( nameof( AnimationLooped ) );
			_frameChanged = eventFactory.GetEvent<FrameChangedEventData>( nameof( FrameChanged ) );

			ref var animationComponent = ref owner.GetOrAddComponent<AnimationStateComponent>();
			animationComponent.CurrentAnimation = SceneStringPool.Intern( animatedSprite.Animation );

			string[] animationNames = animatedSprite.SpriteFrames.GetAnimationNames();
			int animationCount = animationNames.Length;
			_animations = new Dictionary<InternString, AnimationData>( animationCount );

			SpriteFrames spriteFrames = animatedSprite.SpriteFrames;
			for ( int i = 0; i < animationCount; i++ ) {
				var animationName = animationNames[ i ];
				
				var frameCount = spriteFrames.GetFrameCount( animationName );
				var frames = new FrameData[ frameCount ];
				for ( int f = 0; f < frameCount; f++ ) {
					frames[ f ] = new FrameData(
						spriteFrames.GetFrameTexture( animationName, f ),
						spriteFrames.GetFrameDuration( animationName, f )
					);
				}

				_animations[ SceneStringPool.Intern( animationName ) ] = new AnimationData(
					spriteFrames.GetAnimationLoop( animationName ),
					(float)spriteFrames.GetAnimationSpeed( animationName ),
					frameCount,
					frames
				);
			}

			_currentFrame = animatedSprite.Frame;
			_offset = animatedSprite.Offset;

			if ( _animations.ContainsKey( SceneStringPool.Intern( animatedSprite.Autoplay ) ) ) {
				Play( SceneStringPool.Intern( animatedSprite.Autoplay ) );
			}
			animatedSprite.CallDeferred( AnimatedSprite2D.MethodName.QueueFree );
		}

		/*
		===============
		Play
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="animationName"></param>
		/// <param name="speedScale"></param>
		/// <param name="fromEnd"></param>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="KeyNotFoundException"></exception>
		public void Play( InternString animationName, float speedScale = 1.0f, bool fromEnd = false ) {
			if ( !_animations.TryGetValue( animationName, out var animation ) ) {
				throw new KeyNotFoundException( $"There is no animation with name '{animationName}'" );
			}

			if ( animation.FrameCount == 0 ) {
				return;
			}

			_playing = true;
			int endFrame = Math.Max( 0, animation.FrameCount - 1 );
			if ( animationName != _animation ) {
				_animation = animationName;
				
				if ( fromEnd ) {
					SetFrameAndProgress( endFrame, 1.0f );
				} else {
					SetFrameAndProgress( 0, 0.0f );
				}
				AnimationChanged.Publish( new AnimationChangedEventData() );
			} else {
				bool isBackward = Math.Sign( speedScale * _customSpeedScale ) == -1;
				
				if ( fromEnd && isBackward && _currentFrame == 0 && _frameProgress <= 0.0f ) {
					SetFrameAndProgress( endFrame, 1.0f );
				} else if ( !fromEnd && !isBackward && _currentFrame == endFrame && _frameProgress >= 1.0f ) {
					SetFrameAndProgress( 0, 0.0f );
				}
			}
		}

		/*
		===============
		Pause
		===============
		*/
		public void Pause() {
			StopInternal( false );
		}

		/*
		===============
		Stop
		===============
		*/
		public void Stop() {
			StopInternal( true );
		}

		/*
		===============
		Update
		===============
		*/
		public override void Update( float deltaTime ) {
			if ( !_playing || !_animations.TryGetValue( _animation, out var animation ) ) {
				return;
			}

			float remaining = deltaTime;
			int i = 0;
			while ( remaining > 0.0f ) {
				float speed = animation.Speed * _speedScale * _customSpeedScale * _frameSpeedScale;
				float absSpeed = MathF.Abs( speed );

				if ( speed == 0.0f ) {
					return;
				}

				int frameCount = animation.FrameCount;
				int lastFrame = frameCount - 1;
				if ( Math.Sign( speed ) == -1 ) {
					if ( _frameProgress >= 1.0f ) {
						if ( _currentFrame >= lastFrame ) {
							if ( animation.Loop ) {
								_currentFrame = 0;
								AnimationLooped.Publish( new AnimationLoopedEventData() );
							} else {
								_currentFrame = 0;
								Pause();
								AnimationFinished.Publish( new AnimationFinishedEventData() );
								return;
							}
						} else {
							_currentFrame++;
						}
						CalcFrameSpeedScale();
						_frameProgress = 0.0f;
						FrameChanged.Publish( new FrameChangedEventData() );
					}
					float toProcess = Math.Min( ( 1.0f - _frameProgress ) / absSpeed, remaining );
					_frameProgress += toProcess * absSpeed;
					remaining -= toProcess;
				} else {
					// backwards

					if ( _frameProgress <= 0.0f ) {
						if ( _currentFrame <= 0 ) {
							if ( animation.Loop ) {
								_currentFrame = lastFrame;
								Pause();
								AnimationFinished.Publish( new AnimationFinishedEventData() );
								return;
							}
						} else {
							_currentFrame--;
						}
						CalcFrameSpeedScale();
						_frameProgress = 1.0f;
						FrameChanged.Publish( new FrameChangedEventData() );
					}
					float toProcess = Math.Min( _frameProgress / absSpeed, remaining );
					_frameProgress = toProcess * absSpeed;
					remaining -= toProcess;
				}
				i++;
				if ( i > frameCount ) {
					return; // prevents freezing if toProcess is each time much less than remaining 
				}
			}
		}

		/*
		===============
		Draw
		===============
		*/
		public override void Draw( float deltaTime ) {
			if ( !_animations.TryGetValue( _animation, out var animation ) ) {
				return;
			}
			if ( !_owner.TryGetTarget( out var owner ) ) {
				return;
			}
			
			Texture2D texture = animation.Frames[ _currentFrame ].Texture;
			Vector2 offset = ( _offset + new Vector2( 0.5f, 0.5f ) ).Floor();
			Rect2 dstRect = new Rect2( offset, texture.GetSize() );

			ref var animationState = ref owner.GetComponent<AnimationStateComponent>();
			if ( animationState.HFlip ) {
				dstRect.Size = new Vector2( -dstRect.Size.X, dstRect.Size.Y );
			}
			if ( animationState.VFlip ) {
				dstRect.Size = new Vector2( dstRect.Size.X, -dstRect.Size.Y );
			}
			RenderingServer.CanvasItemAddTextureRectRegion( _canvasItemRid, dstRect, texture.GetRid(), new Rect2( new Vector2(), texture.GetSize() ) );
		}

		/*
		===============
		SetFrameAndProgress
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="progress"></param>
		private void SetFrameAndProgress( int frame, float progress ) {
			bool hasAnimation = _animations.ContainsKey( _animation );
			int endFrame = hasAnimation ? Math.Max( 0, _animations[ _animation ].FrameCount - 1 ) : 0;
			bool isChanged = _currentFrame != frame;

			if ( frame < 0 ) {
				_currentFrame = 0;
			} else if ( hasAnimation && frame > endFrame ) {
				_currentFrame = endFrame;
			} else {
				_currentFrame = frame;
			}

			CalcFrameSpeedScale();
			_frameProgress = progress;

			if ( !isChanged ) {
				return; // no changes, don't redraw
			}
			FrameChanged.Publish( new FrameChangedEventData() );
		}

		/*
		===============
		StopInternal
		===============
		*/
		private void StopInternal( bool reset ) {
			_playing = false;
			if ( reset ) {
				_customSpeedScale = 1.0f;
				SetFrameAndProgress( 0, 0.0f );
			}
		}
		
		/*
		===============
		GetFrameDuration
		===============
		*/
		private float GetFrameDuration() {
			if ( _animations.TryGetValue( _animation, out var animation ) ) {
				return animation.Frames[ _currentFrame ].Duration;
			}
			return 1.0f;
		}

		/*
		===============
		CalcFrameSpeedScale
		===============
		*/
		private void CalcFrameSpeedScale() {
			_frameSpeedScale = 1.0f / GetFrameDuration();
		}
	};
};