using NUnit.Framework;
using Nomad.Logger.Private;

namespace Nomad.Logger.Tests {
	[TestFixture]
	[Category("Nomad.Logger")]
	[Category("Unit")]
	public class SinkTypeTests {
		[Test]
		public void SinkType_ValuesRemainStable() {
			using ( Assert.EnterMultipleScope() ) {
				Assert.That( (int)SinkType.InGame, Is.EqualTo( 0 ) );
				Assert.That( (int)SinkType.Engine, Is.EqualTo( 1 ) );
				Assert.That( (int)SinkType.File, Is.EqualTo( 2 ) );
				Assert.That( (int)SinkType.Console, Is.EqualTo( 3 ) );
				Assert.That( (int)SinkType.Count, Is.EqualTo( 4 ) );
			}
		}
	}
}
