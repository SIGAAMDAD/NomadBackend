using NUnit.Framework;
using Nomad.Core.Logger;
using Nomad.Events;
using Nomad.Logger.Private.Services;

namespace Nomad.Logger.Tests {
	[TestFixture]
	[Category("Nomad.Logger")]
	[Category("Unit")]
	public class LoggerServiceTests {
		private GameEventRegistry _eventRegistry;
		private MockLogger _logger;

		[SetUp]
		public void SetUp() {
			_eventRegistry = new GameEventRegistry( _logger = new MockLogger() );
		}

		[TearDown]
		public void TearDown() {
			_eventRegistry.Dispose();
			_logger.Dispose();
		}

		[Test]
		public void Constructor_AllowsUnconfiguredInfoMessagesOnDefaultCategory() {
			using var service = new LoggerService();
			var sink = new RecordingSink();
			service.AddSink( sink );

			service.PrintLine( "hello" );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( sink.WaitForMessages( 1 ), Is.True );
				Assert.That( sink.Messages[0], Does.Contain( "[Logger] hello" ) );
			}
		}

		[Test]
		public void InitConfig_RegistersLogDepthAndFiltersDefaultMessages() {
			using var service = new LoggerService();
			var sink = new RecordingSink();
			var cvars = new MockCVarSystem( _eventRegistry );
			service.AddSink( sink );

			service.InitConfig( cvars );
			service.PrintError( "error" );
			service.PrintWarning( "warning" );
			service.PrintLine( "info" );
			service.PrintDebug( "debug" );

			Assert.That( sink.WaitForMessages( 1, 350 ), Is.False );
		}

		[Test]
		public void AddSink_WhenSinkIsNull_Throws() {
			using var service = new LoggerService();

			Assert.That( () => service.AddSink( null ), Throws.ArgumentNullException );
		}

		[Test]
		public void AddSink_AttachesToExistingDefaultCategoryAndClearDelegatesToAllSinks() {
			using var service = new LoggerService();
			var sinkA = new RecordingSink();
			var sinkB = new RecordingSink();

			service.AddSink( sinkA );
			service.AddSink( sinkB );
			service.PrintLine( "message" );
			service.Clear();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( sinkA.WaitForMessages( 1 ), Is.True );
				Assert.That( sinkB.WaitForMessages( 1 ), Is.True );
				Assert.That( sinkA.ClearCount, Is.EqualTo( 1 ) );
				Assert.That( sinkB.ClearCount, Is.EqualTo( 1 ) );
			}
		}

		[Test]
		public void CreateCategory_UsesConstructorValuesAndExistingSinks() {
			using var service = new LoggerService();
			var sink = new RecordingSink();
			service.AddSink( sink );

			ILoggerCategory category = service.CreateCategory( "Physics", LogLevel.Debug, false );
			category.PrintLine( "step" );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( category.Name, Is.EqualTo( "Physics" ) );
				Assert.That( category.Level, Is.EqualTo( LogLevel.Debug ) );
				Assert.That( category.Enabled, Is.False );
				Assert.That( sink.WaitForMessages( 1 ), Is.True );
				Assert.That( sink.Messages[0], Does.Contain( "[Physics] step" ) );
			}
		}

		[Test]
		public void CreateCategory_BeforeAddSink_DoesNotReceiveLaterGlobalSinkBecauseCategoryIsIndependent() {
			using var service = new LoggerService();
			ILoggerCategory category = service.CreateCategory( "Physics", LogLevel.Debug, true );
			var sink = new RecordingSink();

			service.AddSink( sink );
			category.PrintLine( "step" );

			Assert.That( sink.WaitForMessages( 1, 350 ), Is.False );
		}

		[Test]
		public void Dispose_DisposesSinksAndCanBeCalledMoreThanOnce() {
			var service = new LoggerService();
			var sink = new RecordingSink();
			service.AddSink( sink );

			service.Dispose();
			service.Dispose();

			Assert.That( sink.DisposeCount, Is.EqualTo( 1 ) );
		}
	}
}
