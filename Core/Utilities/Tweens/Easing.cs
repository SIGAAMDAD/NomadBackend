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

namespace NomadCore.Utilities.Tweens {
	/*
	===================================================================================
	
	Easing
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	
	public static class Easing {
		public static readonly IEasingFunction Linear = new LinearEasing();
		public static readonly IEasingFunction QuadIn = new QuadInEasing();
		public static readonly IEasingFunction QuadOut = new QuadOutEasing();
		public static readonly IEasingFunction QuadInOut = new QuadInOutEasing();
		public static readonly IEasingFunction CubicIn = new CubicInEasing();
		public static readonly IEasingFunction CubicOut = new CubicOutEasing();
		public static readonly IEasingFunction CubicInOut = new CubicInOutEasing();
		public static readonly IEasingFunction SineIn = new SineInEasing();
		public static readonly IEasingFunction SineOut = new SineOutEasing();
		public static readonly IEasingFunction SineInOut = new SineInOutEasing();
		public static readonly IEasingFunction BackIn = new BackInEasing();
		public static readonly IEasingFunction BackOut = new BackOutEasing();
		public static readonly IEasingFunction BackInOut = new BackInOutEasing();
		public static readonly IEasingFunction BounceIn = new BounceInEasing();
		public static readonly IEasingFunction BounceOut = new BounceOutEasing();
		public static readonly IEasingFunction BounceInOut = new BounceInOutEasing();
		public static readonly IEasingFunction ElasticIn = new ElasticInEasing();
		public static readonly IEasingFunction ElasticOut = new ElasticOutEasing();
		public static readonly IEasingFunction ElasticInOut = new ElasticInOutEasing();

		private readonly struct LinearEasing : IEasingFunction {
			public float Calculate( float t ) => t;
		};
		private readonly struct QuadInEasing : IEasingFunction {
			public float Calculate( float t ) => t * t;
		};
		private readonly struct QuadOutEasing : IEasingFunction {
			public float Calculate( float t ) => 1 - ( 1 - t ) * ( 1 - t );
		};
		private readonly struct QuadInOutEasing : IEasingFunction {
			public float Calculate( float t ) => t < 0.5f ? 2 * t * t : 1 - MathF.Pow( -2 * t + 2, 2 ) / 2;
		};
		private readonly struct CubicInEasing : IEasingFunction {
			public float Calculate( float t ) => t * t * t;
		};
		private readonly struct CubicOutEasing : IEasingFunction {
			public float Calculate( float t ) => 1 - MathF.Pow( 1 - t, 3 );
		};
		private readonly struct CubicInOutEasing : IEasingFunction {
			public float Calculate( float t ) => t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow( -2 * t + 2, 3 ) / 2;
		};
		private readonly struct SineInEasing : IEasingFunction {
			public float Calculate( float t ) => 1 - MathF.Cos( t * MathF.PI / 2 );
		};
		private readonly struct SineOutEasing : IEasingFunction {
			public float Calculate( float t ) => MathF.Sin( t * MathF.PI / 2 );
		};
		private readonly struct SineInOutEasing : IEasingFunction {
			public float Calculate( float t ) => -( MathF.Cos( MathF.PI * t ) - 1 ) / 2;
		};
		private readonly struct BackInEasing : IEasingFunction {
			private const float c1 = 1.70158f;
			public float Calculate( float t ) => ( c1 + 1 ) * t * t * t - c1 * t * t;
		};
		private readonly struct BackOutEasing : IEasingFunction {
			private const float c1 = 1.70158f;
			public float Calculate( float t ) => 1 + ( c1 + 1 ) * MathF.Pow( t - 1, 3 ) + c1 * MathF.Pow( t - 1, 2 );
		};
		private class BackInOutEasing : IEasingFunction {
			private const float c1 = 1.70158f;
			private const float c2 = c1 * 1.525f;
			public float Calculate( float t ) {
				return t < 0.5f
					? MathF.Pow( 2 * t, 2 ) * ( ( c2 + 1 ) * 2 * t - c2 ) / 2
					: ( MathF.Pow( 2 * t - 2, 2 ) * ( ( c2 + 1 ) * ( t * 2 - 2 ) + c2 ) + 2 ) / 2;
			}
		};
		private readonly struct BounceInEasing : IEasingFunction {
			public float Calculate( float t ) => 1 - BounceOutEasing.Instance.Calculate( 1 - t );
		};
		private readonly struct BounceOutEasing : IEasingFunction {
			public static readonly BounceOutEasing Instance = new();

			public float Calculate( float t ) {
				const float n1 = 7.5625f;
				const float d1 = 2.75f;

				if ( t < 1 / d1 ) return n1 * t * t;
				if ( t < 2 / d1 ) return n1 * ( t -= 1.5f / d1 ) * t + 0.75f;
				if ( t < 2.5f / d1 ) return n1 * ( t -= 2.25f / d1 ) * t + 0.9375f;
				return n1 * ( t -= 2.625f / d1 ) * t + 0.984375f;
			}
		};
		private readonly struct BounceInOutEasing : IEasingFunction {
			public float Calculate( float t ) => t < 0.5f
				? ( 1 - BounceOutEasing.Instance.Calculate( 1 - 2 * t ) ) / 2
				: ( 1 + BounceOutEasing.Instance.Calculate( 2 * t - 1 ) ) / 2;
		};
		private readonly struct ElasticInEasing : IEasingFunction {
			private const float c4 = ( 2 * MathF.PI ) / 3;
			public float Calculate( float t ) {
				return t == 0 ? 0 :
					   t == 1 ? 1 :
					   -MathF.Pow( 2, 10 * t - 10 ) * MathF.Sin( ( t * 10 - 10.75f ) * c4 );
			}
		};
		private readonly struct ElasticOutEasing : IEasingFunction {
			private const float c4 = ( 2 * MathF.PI ) / 3;
			public float Calculate( float t ) {
				return t == 0 ? 0 :
					   t == 1 ? 1 :
					   MathF.Pow( 2, -10 * t ) * MathF.Sin( ( t * 10 - 0.75f ) * c4 ) + 1;
			}
		};
		private readonly struct ElasticInOutEasing : IEasingFunction {
			private const float c5 = ( 2 * MathF.PI ) / 4.5f;
			public float Calculate( float t ) {
				return t == 0 ? 0 :
					   t == 1 ? 1 :
					   t < 0.5f
						   ? -( MathF.Pow( 2, 20 * t - 10 ) * MathF.Sin( ( 20 * t - 11.125f ) * c5 ) ) / 2
						   : MathF.Pow( 2, -20 * t + 10 ) * MathF.Sin( ( 20 * t - 11.125f ) * c5 ) / 2 + 1;
			}
		};
	};
};