using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Nomad.Core.FileSystem;
using Nomad.Core.FileSystem.Configs;
using Nomad.Core.FileSystem.Streams;
using Nomad.Events;
using Nomad.Logger.Private.Sinks;

namespace Nomad.Logger.Tests {
	[TestFixture]
	[Category("Nomad.Logger")]
	[Category("Unit")]
	public class FileSinkTests {
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
		public void Constructor_RegistersLogFileCVarAndOpensConfiguredFileForOverwrite() {
			var cvars = new MockCVarSystem( _eventRegistry );
			var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );
			var writer = new Mock<IFileWriteStream>( MockBehavior.Strict );
			writer.SetupAllProperties();
			writer.Setup( stream => stream.Dispose() );
			writer.Setup( stream => stream.Flush() );
			fileSystem.Setup( fs => fs.OpenWrite( It.IsAny<IWriteConfig>() ) )
				.Returns( writer.Object );

			using var sink = new FileSink( cvars, fileSystem.Object );

			fileSystem.Verify( fs => fs.OpenWrite( It.Is<FileWriteConfig>( config => config.FilePath == "debug.log" && config.Append == false ) ), Times.Once );
		}

		[Test]
		public void PrintClearFlushAndDispose_DelegateToWriter() {
			var cvars = new MockCVarSystem( _eventRegistry );
			var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );
			var writer = new Mock<IFileWriteStream>( MockBehavior.Strict );
			fileSystem.Setup( fs => fs.OpenWrite( It.IsAny<IWriteConfig>() ) )
				.Returns( writer.Object );
			writer.Setup( stream => stream.WriteLine( "hello" ) );
			writer.Setup( stream => stream.SetLength( 0 ) );
			writer.Setup( stream => stream.Flush() );
			writer.Setup( stream => stream.Dispose() );

			var sink = new FileSink( cvars, fileSystem.Object );
			sink.Print( "hello" );
			sink.Clear();
			sink.Flush();
			sink.Dispose();

			using ( Assert.EnterMultipleScope() ) {
				writer.Verify( stream => stream.WriteLine( "hello" ), Times.Once );
				writer.Verify( stream => stream.SetLength( 0 ), Times.Once );
				writer.Verify( stream => stream.Flush(), Times.AtLeastOnce );
				writer.Verify( stream => stream.Dispose(), Times.Once );
			}
		}

		[Test]
		public void Constructor_WhenOpenWriteThrows_Rethrows() {
			var cvars = new MockCVarSystem( _eventRegistry );
			var fileSystem = new Mock<IFileSystem>( MockBehavior.Strict );
			fileSystem.Setup( fs => fs.OpenWrite( It.IsAny<IWriteConfig>() ) )
				.Throws( new InvalidOperationException( "no disk" ) );

			Assert.Throws<InvalidOperationException>( () => new FileSink( cvars, fileSystem.Object ) );
		}
	}
}
