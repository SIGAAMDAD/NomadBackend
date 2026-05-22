using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Moq;
using Nomad.Core.CVars;
using Nomad.Core.Exceptions;
using Nomad.Core.FileSystem;
using NUnit.Framework;

using GlobalCVarSystem = Nomad.CVars.Global.CVarSystem;

namespace Nomad.CVars.Tests {
	[TestFixture]
	[Category("Nomad.CVars")]
	[Category("System")]
	[Category("Integration")]
	public sealed class CVarSystemWrapperTests {
		[SetUp]
		public void SetUp() {
			ResetGlobalInstance();
		}

		[TearDown]
		public void TearDown() {
			ResetGlobalInstance();
		}

		[Test]
		public void Instance_WhenNotInitialized_ThrowsSubsystemNotInitializedException() {
			Assert.Throws<SubsystemNotInitializedException>( () => {
				_ = GlobalCVarSystem.Instance;
			} );
		}

		[Test]
		public void Initialize_WhenInstanceIsNull_ThrowsArgumentNullException() {
			var exception = Assert.Throws<ArgumentNullException>( () => {
				Initialize( null! );
			} );

			Assert.That( exception!.ParamName, Is.EqualTo( "instance" ) );
		}

		[Test]
		public void Initialize_WhenInstanceIsValid_SetsInstance() {
			var service = new RecordingCVarSystemService();

			Initialize( service );

			Assert.That( GlobalCVarSystem.Instance, Is.SameAs( service ) );
		}

		[Test]
		public void Initialize_WhenCalledAgain_ReplacesInstance() {
			var first = new RecordingCVarSystemService();
			var second = new RecordingCVarSystemService();

			Initialize( first );
			Initialize( second );

			Assert.That( GlobalCVarSystem.Instance, Is.SameAs( second ) );
		}

		[Test]
		public void PublicMethods_WhenNotInitialized_ThrowSubsystemNotInitializedException() {
			CVarCreateInfo<int> createInfo = default!;
			var cvar = Mock.Of<ICVar>();
			var fileSystem = Mock.Of<IFileSystem>();

			AssertUninitialized( () => GlobalCVarSystem.Register( createInfo ) );
			AssertUninitialized( () => GlobalCVarSystem.Unregister( cvar ) );
			AssertUninitialized( () => GlobalCVarSystem.CVarExists( "test" ) );
			AssertUninitialized( () => GlobalCVarSystem.CVarExists<int>( "test" ) );
			AssertUninitialized( () => GlobalCVarSystem.GetCVar( "test" ) );
			AssertUninitialized( () => GlobalCVarSystem.GetCVar<int>( "test" ) );
			AssertUninitialized( () => GlobalCVarSystem.GetCVars() );
			AssertUninitialized( () => GlobalCVarSystem.GetCVarsWithValueType<int>() );
			AssertUninitialized( () => GlobalCVarSystem.GetCVarsInGroup( "Default" ) );
			AssertUninitialized( () => GlobalCVarSystem.GroupExists( "Default" ) );
			AssertUninitialized( () => GlobalCVarSystem.Restart() );
			AssertUninitialized( () => GlobalCVarSystem.TryFind( "test", out ICVar? _ ) );
			AssertUninitialized( () => GlobalCVarSystem.TryFind<int>( "test", out ICVar<int>? _ ) );
			AssertUninitialized( () => GlobalCVarSystem.Save( fileSystem, "config.ini" ) );
			AssertUninitialized( () => GlobalCVarSystem.Load( fileSystem, "config.ini" ) );
		}

