using System.Linq;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Input.Private.Services;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Loading")]
	[Category("Unit")]
	public class BindLoaderTests {
		private MockLogger _logger = new MockLogger();

		[OneTimeTearDown]
		public void TearDown() {
			_logger?.Dispose();
		}

		[Test]
		public void LoadBindDatabase_WhenFileDoesNotExist_ReturnsFalseAndEmptyBindings() {
			var fileSystem = new InputFileSystemFixture();
			var loader = new BindLoader( fileSystem.Object, _logger );

			bool loaded = loader.LoadBindDatabase( "Assets/Assets/Config/Binds/Missing.json", out var binds );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( loaded, Is.False );
				Assert.That( binds, Is.Empty );
			}
		}

		[Test]
		public void LoadBindDatabase_WithArrayPayload_ParsesMultipleBindingKinds() {
			const string path = "Assets/Config/Binds/TestBindings.json";
			var fileSystem = new InputFileSystemFixture( (path, """
			{
			  "Bindings": [
			    {
			      "Name": "Jump",
			      "Id": "player.jump",
			      "ValueType": "Button",
			      "Scheme": "KeyboardAndMouse",
			      "Bindings": {
			        "DeviceId": "Keyboard",
			        "ControlId": "Space",
			        "Modifiers": [ "Shift" ]
			      }
			    },
			    {
			      "Name": "Look",
			      "Id": "camera.look",
			      "ValueType": "Vector2",
			      "Scheme": "KeyboardAndMouse",
			      "Bindings": {
			        "Kind": "Delta2D",
			        "DeviceId": "Mouse",
			        "ControlId": "Delta",
			        "Sensitivity": 2.0,
			        "InvertY": true
			      }
			    }
			  ]
			}
			""") );
			var loader = new BindLoader( fileSystem.Object, _logger );

			bool loaded = loader.LoadBindDatabase( path, out var binds );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( loaded, Is.True );
				Assert.That( binds, Has.Length.EqualTo( 2 ) );
				Assert.That( binds[ 0 ].Name, Is.EqualTo( "Jump" ) );
				Assert.That( binds[ 0 ].Id, Is.EqualTo( "player.jump" ) );
				Assert.That( binds[ 0 ].Bindings[ 0 ].Kind, Is.EqualTo( InputBindingKind.Button ) );
				Assert.That( binds[ 0 ].Bindings[ 0 ].Button.Modifiers, Is.EqualTo( new[] { InputControlId.Shift } ) );
				Assert.That( binds[ 1 ].Name, Is.EqualTo( "Look" ) );
				Assert.That( binds[ 1 ].Id, Is.EqualTo( "camera.look" ) );
				Assert.That( binds[ 1 ].Bindings[ 0 ].Kind, Is.EqualTo( InputBindingKind.Delta2D ) );
				Assert.That( binds[ 1 ].Bindings[ 0 ].Delta2D.Sensitivity, Is.EqualTo( 2.0f ) );
				Assert.That( binds[ 1 ].Bindings[ 0 ].Delta2D.InvertY, Is.True );
			}
		}

		[Test]
		public void LoadBindDatabase_WithObjectPayload_UsesPropertyNameAsFallbackActionName() {
			const string path = "Assets/Config/Binds/ObjectBindings.json";
			var fileSystem = new InputFileSystemFixture( (path, """
			{
			  "Bindings": {
			    "Move": {
				  "Id": "Move",
			      "ValueType": "Vector2",
			      "Scheme": "KeyboardAndMouse",
			      "Bindings": {
			        "Kind": "Axis2DComposite",
			        "Up": "W",
			        "Down": "S",
			        "Left": "A",
			        "Right": "D"
			      }
			    }
			  }
			}
			""") );
			var loader = new BindLoader( fileSystem.Object, _logger );

			loader.LoadBindDatabase( path, out var binds );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( binds, Has.Length.EqualTo( 1 ) );
				Assert.That( binds[ 0 ].Name, Is.EqualTo( "Move" ) );
				Assert.That( binds[ 0 ].Id, Is.EqualTo( "Move" ) );
				Assert.That( binds[ 0 ].Bindings[ 0 ].Kind, Is.EqualTo( InputBindingKind.Axis2DComposite ) );
			}
		}

		[Test]
		public void LoadBindDatabase_MergesDuplicateActionNamesWithinTheSameFile() {
			const string path = "Assets/Config/Binds/DuplicateActions.json";
			var fileSystem = new InputFileSystemFixture( (path, """
			{
			  "Bindings": [
			    {
				  "Id": "Shoot",
			      "Name": "Shoot",
			      "ValueType": "Button",
			      "Scheme": "KeyboardAndMouse",
			      "Bindings": { "DeviceId": "MouseButton", "ControlId": "Left" }
			    },
			    {
				  "Id": "Shoot",
			      "Name": "Shoot",
			      "ValueType": "Button",
			      "Scheme": "Gamepad",
			      "Bindings": { "DeviceId": "Gamepad", "ControlId": "A" }
			    }
			  ]
			}
			""") );
			var loader = new BindLoader( fileSystem.Object, _logger );

			loader.LoadBindDatabase( path, out var binds );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( binds, Has.Length.EqualTo( 1 ) );
				Assert.That( binds[ 0 ].Name, Is.EqualTo( "Shoot" ) );
				Assert.That( binds[ 0 ].Bindings, Has.Length.EqualTo( 2 ) );
				Assert.That( binds[ 0 ].Bindings.Select( binding => binding.Scheme ), Is.EquivalentTo( new[] { InputScheme.KeyboardAndMouse, InputScheme.Gamepad } ) );
			}
		}

		[Test]
		public void LoadBindDatabase_WhenKindIsOmittedForMouseMotion_InfersDelta2D() {
			const string path = "Assets/Config/Binds/InferDelta.json";
			var fileSystem = new InputFileSystemFixture( (path, """
			{
			  "Bindings": [
			    {
				  "Id": "Look",
			      "Name": "Look",
			      "ValueType": "Vector2",
			      "Scheme": "KeyboardAndMouse",
			      "Bindings": {
			        "DeviceId": "MouseMotion",
			        "ControlId": "Delta"
			      }
			    }
			  ]
			}
			""") );
			var loader = new BindLoader( fileSystem.Object, _logger );

			loader.LoadBindDatabase( path, out var binds );

			Assert.That( binds[ 0 ].Bindings[ 0 ].Kind, Is.EqualTo( InputBindingKind.Delta2D ) );
		}

		[Test]
		public void LoadBindDatabase_WhenDuplicateActionsDisagreeOnValueType_Throws() {
			const string path = "Assets/Config/Binds/ConflictingValueTypes.json";
			var fileSystem = new InputFileSystemFixture( (path, """
			{
			  "Bindings": [
			    {
				  "Id": "Move",
			      "Name": "Move",
			      "ValueType": "Button",
			      "Scheme": "KeyboardAndMouse",
			      "Bindings": { "DeviceId": "Keyboard", "ControlId": "W" }
			    },
			    {
				  "Id": "Move",
			      "Name": "Move",
			      "ValueType": "Vector2",
			      "Scheme": "KeyboardAndMouse",
			      "Bindings": {
			        "Kind": "Axis2DComposite",
			        "Up": "W",
			        "Down": "S",
			        "Left": "A",
			        "Right": "D"
			      }
			    }
			  ]
			}
			""") );
			var loader = new BindLoader( fileSystem.Object, _logger );

			Assert.That( () => loader.LoadBindDatabase( path, out _ ), Throws.Exception );
		}

		[Test]
		public void LoadBindDatabase_UsesVirtualFileSystemSearchDirectories() {
			var fileSystem = new InputFileSystemFixture( ("Assets/Config/Bindings/SearchOnly.json", """
			{
			  "Bindings": [
			    {
				  "Id": "Pause",
			      "Name": "Pause",
			      "ValueType": "Button",
			      "Scheme": "KeyboardAndMouse",
			      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Escape" }
			    }
			  ]
			}
			""") );
			var loader = new BindLoader( fileSystem.Object, _logger );

			bool loaded = loader.LoadBindDatabase( "SearchOnly.json", out var binds );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( loaded, Is.True );
				Assert.That( binds, Has.Length.EqualTo( 1 ) );
				Assert.That( binds[ 0 ].Name, Is.EqualTo( "Pause" ) );
				Assert.That( binds[ 0 ].Bindings[ 0 ].Button.ControlId, Is.EqualTo( InputControlId.Escape ) );
			}
		}
	}
}
