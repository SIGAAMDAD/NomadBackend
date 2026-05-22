using System.Reflection;
using NUnit.Framework;
using Nomad.Core.Exceptions;
using Nomad.Core.Logger;
using Nomad.Logger.Globals;

namespace Nomad.Logger.Tests {
	[TestFixture]
	[Category("Nomad.Logger")]
	[Category("Unit")]
	public class LoggingGlobalTests {
		[SetUp]
		public void SetUp() {
			SetLoggingInstance( null );
		}

		[TearDown]
		public void TearDown() {
			SetLoggingInstance( null );
		}

		[Test]
		public void Instance_WhenNotInitialized_ThrowsSubsystemNotInitializedException() {
			Assert.Throws<SubsystemNotInitializedException>( () => _ = Logging.Instance );
		}

		[Test]
		public void Initialize_WhenInstanceIsNull_Throws() {
			Assert.That( () => InvokeInitialize( null ), Throws.TargetInvocationException.With.InnerException.TypeOf<System.ArgumentNullException>() );
		}

		[Test]
		public void StaticMethods_DelegateToInitializedLogger() {
			var logger = new MockLoggerService();
			InvokeInitialize( logger );
			var sink = new RecordingSink();

			Logging.PrintLine( "line" );
			Logging.PrintDebug( "debug" );
			Logging.PrintWarning( "warning" );
			Logging.PrintError( "error" );
			Logging.Clear();
			Logging.AddSink( sink );
			ILoggerCategory category = Logging.CreateCategory( "Gameplay", LogLevel.Warning, false );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( Logging.Instance, Is.SameAs( logger ) );
				Assert.That( logger.PrintLineCount, Is.EqualTo( 1 ) );
				Assert.That( logger.PrintDebugCount, Is.EqualTo( 1 ) );
				Assert.That( logger.PrintWarningCount, Is.EqualTo( 1 ) );
				Assert.That( logger.PrintErrorCount, Is.EqualTo( 1 ) );
				Assert.That( logger.ClearCount, Is.EqualTo( 1 ) );
				Assert.That( logger.Sink, Is.SameAs( sink ) );
				Assert.That( category.Name, Is.EqualTo( "Gameplay" ) );
			}
		}

		private static void InvokeInitialize( ILoggerService logger ) {
			typeof( Logging )
				.GetMethod( "Initialize", BindingFlags.Static | BindingFlags.NonPublic )
				.Invoke( null, new object[] { logger } );
		}

		private static void SetLoggingInstance( ILoggerService logger ) {
			typeof( Logging )
				.GetField( "_instance", BindingFlags.Static | BindingFlags.NonPublic )
				.SetValue( null, logger );
		}

		private sealed class MockLoggerService : ILoggerService {
			public int PrintLineCount { get; private set; }
			public int PrintDebugCount { get; private set; }
			public int PrintWarningCount { get; private set; }
			public int PrintErrorCount { get; private set; }
			public int ClearCount { get; private set; }
			public ILoggerSink Sink { get; private set; }

			public void PrintLine( string message ) => PrintLineCount++;
			public void PrintDebug( string message ) => PrintDebugCount++;
			public void PrintWarning( string message ) => PrintWarningCount++;
			public void PrintError( string message ) => PrintErrorCount++;
			public void Clear() => ClearCount++;
			public void InitConfig( Nomad.Core.CVars.ICVarSystemService cvarSystem ) { }
			public void AddSink( ILoggerSink sink ) => Sink = sink;
			public ILoggerCategory CreateCategory( string name, LogLevel level, bool enabled ) => new MockLoggerCategory( name, level, enabled );
			public void Dispose() { }
		}
	}
}
