using NUnit.Framework;
using Nomad.Input.Private.Repositories;
using Nomad.Input.Private.Services;
using Nomad.Input.Private.ValueObjects;
using Nomad.Input.ValueObjects;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Repositories")]
	[Category("Unit")]
	public class CompiledBindingRepositoryTests {
		[Test]
		public void Current_DefaultsToEmptyGraphAndReplaceSwapsCurrentGraph() {
			var repository = new CompiledBindingRepository();
			var graph = BindingCompilerService.Compile(
				System.Collections.Immutable.ImmutableArray.Create(
					InputTestHelpers.Action(
						"Jump",
						InputValueType.Button,
						InputTestHelpers.Button( Nomad.Core.Input.InputScheme.KeyboardAndMouse, InputDeviceSlot.Keyboard, InputControlId.Space ) ) ) );

			repository.Replace( graph );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( CompiledBindingGraph.Empty.Bindings, Is.Empty );
				Assert.That( repository.Current, Is.SameAs( graph ) );
				Assert.That( CompiledBindingRepository.GetButtonCandidateIndices( repository.Current, InputDeviceSlot.Keyboard, InputControlId.Space, true ).Length, Is.EqualTo( 1 ) );
				Assert.That( CompiledBindingRepository.GetAxisCandidateIndices( repository.Current, InputDeviceSlot.Keyboard, InputControlId.Space ).Length, Is.Zero );
				Assert.That( CompiledBindingRepository.GetDeltaCandidateIndices( repository.Current, InputDeviceSlot.Mouse, InputControlId.Delta ).Length, Is.Zero );
			}
		}
	}
}
