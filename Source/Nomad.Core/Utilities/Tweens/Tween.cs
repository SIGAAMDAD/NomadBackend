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
using System.Collections.Generic;
using System.Diagnostics;

namespace NomadCore.Utilities.Tweens
{
    /*
	===================================================================================
	
	Tween
	
	===================================================================================
	*/
    /// <summary>
    /// 
    /// </summary>

    public class Tween : IDisposable
    {
        public float ElapsedTime => (float)_stopwatch.Elapsed.TotalSeconds;
        public float Progress => Math.Clamp((ElapsedTime - _delay) / _duration, 0f, 1f);
        public bool IsPlaying => _isPlaying;
        public bool IsCompleted => Progress >= 1f;

        private readonly List<ITweenProperty> _properties = new();
        private readonly Stopwatch _stopwatch = new();
        private float _duration;
        private IEasingFunction _easing = Easing.Linear;
        private Action _onCompleted;
        private bool _isPlaying;
        private float _delay;

        /*
		===============
		Dispose
		===============
		*/
        public void Dispose()
        {
            _properties.Clear();
            _easing = null;
            _onCompleted = null;
        }

        /*
		===============
		SetEase
		===============
		*/
        public Tween SetEase(IEasingFunction easing)
        {
            _easing = easing;
            return this;
        }

        /*
		===============
		SetDelay
		===============
		*/
        public Tween SetDelay(float delay)
        {
            _delay = Math.Max(0, delay);
            return this;
        }

        /*
		===============
		OnComplete
		===============
		*/
        public Tween OnComplete(Action action)
        {
            _onCompleted = action;
            return this;
        }

        /*
		===============
		Play
		===============
		*/
        public void Play()
        {
            if (_isPlaying)
            {
                return;
            }

            _isPlaying = true;
            _stopwatch.Restart();
        }

        /*
		===============
		Stop
		===============
		*/
        public void Stop()
        {
            _isPlaying = false;
            _stopwatch.Stop();
        }

        /*
		===============
		Update
		===============
		*/
        public void Update()
        {
            if (!_isPlaying || ElapsedTime < _delay)
            {
                return;
            }

            float t = _easing.Calculate(Progress);

            for (int i = 0; i < _properties.Count; i++)
            {
                _properties[i].Update(t);
            }

            if (IsCompleted)
            {
                _onCompleted?.Invoke();
                Stop();
            }
        }

        /*
		===============
		TweenProperty
		===============
		*/
        public Tween TweenProperty<T>(Action<T> setter, T start, T end, float duration)
        {
            _duration = Math.Max(_duration, duration);

            if (typeof(T) == typeof(float))
            {
                _properties.Add(new FloatTweenProperty(
                    setter as Action<float>,
                    Convert.ToSingle(start),
                    Convert.ToSingle(end),
                    duration
                ));
            }
            else if (typeof(T) == typeof(int))
            {
                _properties.Add(new IntTweenProperty(
                    setter as Action<int>,
                    Convert.ToInt32(start),
                    Convert.ToInt32(end),
                    duration
                ));
            }

            return this;
        }

        private interface ITweenProperty
        {
            void Update(float t);
        };

        private sealed class FloatTweenProperty(Action<float> setter, float start, float end, float duration) : ITweenProperty
        {
            private readonly Action<float> _setter = setter;
            private readonly float _start = start;
            private readonly float _end = end;
            private readonly float _duration = duration;

            /*
			===============
			Update
			===============
			*/
            public void Update(float t)
            {
                float value = _start + (_end - _start) * t;
                _setter?.Invoke(value);
            }
        };

        private sealed class IntTweenProperty(Action<int> setter, int start, int end, float duration) : ITweenProperty
        {
            private readonly Action<int> _setter = setter;
            private readonly int _start = start;
            private readonly int _end = end;
            private readonly float _duration = duration;

            /*
			===============
			Update
			===============
			*/
            public void Update(float t)
            {
                int value = (int)(_start + (_end - _start) * t);
                _setter?.Invoke(value);
            }
        }
    };
};