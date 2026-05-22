/*
===========================================================================
The Nomad Framework
Copyright (C) 2025-2026 Noah Van Til

This Source Code Form is subject to the terms of the Mozilla Public
License, v2. If a copy of the MPL was not distributed with this
file, You can obtain one at https://mozilla.org/MPL/2.0/.

This software is provided "as is", without warranty of any kind,
express or implied, including but not limited to the warranties
of merchantability, fitness for a particular purpose and noninfringement.
===========================================================================
*/

using System;
using System.Collections.Immutable;
using System.Linq;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Events;
using Nomad.Input.Private.Repositories;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests
{
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Bindings")]
	[Category("Unit")]
	public class BindRepositoryTests
	{
		private GameEventRegistry _eventRegistry;
		private MockLogger _logger;

		[SetUp]
		public void SetUp()
		{
			_eventRegistry = InputTestHelpers.CreateEventRegistry(out _logger);
		}

		[TearDown]
		public void TearDown()
		{
			_eventRegistry.Dispose();
			_logger.Dispose();
		}

		[Test]
		public void GetDefaultBindings_ReturnsBindingsLoadedFromConfiguredDefaultsFile()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			var defaults = repository.GetDefaultBindings();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(defaults, Has.Length.EqualTo(1));
				Assert.That(defaults[0].Name, Is.EqualTo("Jump"));
			}
		}

		[Test]
		public void GetBindMappings_ReturnsDefaultsMergedIntoEachMapping()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				"""),
				("Assets/Config/Bindings/Gameplay.json", """
				{
				  "Bindings": [
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
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			var mappings = repository.GetBindMappings();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(mappings.ContainsKey("Gameplay"), Is.True);
				Assert.That(mappings["Gameplay"].Select(action => action.Name), Is.EquivalentTo(new[] { "Jump", "Move" }));
			}
		}

		[Test]
		public void GetAllBindings_MergesActionsAcrossDefaultsAndMappingsById()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
				    {
				      "Name": "Shoot",
				      "Id": "player.shoot",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "MouseButton", "ControlId": "Left" }
				    }
				  ]
				}
				"""),
				("Assets/Config/Bindings/Gamepad.json", """
				{
				  "Bindings": [
				    {
				      "Name": "Fire",
				      "Id": "player.shoot",
				      "ValueType": "Button",
				      "Scheme": "Gamepad",
				      "Bindings": { "DeviceId": "Gamepad", "ControlId": "A" }
				    }
				  ]
				}
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			var allBindings = repository.GetAllBindings();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(allBindings, Has.Length.EqualTo(1));
				Assert.That(allBindings[0].Name, Is.EqualTo("Shoot"));
				Assert.That(allBindings[0].Id, Is.EqualTo("player.shoot"));
				Assert.That(allBindings[0].Bindings, Has.Length.EqualTo(2));
			}
		}

		[Test]
		public void GetAllBindings_UsesOnlyTheActiveMappingForEachScheme()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
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
				"""),
				("Assets/Config/Bindings/KeyboardAndMouse.json", """
				{
				  "Bindings": [
				    {
					  "Id": "Move",
				      "Name": "Move",
				      "ValueType": "Vector2",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": {
				        "Kind": "Axis2DComposite",
				        "Up": "I",
				        "Down": "K",
				        "Left": "J",
				        "Right": "L"
				      }
				    }
				  ]
				}
				"""),
				("Assets/Config/Bindings/KeyboardAlternative.json", """
				{
				  "Bindings": [
				    {
					  "Id": "Move",
				      "Name": "Move",
				      "ValueType": "Vector2",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": {
				        "Kind": "Axis2DComposite",
				        "Up": "UpArrow",
				        "Down": "DownArrow",
				        "Left": "LeftArrow",
				        "Right": "RightArrow"
				      }
				    }
				  ]
				}
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			bool loaded = repository.SetActiveMapping(InputScheme.KeyboardAndMouse, "KeyboardAlternative");
			var allBindings = repository.GetAllBindings();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(loaded, Is.True);
				Assert.That(allBindings, Has.Length.EqualTo(1));
				Assert.That(allBindings[0].Bindings, Has.Length.EqualTo(1));
				Assert.That(allBindings[0].Bindings[0].Axis2DComposite.Up, Is.EqualTo(InputControlId.UpArrow));
			}
		}

		[Test]
		public void GetMappingsForScheme_ReturnsOnlyMappingsThatContainThatScheme()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, "{ \"Bindings\": [] }"),
				("Assets/Config/Bindings/KeyboardAndMouse.json", """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				"""),
				("Assets/Config/Bindings/Gamepad.json", """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "Gamepad",
				      "Bindings": { "DeviceId": "Gamepad", "ControlId": "A" }
				    }
				  ]
				}
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			var keyboardMappings = repository.GetMappingsForScheme(InputScheme.KeyboardAndMouse);
			var gamepadMappings = repository.GetMappingsForScheme(InputScheme.Gamepad);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(keyboardMappings, Is.EquivalentTo(new[] { "KeyboardAndMouse" }));
				Assert.That(gamepadMappings, Is.EquivalentTo(new[] { "Gamepad" }));
			}
		}

		[Test]
		public void TryGetBindMapping_WhenMappingDoesNotExist_ReturnsFalse()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": []
				}
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			bool found = repository.TryGetBindMapping("Missing", out var bindings);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(found, Is.False);
				Assert.That(bindings, Is.Empty);
			}
		}

		[Test]
		public void Reload_RefreshesTheMergedCachesAfterFileChanges()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				"""),
				("Assets/Config/Bindings/Gameplay.json", """
				{
				  "Bindings": [
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
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);
			fileSystem.SetFile("Assets/Config/Bindings/Gameplay.json", """
			{
			  "Bindings": [
			    {
				  "Id": "Crouch",
			      "Name": "Crouch",
			      "ValueType": "Button",
			      "Scheme": "KeyboardAndMouse",
			      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Ctrl" }
			    }
			  ]
			}
			""");

			repository.Reload();

			Assert.That(repository.GetBindMappings()["Gameplay"].Select(action => action.Name), Is.EquivalentTo(new[] { "Jump", "Crouch" }));
		}

		[Test]
		public void Constructor_WhenDefaultsFileIsMissing_ThrowsFileNotFoundException()
		{
			var fileSystem = new InputFileSystemFixture();
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, "Assets/Config/Bindings/MissingDefaults.json");

			Assert.That(() => new BindRepository(fileSystem.Object, cvarSystem, _logger), Throws.TypeOf<System.IO.FileNotFoundException>());
		}

		[Test]
		public void Reload_WhenTwoMappingsShareTheSameFilename_ThrowsInvalidOperationException()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": []
				}
				"""),
				("Assets/Config/Bindings/A/Gameplay.json", "{ \"Bindings\": [] }"),
				("Assets/Config/Bindings/B/Gameplay.json", "{ \"Bindings\": [] }")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			Assert.That(() => new BindRepository(fileSystem.Object, cvarSystem, _logger), Throws.TypeOf<InvalidOperationException>());
		}

		[Test]
		public void GetActiveMapping_ReturnsMappingLoadedFromConfiguredSettings()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, "{ \"Bindings\": [] }"),
				("Assets/Config/Bindings/KeyboardAlternative.json", """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				"""),
				("Assets/Config/Bindings/GamepadCustom.json", """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "Gamepad",
				      "Bindings": { "DeviceId": "Gamepad", "ControlId": "A" }
				    }
				  ]
				}
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);
			cvarSystem.SetCVar(Nomad.Input.Private.Constants.CVars.KEYBOARD_MOUSE_MAPPING, "KeyboardAlternative");
			cvarSystem.SetCVar(Nomad.Input.Private.Constants.CVars.GAMEPAD_MAPPING, "GamepadCustom");

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(repository.GetActiveMapping(InputScheme.KeyboardAndMouse), Is.EqualTo("KeyboardAlternative"));
				Assert.That(repository.GetActiveMapping(InputScheme.Gamepad), Is.EqualTo("GamepadCustom"));
			}
		}

		[Test]
		public void GetActiveMapping_IgnoresConfiguredMappingsThatDoNotMatchTheirScheme()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, "{ \"Bindings\": [] }"),
				("Assets/Config/Bindings/Gamepad.json", """
				{
				  "Bindings": [
				    {
					  "Id": "Confirm",
				      "Name": "Confirm",
				      "ValueType": "Button",
				      "Scheme": "Gamepad",
				      "Bindings": { "DeviceId": "Gamepad", "ControlId": "A" }
				    }
				  ]
				}
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);
			cvarSystem.SetCVar(Nomad.Input.Private.Constants.CVars.KEYBOARD_MOUSE_MAPPING, "Gamepad");

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			Assert.That(repository.GetActiveMapping(InputScheme.KeyboardAndMouse), Is.Null);
		}

		[Test]
		public void SetActiveMapping_WhenNameIsBlank_ReturnsFalse()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, "{ \"Bindings\": [] }"),
				("Assets/Config/Bindings/KeyboardAndMouse.json", """
				{
				  "Bindings": [
				    {
					  "Id": "Jump",
				      "Name": "Jump",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "Space" }
				    }
				  ]
				}
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			Assert.That(repository.SetActiveMapping(InputScheme.KeyboardAndMouse, "   "), Is.False);
		}

		[Test]
		public void SetActionBindings_ReplacesAnExistingBindingSetInTheNamedMapping()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, "{ \"Bindings\": [] }"),
				("Assets/Config/Bindings/Gameplay.json", """
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
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			bool updated = repository.SetActionBindings(
				"Gameplay",
				"player.jump",
				new[]
				{
					InputTestHelpers.Button(InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Enter)
				}.ToImmutableArray());

			using (Assert.EnterMultipleScope())
			{
				Assert.That(updated, Is.True);
				Assert.That(repository.GetBindMappings()["Gameplay"].Single(action => action.Name == "Jump").Bindings[0].Button.ControlId, Is.EqualTo(InputControlId.Enter));
			}
		}

		[Test]
		public void SetActionBindings_AddsActionToMappingWhenItOnlyExistsInDefaults()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
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
				"""),
				("Assets/Config/Bindings/Gameplay.json", "{ \"Bindings\": [] }")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			bool updated = repository.SetActionBindings(
				"Gameplay",
				"player.jump",
				new[]
				{
					InputTestHelpers.Button(InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Enter)
				}.ToImmutableArray());

			using (Assert.EnterMultipleScope())
			{
				Assert.That(updated, Is.True);
				Assert.That(repository.GetBindMappings()["Gameplay"].Single(action => action.Name == "Jump").Bindings[0].Button.ControlId, Is.EqualTo(InputControlId.Enter));
				Assert.That(repository.GetAllBindings().Single(action => action.Name == "Jump").Bindings[0].Button.ControlId, Is.EqualTo(InputControlId.Space));
			}
		}

		[Test]
		public void SetActionBindings_WhenMappingOrActionCannotBeResolved_ReturnsFalse()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, "{ \"Bindings\": [] }"),
				("Assets/Config/Bindings/Gameplay.json", "{ \"Bindings\": [] }")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			using var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(repository.SetActionBindings("", "Jump", Enumerable.Empty<InputBindingDefinition>().ToImmutableArray()), Is.False);
				Assert.That(repository.SetActionBindings("Missing", "Jump", Enumerable.Empty<InputBindingDefinition>().ToImmutableArray()), Is.False);
				Assert.That(repository.SetActionBindings("Gameplay", "Missing", Enumerable.Empty<InputBindingDefinition>().ToImmutableArray()), Is.False);
			}
		}

		[Test]
		public void Constructor_WhenActionValueTypesConflictAcrossDefaultsAndMapping_ThrowsInvalidOperationException()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, """
				{
				  "Bindings": [
				    {
					  "Id": "Interact",
				      "Name": "Interact",
				      "ValueType": "Button",
				      "Scheme": "KeyboardAndMouse",
				      "Bindings": { "DeviceId": "Keyboard", "ControlId": "E" }
				    }
				  ]
				}
				"""),
				("Assets/Config/Bindings/Gameplay.json", """
				{
				  "Bindings": [
				    {
					  "Id": "Interact",
				      "Name": "Interact",
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
				""")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			Assert.That(() => new BindRepository(fileSystem.Object, cvarSystem, _logger), Throws.TypeOf<InvalidOperationException>());
		}

		[Test]
		public void PublicMembers_WhenDisposed_ThrowObjectDisposedException()
		{
			const string defaultsPath = "Assets/Config/Bindings/DefaultBinds.json";
			var fileSystem = new InputFileSystemFixture(
				(defaultsPath, "{ \"Bindings\": [] }"),
				("Assets/Config/Bindings/Gameplay.json", "{ \"Bindings\": [] }")
			);
			var cvarSystem = InputTestHelpers.CreateCVarSystem(_eventRegistry, defaultsPath);

			var repository = new BindRepository(fileSystem.Object, cvarSystem, _logger);
			repository.Dispose();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(() => repository.GetDefaultBindings(), Throws.TypeOf<ObjectDisposedException>());
				Assert.That(() => repository.GetAllBindings(), Throws.TypeOf<ObjectDisposedException>());
				Assert.That(() => repository.GetBindMappings(), Throws.TypeOf<ObjectDisposedException>());
				Assert.That(() => repository.GetMappingsForScheme(InputScheme.KeyboardAndMouse), Throws.TypeOf<ObjectDisposedException>());
				Assert.That(() => repository.GetActiveMapping(InputScheme.KeyboardAndMouse), Throws.TypeOf<ObjectDisposedException>());
				Assert.That(() => repository.SetActiveMapping(InputScheme.KeyboardAndMouse, "Gameplay"), Throws.TypeOf<ObjectDisposedException>());
				Assert.That(() => repository.TryGetBindMapping("Gameplay", out _), Throws.TypeOf<ObjectDisposedException>());
				Assert.That(() => repository.SetActionBindings("Gameplay", "Jump", Enumerable.Empty<InputBindingDefinition>().ToImmutableArray()), Throws.TypeOf<ObjectDisposedException>());
				Assert.That(() => repository.Reload(), Throws.TypeOf<ObjectDisposedException>());
			}
		}
	}
}
