using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Core.Util;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.Services;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;
using Nomad.Core.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Dispatch")]
	[Category("Unit")]
	public class ActionResolverServiceTests {
		[Test]
		public void ResolveMatchesNonAlloc_ResolvesButtonBindingsFromPressedState() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Jump",
					"player.jump",
					InputValueType.Button,
					InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space )
				)
			);
			var state = new InputStateService();
			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, true );
			var matcher = new BindingMatcherService( repository, state );
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			BindingMatchSet matches = matcher.MatchKeyboard( graph, new KeyboardEventArgs( KeyNum.Space, 25, true ), uint.MaxValue, InputScheme.KeyboardAndMouse );
			ReadOnlySpan<ResolvedAction> actions = resolver.ResolveMatchesNonAlloc( graph, matches, 25 );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( actions.Length, Is.EqualTo( 1 ) );
				Assert.That( actions[0].ActionId.ToString(), Is.EqualTo( "player.jump" ) );
				Assert.That( actions[0].Phase, Is.EqualTo( InputActionPhase.Started ) );
				Assert.That( actions[0].ButtonValue, Is.True );
			}
		}

		[Test]
		public void ResolveMatchesNonAlloc_ButtonUsesStartedOnceUntilReleased() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Jump",
					InputValueType.Button,
					InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space )
				)
			);
			var state = new InputStateService();
			var matcher = new BindingMatcherService( repository, state );
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, true );
			ReadOnlySpan<ResolvedAction> startedActions = resolver.ResolveMatchesNonAlloc(
				graph,
				matcher.MatchKeyboard( graph, new KeyboardEventArgs( KeyNum.Space, 25, true ), uint.MaxValue, InputScheme.KeyboardAndMouse ),
				25
			);

			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, true );
			ReadOnlySpan<ResolvedAction> performedActions = resolver.ResolveMatchesNonAlloc(
				graph,
				matcher.MatchKeyboard( graph, new KeyboardEventArgs( KeyNum.Space, 35, true ), uint.MaxValue, InputScheme.KeyboardAndMouse ),
				35
			);

			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, false );
			ReadOnlySpan<ResolvedAction> canceledActions = resolver.ResolveMatchesNonAlloc(
				graph,
				matcher.MatchKeyboard( graph, new KeyboardEventArgs( KeyNum.Space, 45, false ), uint.MaxValue, InputScheme.KeyboardAndMouse ),
				45
			);

			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, true );
			ReadOnlySpan<ResolvedAction> restartedActions = resolver.ResolveMatchesNonAlloc(
				graph,
				matcher.MatchKeyboard( graph, new KeyboardEventArgs( KeyNum.Space, 55, true ), uint.MaxValue, InputScheme.KeyboardAndMouse ),
				55
			);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( startedActions.Length, Is.EqualTo( 1 ) );
				Assert.That( startedActions[0].Phase, Is.EqualTo( InputActionPhase.Started ) );
				Assert.That( performedActions.Length, Is.EqualTo( 1 ) );
				Assert.That( performedActions[0].Phase, Is.EqualTo( InputActionPhase.Performed ) );
				Assert.That( canceledActions.Length, Is.EqualTo( 1 ) );
				Assert.That( canceledActions[0].Phase, Is.EqualTo( InputActionPhase.Canceled ) );
				Assert.That( restartedActions.Length, Is.EqualTo( 1 ) );
				Assert.That( restartedActions[0].Phase, Is.EqualTo( InputActionPhase.Started ) );
			}
		}

		[Test]
		public void ResolveMatchesNonAlloc_AppliesAxis1DProcessors() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Throttle",
					InputValueType.Float,
					InputTestHelpers.Axis1D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, deadzone: 0.2f, sensitivity: 2.0f, scale: 0.5f, invert: true )
				)
			);
			var state = new InputStateService();
			state.SetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.4f );
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			ReadOnlySpan<ResolvedAction> actions = resolver.ResolveMatchesNonAlloc(
				graph,
				CreateSingleMatchSet( CompiledBindingRepository.GetAxisCandidateIndices( graph, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger )[0], 10 ),
				50
			);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( actions.Length, Is.EqualTo( 1 ) );
				Assert.That( actions[0].FloatValue, Is.EqualTo( -0.4f ).Within( 0.0001f ) );
				Assert.That( actions[0].Phase, Is.EqualTo( InputActionPhase.Started ) );
			}
		}

		[Test]
		public void ResolveMatchesNonAlloc_Axis1DInsideDeadzoneDoesNotStartAction() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Throttle",
					InputValueType.Float,
					InputTestHelpers.Axis1D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, deadzone: 0.5f )
				)
			);
			var state = new InputStateService();
			state.SetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.25f );
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			ReadOnlySpan<ResolvedAction> actions = resolver.ResolveMatchesNonAlloc(
				graph,
				CreateSingleMatchSet( CompiledBindingRepository.GetAxisCandidateIndices( graph, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger )[0], 10 ),
				50
			);

			Assert.That( actions.Length, Is.Zero );
		}

		[Test]
		public void ResolveMatchesNonAlloc_Axis1DUsesStartedThenPerformedAcrossActiveFrames() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Throttle",
					InputValueType.Float,
					InputTestHelpers.Axis1D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger )
				)
			);
			var state = new InputStateService();
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;
			BindingMatchSet matches = CreateSingleMatchSet( CompiledBindingRepository.GetAxisCandidateIndices( graph, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger )[0], 10 );

			state.SetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.4f );
			ReadOnlySpan<ResolvedAction> startedActions = resolver.ResolveMatchesNonAlloc( graph, matches, 50 );

			state.SetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.6f );
			ReadOnlySpan<ResolvedAction> performedActions = resolver.ResolveMatchesNonAlloc( graph, matches, 60 );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( startedActions.Length, Is.EqualTo( 1 ) );
				Assert.That( startedActions[0].Phase, Is.EqualTo( InputActionPhase.Started ) );
				Assert.That( performedActions.Length, Is.EqualTo( 1 ) );
				Assert.That( performedActions[0].Phase, Is.EqualTo( InputActionPhase.Performed ) );
			}
		}

		[Test]
		public void ResolveMatchesNonAlloc_Axis1DCanStartAgainAfterReturningToZero() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Throttle",
					InputValueType.Float,
					InputTestHelpers.Axis1D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger )
				)
			);
			var state = new InputStateService();
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;
			BindingMatchSet matches = CreateSingleMatchSet( CompiledBindingRepository.GetAxisCandidateIndices( graph, InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger )[0], 10 );

			state.SetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.4f );
			ReadOnlySpan<ResolvedAction> startedActions = resolver.ResolveMatchesNonAlloc( graph, matches, 50 );

			state.SetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.0f );
			ReadOnlySpan<ResolvedAction> canceledActions = resolver.ResolveMatchesNonAlloc( graph, matches, 60 );

			state.SetAxis1D( InputDeviceSlot.Gamepad0, InputControlId.LeftTrigger, 0.3f );
			ReadOnlySpan<ResolvedAction> restartedActions = resolver.ResolveMatchesNonAlloc( graph, matches, 70 );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( startedActions.Length, Is.EqualTo( 1 ) );
				Assert.That( startedActions[0].Phase, Is.EqualTo( InputActionPhase.Started ) );
				Assert.That( canceledActions.Length, Is.EqualTo( 1 ) );
				Assert.That( canceledActions[0].Phase, Is.EqualTo( InputActionPhase.Canceled ) );
				Assert.That( restartedActions.Length, Is.EqualTo( 1 ) );
				Assert.That( restartedActions[0].Phase, Is.EqualTo( InputActionPhase.Started ) );
			}
		}

		[Test]
		public void ResolveMatchesNonAlloc_AppliesAxis2DProcessors() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"LookStick",
					InputValueType.Vector2,
					InputTestHelpers.Axis2D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.RightStick, deadzone: 0.1f, sensitivity: 2.0f, scaleX: 2.0f, scaleY: 0.5f, invertX: true )
				)
			);
			var state = new InputStateService();
			state.SetAxis2D( InputDeviceSlot.Gamepad0, InputControlId.RightStick, new Vector2( 0.25f, -0.5f ) );
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			ReadOnlySpan<ResolvedAction> actions = resolver.ResolveMatchesNonAlloc(
				graph,
				CreateSingleMatchSet( CompiledBindingRepository.GetAxisCandidateIndices( graph, InputDeviceSlot.Gamepad0, InputControlId.RightStick )[0], 10 ),
				50
			);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( actions.Length, Is.EqualTo( 1 ) );
				Assert.That( actions[0].Vector2Value.X, Is.EqualTo( -1.0f ).Within( 0.0001f ) );
				Assert.That( actions[0].Vector2Value.Y, Is.EqualTo( -0.5f ).Within( 0.0001f ) );
			}
		}

		[Test]
		public void ResolveMatchesNonAlloc_Axis2DInsideDeadzoneDoesNotStartAction() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"LookStick",
					InputValueType.Vector2,
					InputTestHelpers.Axis2D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.RightStick, deadzone: 0.5f )
				)
			);
			var state = new InputStateService();
			state.SetAxis2D( InputDeviceSlot.Gamepad0, InputControlId.RightStick, new Vector2( 0.2f, 0.2f ) );
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			ReadOnlySpan<ResolvedAction> actions = resolver.ResolveMatchesNonAlloc(
				graph,
				CreateSingleMatchSet( CompiledBindingRepository.GetAxisCandidateIndices( graph, InputDeviceSlot.Gamepad0, InputControlId.RightStick )[0], 10 ),
				50
			);

			Assert.That( actions.Length, Is.Zero );
		}

		[Test]
		public void ResolveMatchesNonAlloc_AppliesMouseDeltaProcessors() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Look",
					InputValueType.Vector2,
					InputTestHelpers.Delta2D( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.Delta, sensitivity: 2.0f, scaleX: 0.5f, scaleY: 1.5f, invertY: true )
				)
			);
			var state = new InputStateService();
			state.AddMouseDelta( new Vector2( 4.0f, 2.0f ) );
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			ReadOnlySpan<ResolvedAction> actions = resolver.ResolveMatchesNonAlloc(
				graph,
				CreateSingleMatchSet( CompiledBindingRepository.GetDeltaCandidateIndices( graph, InputDeviceSlot.Mouse, InputControlId.Delta )[0], 10 ),
				75
			);

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( actions.Length, Is.EqualTo( 1 ) );
				Assert.That( actions[0].Vector2Value.X, Is.EqualTo( 4.0f ).Within( 0.0001f ) );
				Assert.That( actions[0].Vector2Value.Y, Is.EqualTo( -6.0f ).Within( 0.0001f ) );
			}
		}

		[Test]
		public void ResolveMatchesNonAlloc_PrefersHigherScoreThenHigherMagnitudePerAction() {
			var state = new InputStateService();
			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.Space, true );
			var resolver = new ActionResolverService( new CompiledBindingRepository(), state );
			ButtonBinding button = new( InputDeviceSlot.Keyboard, InputControlId.Space );

			var lowerScoreBinding = new CompiledBinding(
				actionName: new InternString( "Jump" ),
				actionId: new InternString( "Jump" ),
				actionIndex: 0,
				valueType: InputValueType.Button,
				kind: InputBindingKind.Button,
				scheme: InputScheme.KeyboardAndMouse,
				priority: 0,
				scoreBase: 10,
				consumesInput: false,
				contextMask: uint.MaxValue,
				modifierMask0: button.ModifierMask0,
				modifierMask1: button.ModifierMask1,
				modifierMask2: button.ModifierMask2,
				modifierMask3: button.ModifierMask3,
				button: button,
				axis1D: default,
				axis1DComposite: default,
				axis2D: default,
				axis2DComposite: default,
				delta2D: default
			);
			var higherScoreBinding = new CompiledBinding(
				actionName: new InternString( "Jump" ),
				actionId: new InternString( "Jump" ),
				actionIndex: 0,
				valueType: InputValueType.Button,
				kind: InputBindingKind.Button,
				scheme: InputScheme.KeyboardAndMouse,
				priority: 0,
				scoreBase: 100,
				consumesInput: false,
				contextMask: uint.MaxValue,
				modifierMask0: button.ModifierMask0,
				modifierMask1: button.ModifierMask1,
				modifierMask2: button.ModifierMask2,
				modifierMask3: button.ModifierMask3,
				button: button,
				axis1D: default,
				axis1DComposite: default,
				axis2D: default,
				axis2DComposite: default,
				delta2D: default
			);
			var graph = new CompiledBindingGraph(
				new[] { lowerScoreBinding, higherScoreBinding },
				new[] { new CompiledActionInfo( new InternString( "Jump" ) ) },
				Array.Empty<Bucket>(),
				Array.Empty<int>(),
				Array.Empty<Bucket>(),
				Array.Empty<int>(),
				Array.Empty<Bucket>(),
				Array.Empty<int>(),
				Array.Empty<int>(),
				Array.Empty<int>()
			);

			ReadOnlySpan<ResolvedAction> actions = resolver.ResolveMatchesNonAlloc(
				graph,
				new BindingMatchSet( new[] { 0, 1 }, new[] { 10, 100 } ),
				90
			);

			Assert.That( actions.Length, Is.EqualTo( 1 ) );
		}

		[Test]
		public void ResolveKeyboardCompositesNonAlloc_ResolvesCompositeBindingsFromPressedKeys() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Throttle",
					InputValueType.Float,
					InputTestHelpers.Composite1D( InputScheme.KeyboardAndMouse, InputControlId.S, InputControlId.W )
				),
				InputTestHelpers.Action(
					"Move",
					InputValueType.Vector2,
					InputTestHelpers.Composite2D( InputScheme.KeyboardAndMouse, InputControlId.W, InputControlId.S, InputControlId.A, InputControlId.D )
				)
			);
			var state = new InputStateService();
			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.W, true );
			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.D, true );
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			ReadOnlySpan<ResolvedAction> actions = resolver.ResolveKeyboardCompositesNonAlloc( graph, uint.MaxValue, InputScheme.KeyboardAndMouse, 120 );
			ResolvedAction[] actionArray = actions.ToArray();

			Assert.That( actionArray.Length, Is.EqualTo( 2 ) );
			Assert.That( actionArray.Any( action => action.ActionId.ToString() == "Throttle" && action.FloatValue > 0.0f ), Is.True );
			Assert.That( actionArray.Any( action => action.ActionId.ToString() == "Move" && action.Vector2Value != Vector2.Zero ), Is.True );
			Assert.That( actionArray.Any( action => action.ActionId.ToString() == "Throttle" && action.Phase == InputActionPhase.Started ), Is.True );
			Assert.That( actionArray.Any( action => action.ActionId.ToString() == "Move" && action.Phase == InputActionPhase.Started ), Is.True );
		}

		[Test]
		public void ResolveKeyboardCompositesNonAlloc_CancelsCompositeWhenKeysAreReleased() {
			var repository = InputTestHelpers.CompileToRepository(
				InputTestHelpers.Action(
					"Move",
					InputValueType.Vector2,
					InputTestHelpers.Composite2D( InputScheme.KeyboardAndMouse, InputControlId.W, InputControlId.S, InputControlId.A, InputControlId.D )
				)
			);
			var state = new InputStateService();
			var resolver = new ActionResolverService( repository, state );
			CompiledBindingGraph graph = repository.Current;

			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.W, true );
			ReadOnlySpan<ResolvedAction> started = resolver.ResolveKeyboardCompositesNonAlloc( graph, uint.MaxValue, InputScheme.KeyboardAndMouse, 10 );

			state.SetPressed( InputDeviceSlot.Keyboard, InputControlId.W, false );
			ReadOnlySpan<ResolvedAction> canceled = resolver.ResolveKeyboardCompositesNonAlloc( graph, uint.MaxValue, InputScheme.KeyboardAndMouse, 20 );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( started.Length, Is.EqualTo( 1 ) );
				Assert.That( started[0].Phase, Is.EqualTo( InputActionPhase.Started ) );
				Assert.That( canceled.Length, Is.EqualTo( 1 ) );
				Assert.That( canceled[0].Phase, Is.EqualTo( InputActionPhase.Canceled ) );
				Assert.That( canceled[0].Vector2Value, Is.EqualTo( Vector2.Zero ) );
			}
		}

		private static BindingMatchSet CreateSingleMatchSet( int bindingIndex, int score ) {
			return new BindingMatchSet( new[] { bindingIndex }, new[] { score } );
		}
	}
}