		[Test]
		public void Register_DelegatesToInstance_AndReturnsResult() {
			var service = new RecordingCVarSystemService();
			var expected = Mock.Of<ICVar<int>>();
			CVarCreateInfo<int> createInfo = default!;

			service.RegisterReturn = expected;

			Initialize( service );

			var actual = GlobalCVarSystem.Register( createInfo );

			Assert.That( actual, Is.SameAs( expected ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "Register" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.EqualTo( typeof( int ) ) );
		}

		[Test]
		public void Unregister_DelegatesToInstance() {
			var service = new RecordingCVarSystemService();
			var cvar = Mock.Of<ICVar>();

			Initialize( service );

			GlobalCVarSystem.Unregister( cvar );

			Assert.That( service.SingleCall.Method, Is.EqualTo( "Unregister" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.SameAs( cvar ) );
		}

		[TestCase( true )]
		[TestCase( false )]
		public void CVarExists_DelegatesToInstance_AndReturnsResult( bool expected ) {
			var service = new RecordingCVarSystemService {
				CVarExistsReturn = expected
			};

			Initialize( service );

			var actual = GlobalCVarSystem.CVarExists( "r_fullscreen" );

			Assert.That( actual, Is.EqualTo( expected ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "CVarExists" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.EqualTo( "r_fullscreen" ) );
		}

		[TestCase( true )]
		[TestCase( false )]
		public void CVarExistsGeneric_DelegatesToInstance_AndReturnsResult( bool expected ) {
			var service = new RecordingCVarSystemService {
				CVarExistsGenericReturn = expected
			};

			Initialize( service );

			var actual = GlobalCVarSystem.CVarExists<int>( "r_width" );

			Assert.That( actual, Is.EqualTo( expected ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "CVarExists<T>" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.EqualTo( "r_width" ) );
			Assert.That( service.SingleCall.Args[ 1 ], Is.EqualTo( typeof( int ) ) );
		}

		[Test]
		public void GetCVar_DelegatesToInstance_AndReturnsResult() {
			var expected = Mock.Of<ICVar>();
			var service = new RecordingCVarSystemService {
				GetCVarReturn = expected
			};

			Initialize( service );

			var actual = GlobalCVarSystem.GetCVar( "g_godMode" );

			Assert.That( actual, Is.SameAs( expected ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "GetCVar" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.EqualTo( "g_godMode" ) );
		}

		[Test]
		public void GetCVarGeneric_DelegatesToInstance_AndReturnsResult() {
			var expected = Mock.Of<ICVar<float>>();
			var service = new RecordingCVarSystemService {
				GetCVarGenericReturn = expected
			};

			Initialize( service );

			var actual = GlobalCVarSystem.GetCVar<float>( "s_volume" );

			Assert.That( actual, Is.SameAs( expected ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "GetCVar<T>" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.EqualTo( "s_volume" ) );
			Assert.That( service.SingleCall.Args[ 1 ], Is.EqualTo( typeof( float ) ) );
		}

		[Test]
		public void GetCVars_DelegatesToInstance_AndReturnsResult() {
			var expected = new[]
			{
			Mock.Of<ICVar>(),
			Mock.Of<ICVar>()
		};

			var service = new RecordingCVarSystemService {
				GetCVarsReturn = expected
			};

			Initialize( service );

			var actual = GlobalCVarSystem.GetCVars();

			Assert.That( actual, Is.SameAs( expected ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "GetCVars" ) );
		}

		[Test]
		public void GetCVarsWithValueType_DelegatesToInstance_AndReturnsResult() {
			var expected = new[]
			{
			Mock.Of<ICVar<int>>(),
			Mock.Of<ICVar<int>>()
		};

			var service = new RecordingCVarSystemService {
				GetCVarsWithValueTypeReturn = expected
			};

			Initialize( service );

			var actual = GlobalCVarSystem.GetCVarsWithValueType<int>();

			Assert.That( actual, Is.SameAs( expected ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "GetCVarsWithValueType<T>" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.EqualTo( typeof( int ) ) );
		}

		[Test]
		public void GetCVarsInGroup_DelegatesToInstance_AndReturnsResult() {
			var expected = new[]
			{
			Mock.Of<ICVar>(),
			Mock.Of<ICVar>()
		};

			var service = new RecordingCVarSystemService {
				GetCVarsInGroupReturn = expected
			};

			Initialize( service );

			var actual = GlobalCVarSystem.GetCVarsInGroup( "Rendering" );

			Assert.That( actual, Is.SameAs( expected ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "GetCVarsInGroup" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.EqualTo( "Rendering" ) );
		}

		[TestCase( true )]
		[TestCase( false )]
		public void GroupExists_DelegatesToInstance_AndReturnsResult( bool expected ) {
			var service = new RecordingCVarSystemService {
				GroupExistsReturn = expected
			};

			Initialize( service );

			var actual = GlobalCVarSystem.GroupExists( "Audio" );

			Assert.That( actual, Is.EqualTo( expected ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "GroupExists" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.EqualTo( "Audio" ) );
		}

		[Test]
		public void Restart_DelegatesToInstance() {
			var service = new RecordingCVarSystemService();

			Initialize( service );

			GlobalCVarSystem.Restart();

			Assert.That( service.SingleCall.Method, Is.EqualTo( "Restart" ) );
		}

		[TestCase( true )]
		[TestCase( false )]
		public void TryFind_DelegatesToInstance_ReturnsResult_AndAssignsOutValue( bool expectedReturn ) {
			var expectedCVar = Mock.Of<ICVar>();

			var service = new RecordingCVarSystemService {
				TryFindReturn = expectedReturn,
				TryFindOut = expectedCVar
			};

			Initialize( service );

			var actualReturn = GlobalCVarSystem.TryFind( "com_showFps", out var actualCVar );

			Assert.That( actualReturn, Is.EqualTo( expectedReturn ) );
			Assert.That( actualCVar, Is.SameAs( expectedCVar ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "TryFind" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.EqualTo( "com_showFps" ) );
		}

		[TestCase( true )]
		[TestCase( false )]
		public void TryFindGeneric_DelegatesToInstance_ReturnsResult_AndAssignsOutValue( bool expectedReturn ) {
			var expectedCVar = Mock.Of<ICVar<bool>>();

			var service = new RecordingCVarSystemService {
				TryFindGenericReturn = expectedReturn,
				TryFindGenericOut = expectedCVar
			};

			Initialize( service );

			var actualReturn = GlobalCVarSystem.TryFind<bool>( "r_vsync", out var actualCVar );

			Assert.That( actualReturn, Is.EqualTo( expectedReturn ) );
			Assert.That( actualCVar, Is.SameAs( expectedCVar ) );
			Assert.That( service.SingleCall.Method, Is.EqualTo( "TryFind<T>" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.EqualTo( "r_vsync" ) );
			Assert.That( service.SingleCall.Args[ 1 ], Is.EqualTo( typeof( bool ) ) );
		}

		[Test]
		public void Save_DelegatesToInstance() {
			var service = new RecordingCVarSystemService();
			var fileSystem = Mock.Of<IFileSystem>();

			Initialize( service );

			GlobalCVarSystem.Save( fileSystem, "user.cfg" );

			Assert.That( service.SingleCall.Method, Is.EqualTo( "Save" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.SameAs( fileSystem ) );
			Assert.That( service.SingleCall.Args[ 1 ], Is.EqualTo( "user.cfg" ) );
		}

		[Test]
		public void Load_DelegatesToInstance() {
			var service = new RecordingCVarSystemService();
			var fileSystem = Mock.Of<IFileSystem>();

			Initialize( service );

			GlobalCVarSystem.Load( fileSystem, "user.cfg" );

			Assert.That( service.SingleCall.Method, Is.EqualTo( "Load" ) );
			Assert.That( service.SingleCall.Args[ 0 ], Is.SameAs( fileSystem ) );
			Assert.That( service.SingleCall.Args[ 1 ], Is.EqualTo( "user.cfg" ) );
		}

		private static void AssertUninitialized( TestDelegate action ) {
			Assert.Throws<SubsystemNotInitializedException>( action );
		}

		private static void Initialize( ICVarSystemService instance ) {
			var method = typeof( GlobalCVarSystem ).GetMethod(
				"Initialize",
				BindingFlags.Static | BindingFlags.NonPublic
			);

			Assert.That( method, Is.Not.Null );

			try {
				method!.Invoke( null, new object?[] { instance } );
			} catch ( TargetInvocationException exception ) when ( exception.InnerException is not null ) {
				ExceptionDispatchInfo.Capture( exception.InnerException ).Throw();
				throw;
			}
		}

		private static void ResetGlobalInstance() {
			var field = typeof( GlobalCVarSystem ).GetField(
				"_instance",
				BindingFlags.Static | BindingFlags.NonPublic
			);

			Assert.That( field, Is.Not.Null );

			field!.SetValue( null, null );
		}

		private sealed record CallRecord( string Method, object?[] Args );

		private sealed class RecordingCVarSystemService : ICVarSystemService {
			private readonly List<CallRecord> _calls = new();

			public object? RegisterReturn { get; set; }

			public bool CVarExistsReturn { get; set; }

			public bool CVarExistsGenericReturn { get; set; }

			public ICVar? GetCVarReturn { get; set; }

			public object? GetCVarGenericReturn { get; set; }

			public ICVar[] GetCVarsReturn { get; set; } = Array.Empty<ICVar>();

			public object? GetCVarsWithValueTypeReturn { get; set; }

			public ICVar[]? GetCVarsInGroupReturn { get; set; }

			public bool GroupExistsReturn { get; set; }

			public bool TryFindReturn { get; set; }

			public ICVar? TryFindOut { get; set; }

			public bool TryFindGenericReturn { get; set; }

			public object? TryFindGenericOut { get; set; }

			public CallRecord SingleCall {
				get {
					Assert.That( _calls, Has.Count.EqualTo( 1 ) );
					return _calls[ 0 ];
				}
			}

			public ICVar<T> Register<T>( CVarCreateInfo<T> createInfo ) {
				Record( "Register", typeof( T ) );
				return (ICVar<T>)RegisterReturn!;
			}

			public void Unregister( ICVar cvar ) {
				Record( "Unregister", cvar );
			}

			public void AddGroup( string groupName ) {
				Record( "AddGroup", groupName );
			}

			public bool CVarExists( string name ) {
				Record( "CVarExists", name );
				return CVarExistsReturn;
			}

			public bool CVarExists<T>( string name ) {
				Record( "CVarExists<T>", name, typeof( T ) );
				return CVarExistsGenericReturn;
			}

			public ICVar? GetCVar( string name ) {
				Record( "GetCVar", name );
				return GetCVarReturn;
			}

			public ICVar<T>? GetCVar<T>( string name ) {
				Record( "GetCVar<T>", name, typeof( T ) );
				return (ICVar<T>?)GetCVarGenericReturn;
			}

			public ICVar[] GetCVars() {
				Record( "GetCVars" );
				return GetCVarsReturn;
			}

			public ICVar<T>[] GetCVarsWithValueType<T>() {
				Record( "GetCVarsWithValueType<T>", typeof( T ) );
				return (ICVar<T>[])GetCVarsWithValueTypeReturn!;
			}

			public ICVar[]? GetCVarsInGroup( string groupName ) {
				Record( "GetCVarsInGroup", groupName );
				return GetCVarsInGroupReturn;
			}

			public bool GroupExists( string groupName ) {
				Record( "GroupExists", groupName );
				return GroupExistsReturn;
			}

			public void Restart() {
				Record( "Restart" );
			}

			public bool TryFind( string name, out ICVar? cvar ) {
				Record( "TryFind", name );
				cvar = TryFindOut;
				return TryFindReturn;
			}

			public bool TryFind<T>( string name, out ICVar<T>? cvar ) {
				Record( "TryFind<T>", name, typeof( T ) );
				cvar = (ICVar<T>?)TryFindGenericOut;
				return TryFindGenericReturn;
			}

			public void Save( IFileSystem fileSystem, string configFile ) {
				Record( "Save", fileSystem, configFile );
			}

			public void Load( IFileSystem fileSystem, string configFile ) {
				Record( "Load", fileSystem, configFile );
			}

			public void Dispose() {
				Record( "Dispose" );
			}

			private void Record( string method, params object?[] args ) {
				_calls.Add( new CallRecord( method, args ) );
			}
		}
	}
}