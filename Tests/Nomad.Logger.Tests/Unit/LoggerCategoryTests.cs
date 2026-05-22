using NUnit.Framework;
using Nomad.Core.Logger;
using Nomad.Logger.Private;

namespace Nomad.Logger.Tests {
	[TestFixture]
	[Category("Nomad.Logger")]
	[Category("Unit")]
	public class LoggerCategoryTests {
		[Test]
		public void Constructor_ExposesNameLevelAndEnabledState() {
			var category = new LoggerCategory( "Audio", LogLevel.Warning, false, new MessageBuilder() );

			category.Enabled = true;

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( category.Name, Is.EqualTo( "Audio" ) );
				Assert.That( category.Level, Is.EqualTo( LogLevel.Warning ) );
				Assert.That( category.Enabled, Is.True );
			}
		}

		[Test]
		public void PrintLine_FormatsAndDispatchesToAllSinks() {
			var sinkA = new RecordingSink();
			var sinkB = new RecordingSink();
			var category = new LoggerCategory( "Gameplay", LogLevel.Info, true, new MessageBuilder() );
			category.AddSink( sinkA );
			category.AddSink( sinkB );

			category.PrintLine( "player spawned" );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( sinkA.WaitForMessages( 1 ), Is.True );
				Assert.That( sinkB.WaitForMessages( 1 ), Is.True );
				Assert.That( sinkA.Messages[0], Does.Contain( "[Gameplay] player spawned" ) );
				Assert.That( sinkB.Messages[0], Does.Contain( "[Gameplay] player spawned" ) );
			}
		}

		[Test]
		public void PrintWarningErrorAndDebug_DispatchRawMessages() {
			var sink = new RecordingSink();
			var category = new LoggerCategory( "Gameplay", LogLevel.Debug, true, new MessageBuilder() );
			category.AddSink( sink );

			category.PrintWarning( "warning" );
			category.PrintError( "error" );
			category.PrintDebug( "debug" );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( sink.WaitForMessages( 3 ), Is.True );
				Assert.That( sink.Messages, Is.EquivalentTo( new[] { "warning", "error", "debug" } ) );
			}
		}

		[Test]
		public void RemoveSink_StopsFutureDispatchToThatSink() {
			var removed = new RecordingSink();
			var retained = new RecordingSink();
			var category = new LoggerCategory( "Gameplay", LogLevel.Info, true, new MessageBuilder() );
			category.AddSink( removed );
			category.AddSink( retained );
			category.RemoveSink( removed );

			category.PrintLine( "only retained" );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( retained.WaitForMessages( 1 ), Is.True );
				Assert.That( removed.Messages, Is.Empty );
			}
		}

		[Test]
		public void Dispose_ClearsSinksSoQueuedMessagesAreNotDispatched() {
			var sink = new RecordingSink();
			var category = new LoggerCategory( "Gameplay", LogLevel.Info, true, new MessageBuilder() );
			category.AddSink( sink );

			category.Dispose();
			category.PrintLine( "after dispose" );

			Assert.That( sink.WaitForMessages( 1, 350 ), Is.False );
		}

		[Test]
		public void LoggerThread_WhenSinkThrows_CatchesExceptionAndStopsDispatchLoop() {
			var throwing = new ThrowingSink();
			var sink = new RecordingSink();
			var category = new LoggerCategory( "Gameplay", LogLevel.Info, true, new MessageBuilder() );
			category.AddSink( throwing );
			category.AddSink( sink );

			category.PrintLine( "boom" );

			Assert.That( sink.WaitForMessages( 1, 500 ), Is.False );
		}
	}
}
