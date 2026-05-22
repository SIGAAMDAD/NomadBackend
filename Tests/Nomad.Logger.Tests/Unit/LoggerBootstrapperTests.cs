using System.Reflection;
using NUnit.Framework;
using Nomad.Core.Logger;
using Nomad.Core.ServiceRegistry.Services;
using Nomad.Logger;
using Nomad.Logger.Globals;

namespace Nomad.Logger.Tests {
	[TestFixture]
	[Category("Nomad.Logger")]
	[Category("Unit")]
	public class LoggerBootstrapperTests {
		[SetUp]
		public void SetUp() {
			SetLoggingInstance( null );
		}

		[TearDown]
		public void TearDown() {
			SetLoggingInstance( null );
		}

		[Test]
		public void Initialize_WhenArgumentsAreNull_Throws() {
			var bootstrapper = new LoggerBootstrapper();
			using var registry = new ServiceCollection();
			using var locator = new ServiceLocator( registry );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( () => bootstrapper.Initialize( null, locator ), Throws.ArgumentNullException );
				Assert.That( () => bootstrapper.Initialize( registry, null ), Throws.ArgumentNullException );
			}
		}

		[Test]
		public void Initialize_RegistersLoggerServiceAndInitializesGlobalLogging() {
			var bootstrapper = new LoggerBootstrapper();
			using var registry = new ServiceCollection();
			using var locator = new ServiceLocator( registry );

			bootstrapper.Initialize( registry, locator );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( locator.TryGetService<ILoggerService>( out var service ), Is.True );
				Assert.That( service, Is.SameAs( Logging.Instance ) );
			}

			bootstrapper.Shutdown();
		}

		[Test]
		public void Shutdown_BeforeInitialize_DoesNotThrow() {
			var bootstrapper = new LoggerBootstrapper();

			Assert.DoesNotThrow( () => bootstrapper.Shutdown() );
		}

		private static void SetLoggingInstance( ILoggerService logger ) {
			typeof( Logging )
				.GetField( "_instance", BindingFlags.Static | BindingFlags.NonPublic )
				.SetValue( null, logger );
		}
	}
}
