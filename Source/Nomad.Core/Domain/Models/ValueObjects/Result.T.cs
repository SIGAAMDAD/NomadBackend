/*
===========================================================================
The Nomad AGPL Source Code
Copyright (C) 2025 Noah Van Til

The Nomad Source Code is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published
by the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

The Nomad Source Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with The Nomad Source Code.  If not, see <http://www.gnu.org/licenses/>.

If you have questions concerning this license or the applicable additional
terms, you may contact me via email at nyvantil@gmail.com.
===========================================================================
*/

using NomadCore.Domain.Models.Interfaces;

namespace NomadCore.Domain.Models.ValueObjects {
	/*
	===================================================================================
	
	Result<T>
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="T"></typeparam>
	
	public record Result<T> {
		public bool IsSuccess { get; init; }
		public bool IsFailure => !IsSuccess || Value == null;
		public T? Value { get; init; }
		public IError? Error { get; init; }

		protected Result( T value ) {
			IsSuccess = true;
			Value = value;
			Error = null;
		}
		protected Result( IError error ) {
			IsSuccess = false;
			Value = default;
			Error = error;
		}
		
		public static Result<T> Success( T value ) {
			return new Result<T>( value );
		}
		public static Result<T> Failure( IError error ) {
			return new Result<T>( error );
		}

		/*
		===============
		Deconstruct
		===============
		*/
		public void Deconstruct( out bool isSuccess, out T? value, out IError? error ) {
			isSuccess = IsSuccess;
			value = Value;
			error = Error;
		}
	};
};