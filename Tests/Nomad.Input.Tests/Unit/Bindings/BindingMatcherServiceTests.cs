using System;
using System.Numerics;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.Services;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;
using Nomad.Core.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Bindings")]
	[Category("Unit")]
	public class BindingMatcherServiceTests {
		[Test]
		public void MatchKeyboard_ReturnsCandidateWhenContextAndSchemeMatch() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Jump",
					InputValueType.Button,
					InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space )
				)
			);
			var state = new InputStateService();
			var matcher = new BindingMatcherService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			BindingMatchSet matches = matcher.MatchKeyboard( graph, new KeyboardEventArgs( KeyNum.Space, 10, true ), uint.MaxValue, InputScheme.KeyboardAndMouse );

			Assert.That( matches.Length, Is.EqualTo( 1 ) );
		}

		[Test]
		public void MatchKeyboard_RequiresAllModifiersToBePressed() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"SprintJump",
					InputValueType.Button,
					InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space, InputControlId.Shift )
				)
			);
			var state = new InputStateService();
			var matcher = new BindingMatcherService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			BindingMatchSet withoutModifier = matcher.MatchKeyboard( graph, new KeyboardEventArgs( KeyNum.Space, 10, true ), uint.MaxValue, InputScheme.KeyboardAndMouse );
			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Shift, true );
			BindingMatchSet withModifier = matcher.MatchKeyboard( graph, new KeyboardEventArgs( KeyNum.Space, 10, true ), uint.MaxValue, InputScheme.KeyboardAndMouse );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( withoutModifier.Length, Is.Zero );
				Assert.That( withModifier.Length, Is.EqualTo( 1 ) );
				Assert.That( withModifier.Scores[0], Is.EqualTo( 35 ) );
			}
		}

		[Test]
		public void MatchMouseButton_UsesMouseButtonControlConversion() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"OpenContextMenu",
					InputValueType.Button,
					InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.Right )
				)
			);
			var matcher = new BindingMatcherService( repository, new InputStateService() );
			CompiledBindingGraph graph = repository.Current;

			BindingMatchSet matches = matcher.MatchMouseButton( graph, new MouseButtonEventArgs( MouseButton.Right, 10, true ), uint.MaxValue, InputScheme.KeyboardAndMouse );

			Assert.That( matches.Length, Is.EqualTo( 1 ) );
		}

		[Test]
		public void MatchGamepadButton_UsesTheEventDeviceId() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Confirm",
					InputValueType.Button,
					InputTestHelpers.Button( InputScheme.Gamepad, InputDeviceSlot.Gamepad1, InputControlId.GamepadA )
				)
			);
			var matcher = new BindingMatcherService( repository, new InputStateService() );
			CompiledBindingGraph graph = repository.Current;

			BindingMatchSet matches = matcher.MatchGamepadButton( graph, new GamepadButtonEventArgs( GamepadButton.A, 10, 1, true ), uint.MaxValue, InputScheme.Gamepad );

			Assert.That( matches.Length, Is.EqualTo( 1 ) );
		}

		[Test]
		public void MatchGamepadAxis_FiltersByContextAndScheme() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"LookStick",
					InputValueType.Vector2,
					InputTestHelpers.Axis2D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.RightStick )
				)
			);
			var matcher = new BindingMatcherService( repository, new InputStateService() );
			CompiledBindingGraph graph = repository.Current;

			BindingMatchSet wrongContext = matcher.MatchGamepadAxis( graph, InputDeviceSlot.Gamepad0, InputControlId.RightStick, 0, InputScheme.Gamepad );
			BindingMatchSet wrongScheme = matcher.MatchGamepadAxis( graph, InputDeviceSlot.Gamepad0, InputControlId.RightStick, uint.MaxValue, InputScheme.KeyboardAndMouse );
			BindingMatchSet correct = matcher.MatchGamepadAxis( graph, InputDeviceSlot.Gamepad0, InputControlId.RightStick, uint.MaxValue, InputScheme.Gamepad );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( wrongContext.Length, Is.Zero );
				Assert.That( wrongScheme.Length, Is.Zero );
				Assert.That( correct.Length, Is.EqualTo( 1 ) );
			}
		}

		[Test]
		public void MatchMouseDelta_ReturnsEmptyWhenThereIsNoMouseDelta() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Look",
					InputValueType.Vector2,
					InputTestHelpers.Delta2D( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.Delta )
				)
			);
			var state = new InputStateService();
			var matcher = new BindingMatcherService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			BindingMatchSet noDelta = matcher.MatchMouseDelta( graph, uint.MaxValue, InputScheme.KeyboardAndMouse );
			state.AddMouseDelta( new Vector2( 3.0f, -2.0f ) );
			BindingMatchSet withDelta = matcher.MatchMouseDelta( graph, uint.MaxValue, InputScheme.KeyboardAndMouse );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( noDelta.Length, Is.Zero );
				Assert.That( withDelta.Length, Is.EqualTo( 1 ) );
			}
		}
	}
}
