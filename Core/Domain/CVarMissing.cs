using System;

namespace NomadCore.Domain.Exceptions {
	public sealed class CVarMissing( string name ) : Exception( $"Missing CVar '{name}'" ) {
	};
};