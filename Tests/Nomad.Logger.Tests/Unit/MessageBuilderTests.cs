using System;
using NUnit.Framework;
using Nomad.Core.Logger;
using Nomad.Logger.Private;

namespace Nomad.Logger.Tests {
	[TestFixture]
	[Category("Nomad.Logger")]
	[Category("Unit")]
	public class MessageBuilderTests {
		[TestCase(LogLevel.Info, "", "")]
		[TestCase(LogLevel.Warning, "[color=gold]", "[/color]")]
		[TestCase(LogLevel.Error, "[color=red]", "[/color]")]
		[TestCase(LogLevel.Debug, "[color=light_blue]", "[/color]")]
		public void FormatMessage_AddsCategoryTimestampMessageAndColorTags( LogLevel level, string prefix, string suffix ) {
			var builder = new MessageBuilder();
			var category = new LoggerCategory( "Gameplay", LogLevel.Debug, true, builder );

			string formatted = builder.FormatMessage( category, level, "hello", true );

			using ( Assert.EnterMultipleScope() ) {
				Assert.That( formatted, Does.StartWith( prefix + "[" ) );
				Assert.That( formatted, Does.Contain( "] [Gameplay] hello" ) );
				Assert.That( formatted, Does.EndWith( suffix ) );
			}
		}

		[TestCase((LogLevel)255)]
		[TestCase(LogLevel.Count)]
		public void FormatMessage_WhenLevelIsUnsupported_Throws( LogLevel level ) {
			var builder = new MessageBuilder();
			var category = new LoggerCategory( "Gameplay", LogLevel.Debug, true, builder );

			Assert.Throws<ArgumentOutOfRangeException>( () => builder.FormatMessage( category, level, "hello", true ) );
		}
	}
}
