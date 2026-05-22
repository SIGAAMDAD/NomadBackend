using System;
using System.Collections.Immutable;
using NUnit.Framework;
using Nomad.Core.Input;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.Services;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Bindings")]
	[Category("Unit")]
	public class BindingCompilerServiceTests {
		[Test]
		public void Compile_IndexesButtonBindingsForBothPressedStates() {
			var actions = new[] {
				InputTestHelpers.Action(
					"Jump",
					InputValueType.Button,
					InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space )
				)
			};

			var graph = BindingCompilerService.Compile( actions.ToImmutableArray() );
			ReadOnlySpan<int> pressedCandidates = CompiledBindingRepository.GetButtonCandidateIndices( graph, InputDeviceSlot.Keyboard, InputControlId.Space, true );
			ReadOnlySpan<int> releasedCandidates = CompiledBindingRepository.GetButtonCandidateIndices( graph, InputDeviceSlot.Keyboard, InputControlId.Space, false );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( pressedCandidates.Length, Is.EqualTo( 1 ) );
				Assert.That( releasedCandidates.Length, Is.EqualTo( 1 ) );
				Assert.That( graph.Bindings[pressedCandidates[0]].ActionId.ToString(), Is.EqualTo( "Jump" ) );
				Assert.That( graph.Bindings[releasedCandidates[0]].ActionId.ToString(), Is.EqualTo( "Jump" ) );
			}
		}

		[Test]
		public void Compile_IndexesAxisAndDeltaBindingsIntoSeparateLookups() {
			var actions = new[] {
				InputTestHelpers.Action(
					"Aim",
					InputValueType.Vector2,
					InputTestHelpers.Axis2D( InputScheme.Gamepad, InputDeviceSlot.Gamepad0, InputControlId.LeftStick )
				),
				InputTestHelpers.Action(
					"Look",
					InputValueType.Vector2,
					InputTestHelpers.Delta2D( InputScheme.KeyboardAndMouse, InputDeviceSlot.Mouse, InputControlId.Delta )
				)
			};

			var graph = BindingCompilerService.Compile( actions.ToImmutableArray() );
			ReadOnlySpan<int> axisCandidates = CompiledBindingRepository.GetAxisCandidateIndices( graph, InputDeviceSlot.Gamepad0, InputControlId.LeftStick );
			ReadOnlySpan<int> deltaCandidates = CompiledBindingRepository.GetDeltaCandidateIndices( graph, InputDeviceSlot.Mouse, InputControlId.Delta );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( axisCandidates.Length, Is.EqualTo( 1 ) );
				Assert.That( deltaCandidates.Length, Is.EqualTo( 1 ) );
				Assert.That( graph.Bindings[axisCandidates[0]].ActionId.ToString(), Is.EqualTo( "Aim" ) );
				Assert.That( graph.Bindings[deltaCandidates[0]].ActionId.ToString(), Is.EqualTo( "Look" ) );
			}
		}

		[Test]
		public void Compile_PlacesCompositeBindingsIntoCompositeCollections() {
			var actions = new[] {
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
			};

			var graph = BindingCompilerService.Compile( actions.ToImmutableArray() );
			ReadOnlySpan<int> composite1D = CompiledBindingRepository.GetComposite1DBindingIndices( graph );
			ReadOnlySpan<int> composite2D = CompiledBindingRepository.GetComposite2DBindingIndices( graph );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( composite1D.Length, Is.EqualTo( 1 ) );
				Assert.That( composite2D.Length, Is.EqualTo( 1 ) );
				Assert.That( graph.Bindings[composite1D[0]].ActionId.ToString(), Is.EqualTo( "Throttle" ) );
				Assert.That( graph.Bindings[composite2D[0]].ActionId.ToString(), Is.EqualTo( "Move" ) );
			}
		}

		[Test]
		public void CompileIntoRepository_ReplacesTheCurrentCompiledGraph() {
			var repository = new CompiledBindingRepository();
			var compiler = new BindingCompilerService( repository );
			var actions = new[] {
				InputTestHelpers.Action(
					"Jump",
					InputValueType.Button,
					InputTestHelpers.Button( InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space )
				)
			};

			compiler.CompileIntoRepository( actions.ToImmutableArray() );

			Assert.That( CompiledBindingRepository.GetButtonCandidateIndices( repository.Current, InputDeviceSlot.Keyboard, InputControlId.Space, true ).Length, Is.EqualTo( 1 ) );
		}
	}
}
