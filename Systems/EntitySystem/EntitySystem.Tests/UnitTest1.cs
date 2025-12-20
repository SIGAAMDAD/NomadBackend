namespace EntitySystem.Tests;

using Godot;
using NomadCore.Systems.EntitySystem.Application.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

public static class FastMath {
	// === INTEGER OPERATIONS (SAFE) ===

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static int Min( int a, int b ) {
		int mask = ( a - b ) >> 31;
		return a + ( mask & ( b - a ) );
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static int Max( int a, int b ) {
		int mask = ( a - b ) >> 31;
		return a - ( mask & ( a - b ) );
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static int Clamp( int value, int min, int max ) {
		value = Max( value, min );
		value = Min( value, max );
		return value;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static int Sign( int x ) {
		return ( x >> 31 ) - ( -x >> 31 );
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static int Abs( int x ) {
		// WARNING: Doesn't handle int.MinValue correctly
		// Use AbsSafe if you need full correctness
		int mask = x >> 31;
		return ( x + mask ) ^ mask;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static int AbsSafe( int x ) {
		// Handles int.MinValue (returns int.MaxValue)
		int mask = x >> 31;
		int abs = ( x + mask ) ^ mask;
		return abs == int.MinValue ? int.MaxValue : abs;
	}

	// === FLOAT OPERATIONS (CAREFUL WITH NaN) ===

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static float Min( float a, float b ) {
		// Use SSE if available (handles NaN correctly)
		if ( Sse.IsSupported ) {
			var va = Vector128.Create( a );
			var vb = Vector128.Create( b );
			return Sse.MinScalar( va, vb ).GetElement( 0 );
		}

		// Fallback: Use hardware min (might be branchless on CPU)
		// This MAY compile to MINSS instruction
		return a < b ? a : b;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static float Max( float a, float b ) {
		if ( Sse.IsSupported ) {
			var va = Vector128.Create( a );
			var vb = Vector128.Create( b );
			return Sse.MaxScalar( va, vb ).GetElement( 0 );
		}
		return a > b ? a : b;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static float Clamp( float value, float min, float max ) {
		if ( Sse.IsSupported ) {
			var v = Vector128.Create( value );
			v = Sse.MaxScalar( v, Vector128.Create( min ) );
			v = Sse.MinScalar( v, Vector128.Create( max ) );
			return v.GetElement( 0 );
		}

		// Branching fallback (but clearer)
		if ( value < min ) return min;
		if ( value > max ) return max;
		return value;
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static float Abs( float x ) {
		// Clear sign bit - handles NaN/Inf correctly
		int bits = BitConverter.SingleToInt32Bits( x );
		bits &= 0x7FFFFFFF;
		return BitConverter.Int32BitsToSingle( bits );
	}

	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	public static int Sign( float x ) {
		// For your game: do you expect NaN? If not, use fast version
		int bits = BitConverter.SingleToInt32Bits( x );
		int signBit = bits >> 31; // 0 for positive, 1 for negative
		int isZero = ( ( bits & 0x7FFFFFFF ) - 1 ) >> 31; // -1 if zero, 0 otherwise
		return ( 1 - ( signBit << 1 ) ) & ~isZero; // -1, 0, or 1
	}
}

public class Tests {
	private IEntityFactory _factory = new EntityFactory();
	
	[SetUp]
	public void Setup() {
	}

	[Test]
	public void Test1() {
		// don't u dare fucking say it
		float a = 6;
		float b = 7;

		var startFast = DateTime.UtcNow;
		for ( int i = 0; i < 1_000_000; i++ ) {
			float max = FastMath.Max( a, b );
		}
		var endFast = DateTime.UtcNow;

		Console.WriteLine( $"FastMath.Max over 1B iterations took { ( endFast - startFast ).Nanoseconds }" );
	}
	[Test]
	public void Test2() {
		float a = 6;
		float b = 7;

		var startBasic = DateTime.UtcNow;
		for ( int i = 0; i < 1_000_000; i++ ) {
			float max = Math.Max( a, b );
		}
		var endBasic = DateTime.UtcNow;

		Console.WriteLine( $"Math.Max over 1B iterations took { ( endBasic - startBasic ).Nanoseconds }" );
	}
};