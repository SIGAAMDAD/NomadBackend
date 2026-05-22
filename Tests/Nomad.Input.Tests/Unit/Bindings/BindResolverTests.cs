using System.Collections.Immutable;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Events;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.Services;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Bindings")]
	[Category("Unit")]
	public class BindResolverTests {
		private const string DefaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
		private GameEventRegistry _eventRegistry;
		private MockLogger _logger;

		[SetUp]
		public void SetUp() {
			_eventRegistry = InputTestHelpers.CreateEventRegistry( out _logger );
		}

		[TearDown]
		public void TearDown() {
			_eventRegistry.Dispose();
			_logger.Dispose();
		}

		[Test]
		public void Constructor_RequiresRepositoryAndCallback() {
			using var repository = CreateRepository();

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( () => new BindResolver( null, () => { } ), Throws.ArgumentNullException );
				Assert.That( () => new BindResolver( repository, null ), Throws.ArgumentNullException );
			}
		}

		[Test]
		public void Queries_DelegateToRepository() {
			using var repository = CreateRepository();
			var resolver = new BindResolver( repository, () => { } );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( resolver.GetBindMapping( "Gameplay" ), Is.Not.Null );
				Assert.That( resolver.GetBindMapping( "Missing" ), Is.Null );
				Assert.That( resolver.GetMappingsForScheme( InputScheme.KeyboardAndMouse ), Is.EquivalentTo( new[] { "Gameplay" } ) );
				Assert.That( resolver.GetActiveMapping( InputScheme.KeyboardAndMouse ), Is.EqualTo( "Gameplay" ) );
			}
		}

		[Test]
		public void LoadMapping_OnlyRaisesChangedWhenRepositoryAcceptsMapping() {
			using var repository = CreateRepository();
			int changedCount = 0;
			var resolver = new BindResolver( repository, () => changedCount++ );

			bool missing = resolver.LoadMapping( InputScheme.KeyboardAndMouse, "Missing" );
			bool loaded = resolver.LoadMapping( InputScheme.KeyboardAndMouse, "Gameplay" );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( missing, Is.False );
				Assert.That( loaded, Is.True );
				Assert.That( changedCount, Is.EqualTo( 1 ) );
			}
		}

		[Test]
		public void SetActionBindings_OnlyRaisesChangedWhenRepositoryUpdatesAction() {
			using var repository = CreateRepository();
			int changedCount = 0;
			var resolver = new BindResolver( repository, () => changedCount++ );

			resolver.SetActionBindings( "Gameplay", "Missing", ImmutableArray<InputBindingDefinition>.Empty );
			resolver.SetActionBindings(
				"Gameplay",
				"player.jump",
				ImmutableArray.Create( InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Enter ) ) );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( changedCount, Is.EqualTo( 1 ) );
				Assert.That( repository.GetBindMappings()["Gameplay"][0].Bindings[0].Button.ControlId, Is.EqualTo( InputControlId.Enter ) );
			}
		}

		private BindRepository CreateRepository() {
			var fileSystem = new InputFileSystemFixture(
				( DefaultsPath, "{ \"Bindings\": [] }" ),
				( "Assets/Config/Bindings/Gameplay.json", """
				{
				  "Bindings": [
				    {
				      "Name": "Jump",
				      "Id": "player.jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				""" ) );
			var cvarSystem = InputTestHelpers.CreateCVarSystem( _eventRegistry, DefaultsPath );
			var repository = new BindRepository( fileSystem.Object, cvarSystem, _logger );
			repository.SetActiveMapping( InputScheme.KeyboardAndMouse, "Gameplay" );
			return repository;
		}
	}
}
