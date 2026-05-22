using System.Linq;
using NUnit.Framework;

namespace Nomad.Input.Tests {
	[TestFixture]
	[Category("Nomad.Input")]
	[Category("Fixtures")]
	[Category("Integration")]
	public class InputFileSystemFixtureTests {
		[Test]
		public void SetDirectory_RegistersEmptyDirectoriesWithoutFiles() {
			var fileSystem = new InputFileSystemFixture();

			fileSystem.SetDirectory( "Assets/Config/Bindings/Empty" );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( fileSystem.Object.DirectoryExists( "Assets/Config/Bindings/Empty" ), Is.True );
				Assert.That( fileSystem.Object.GetFiles( "Assets/Config/Bindings/Empty", "*.json", true ), Is.Empty );
			}
		}

		[Test]
		public void SetFile_RegistersParentDirectoriesInMemory() {
			var fileSystem = new InputFileSystemFixture();

			fileSystem.SetFile( "Assets/Config/Bindings/Nested/Gameplay.json", "{ \"Bindings\": [] }" );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( fileSystem.Object.DirectoryExists( "Assets/Config/Bindings" ), Is.True );
				Assert.That( fileSystem.Object.DirectoryExists( "Assets/Config/Bindings/Nested" ), Is.True );
				Assert.That( fileSystem.Object.FileExists( "Assets/Config/Bindings/Nested/Gameplay.json" ), Is.True );
			}
		}

		[Test]
		public void GetFiles_WhenRecursiveIsFalse_OnlyReturnsTopLevelFiles() {
			var fileSystem = new InputFileSystemFixture(
				( "Assets/Config/Bindings/DefaultBinds.json", "{ \"Bindings\": [] }" ),
				( "Assets/Config/Bindings/Nested/Gameplay.json", "{ \"Bindings\": [] }" )
			);

			var topLevelFiles = fileSystem.Object.GetFiles( "Assets/Config/Bindings", "*.json", false );
			var recursiveFiles = fileSystem.Object.GetFiles( "Assets/Config/Bindings", "*.json", true );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( topLevelFiles, Has.Count.EqualTo( 1 ) );
				Assert.That( topLevelFiles.Single(), Is.EqualTo( "Assets/Config/Bindings/DefaultBinds.json" ) );
				Assert.That( recursiveFiles, Has.Count.EqualTo( 2 ) );
			}
		}
	}
}
