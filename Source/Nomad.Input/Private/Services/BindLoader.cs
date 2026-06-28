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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using Nomad.Core.FileSystem;
using Nomad.Core.Input;
using Nomad.Core.Input.ValueObjects;
using Nomad.Core.Memory.Buffers;
using Nomad.Core.Util;
using Nomad.Input.Private.Extensions;
using Nomad.Input.ValueObjects;
using Nomad.Core.Logger;
using Nomad.Core.Compatibility.Guards;

namespace Nomad.Input.Private.Services
{
	/*
	===================================================================================
	
	BindLoader
	
	===================================================================================
	*/
	/// <summary>
	/// 
	/// </summary>

	internal sealed class BindLoader
	{
		private readonly IFileSystem _fileSystem;
		private readonly ILoggerCategory _category;

		/*
		===============
		BindLoader
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="fileSystem"></param>
		/// <param name="logger"></param>
		public BindLoader( IFileSystem fileSystem, ILoggerService logger )
		{
			ArgumentGuard.ThrowIfNull( logger, nameof( logger ) );

			_fileSystem = fileSystem ?? throw new ArgumentNullException( nameof( fileSystem ) );
			_category = logger.CreateCategory( nameof( BindLoader ), LogLevel.Info, true );

			_fileSystem.AddSearchDirectory( Constants.BINDINGS_DIRECTORY );
		}

		/*
		===============
		LoadBindDatabase
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="binds"></param>
		/// <returns></returns>
		public bool LoadBindDatabase( string filePath, out ImmutableArray<InputActionDefinition> binds )
		{
			IBufferHandle? fileBuffer = _fileSystem.LoadFile( filePath );
			if ( fileBuffer == null ) {
				binds = ImmutableArray<InputActionDefinition>.Empty;
				_category.PrintLine( $"Couldn't load bind file '{filePath}'!" );
				return false;
			}

			try {
				using var stream = fileBuffer.AsStream( 0, fileBuffer.Length );
				using var document = JsonLoader.Parse( stream );

				if ( !JsonLoader.TryGetProperty( document.RootElement, "Bindings", out var bindingsElement ) ) {
					throw new Exception( $"Bindings file '{filePath}' is missing a 'Bindings' property." );
				}

				Dictionary<string, uint> contextSchema = ParseContextSchema( document.RootElement );
				var actionIndices = new Dictionary<string, int>( StringComparer.Ordinal );
				var actions = new List<ActionBuilder>();

				switch ( bindingsElement.ValueKind ) {
					case JsonValueKind.Array:
						foreach ( var actionElement in bindingsElement.EnumerateArray() ) {
							ParseActionDefinition( actionElement, actionIndices, actions, contextSchema );
						}
						break;
					case JsonValueKind.Object:
						foreach ( var actionProperty in bindingsElement.EnumerateObject() ) {
							ParseActionDefinition( actionProperty.Value, actionIndices, actions, contextSchema, actionProperty.Name );
						}
						break;
					default:
						throw new Exception( $"Bindings file '{filePath}' has an invalid 'Bindings' payload. Expected an array or object." );
				}

				var builder = ImmutableArray.CreateBuilder<InputActionDefinition>( actions.Count );
				for ( int i = 0; i < actions.Count; i++ ) {
					builder.Add( actions[i].Build() );
				}
				binds = builder.ToImmutable();
				return true;
			} finally {
				fileBuffer.DisposeAsync().AsTask().GetAwaiter().GetResult();
			}
		}

		/*
		===============
		ParseActionDefinition
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="actionElement"></param>
		/// <param name="actionIndices"></param>
		/// <param name="actions"></param>
		/// <param name="fallbackName"></param>
		/// <exception cref="Exception"></exception>
		private static void ParseActionDefinition(
			JsonElement actionElement,
			Dictionary<string, int> actionIndices,
			List<ActionBuilder> actions,
			IReadOnlyDictionary<string, uint> contextSchema,
			string? fallbackName = null
		)
		{
			string name = GetRequiredString( actionElement, "Name", fallbackName );
			string id = GetRequiredString( actionElement, "Id", fallbackName );
			InputValueType valueType = JsonLoader.GetRequired<InputValueType>( actionElement, "ValueType" );
			InputScheme scheme = JsonLoader.GetRequired<InputScheme>( actionElement, "Scheme" );
			uint actionContextMask = ParseContextMask( actionElement, contextSchema, uint.MaxValue );

			if ( !JsonLoader.TryGetProperty( actionElement, "Bindings", out JsonElement bindingsElement ) && !JsonLoader.TryGetProperty( actionElement, "Binding", out bindingsElement ) ) {
				throw new Exception( $"Binding action '{id}' is missing a binding payload." );
			}

			if ( !actionIndices.TryGetValue( id, out int actionIndex ) ) {
				actionIndex = actions.Count;
				actionIndices.Add( id, actionIndex );
				actions.Add( new ActionBuilder( name, id, valueType ) );
			} else if ( actions[actionIndex].ValueType != valueType ) {
				throw new Exception( $"Binding action '{id}' has conflicting value types." );
			}

			if ( bindingsElement.ValueKind == JsonValueKind.Array ) {
				foreach ( JsonElement bindingElement in bindingsElement.EnumerateArray() ) {
					actions[actionIndex].Bindings.Add( ParseBindingDefinition( bindingElement, scheme, contextSchema, actionContextMask ) );
				}
				return;
			}

			if ( bindingsElement.ValueKind != JsonValueKind.Object ) {
				throw new Exception( $"Binding action '{id}' has an invalid binding payload." );
			}

			actions[actionIndex].Bindings.Add( ParseBindingDefinition( bindingsElement, scheme, contextSchema, actionContextMask ) );
		}

		/*
		===============
		ParseBindingDefinition
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindingElement"></param>
		/// <param name="scheme"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private static InputBindingDefinition ParseBindingDefinition(
			JsonElement bindingElement,
			InputScheme scheme,
			IReadOnlyDictionary<string, uint> contextSchema,
			uint actionContextMask
		)
		{
			InputBindingKind kind = JsonLoader.TryGetProperty( bindingElement, "Kind", out JsonElement kindElement )
				? JsonLoader.Read<InputBindingKind>( kindElement, "Kind" )
				: InferBindingKind( bindingElement );

			InputBindingDefinition definition = new InputBindingDefinition {
				Scheme = scheme,
				Kind = kind,
				Priority = JsonLoader.GetOptional<int>( bindingElement, "Priority", 0 ),
				ConsumesInput = JsonLoader.GetOptional<bool>( bindingElement, "ConsumesInput", false ),
				ContextMask = ParseContextMask( bindingElement, contextSchema, actionContextMask )
			};

			switch ( kind ) {
				case InputBindingKind.Button:
					definition.Button = ParseButtonBinding( bindingElement );
					break;
				case InputBindingKind.Axis1D:
					definition.Axis1D = ParseAxis1DBinding( bindingElement );
					break;
				case InputBindingKind.Axis1DComposite:
					definition.Axis1DComposite = ParseAxis1DCompositeBinding( bindingElement );
					break;
				case InputBindingKind.Axis2D:
					definition.Axis2D = ParseAxis2DBinding( bindingElement );
					break;
				case InputBindingKind.Axis2DComposite:
					definition.Axis2DComposite = ParseAxis2DCompositeBinding( bindingElement );
					break;
				case InputBindingKind.Delta2D:
					definition.Delta2D = ParseDelta2DBinding( bindingElement );
					break;
				default:
					throw new Exception( $"Unsupported binding kind '{kind}'." );
			}

			return definition;
		}

		private static Dictionary<string, uint> ParseContextSchema( JsonElement rootElement )
		{
			var schema = new Dictionary<string, uint>( StringComparer.OrdinalIgnoreCase );

			if ( JsonLoader.TryGetProperty( rootElement, "ContextSchema", out JsonElement schemaElement ) ) {
				ParseContextSchemaElement( schemaElement, schema );
			}
			if ( JsonLoader.TryGetProperty( rootElement, "Contexts", out JsonElement contextsElement ) ) {
				ParseContextSchemaElement( contextsElement, schema );
			}

			return schema;
		}

		private static void ParseContextSchemaElement( JsonElement schemaElement, Dictionary<string, uint> schema )
		{
			if ( schemaElement.ValueKind != JsonValueKind.Object ) {
				throw new Exception( "Input context schema must be an object." );
			}

			foreach ( JsonProperty property in schemaElement.EnumerateObject() ) {
				schema[property.Name] = ParseContextSchemaValue( property.Value );
			}
		}

		private static uint ParseContextSchemaValue( JsonElement valueElement )
		{
			if ( valueElement.ValueKind == JsonValueKind.Number ) {
				int bitIndex = valueElement.GetInt32();
				if ( bitIndex < 0 || bitIndex > 31 ) {
					throw new Exception( "Input context bit index must be between 0 and 31." );
				}
				return 1u << bitIndex;
			}

			if ( valueElement.ValueKind == JsonValueKind.String ) {
				string value = valueElement.GetString()!;
				if ( value.StartsWith( "0x", StringComparison.OrdinalIgnoreCase ) ) {
					return Convert.ToUInt32( value[2..], 16 );
				}
				if ( uint.TryParse( value, out uint mask ) ) {
					return mask;
				}
			}

			throw new Exception( "Input context schema values must be bit indices or numeric masks." );
		}

		private static uint ParseContextMask( JsonElement element, IReadOnlyDictionary<string, uint> contextSchema, uint fallbackMask )
		{
			uint mask = fallbackMask;

			if ( JsonLoader.TryGetProperty( element, "ContextMask", out JsonElement maskElement ) ) {
				mask = ParseContextMaskValue( maskElement, contextSchema );
			}
			if ( JsonLoader.TryGetProperty( element, "Context", out JsonElement contextElement ) ) {
				mask = ParseContextMaskValue( contextElement, contextSchema );
			}
			if ( JsonLoader.TryGetProperty( element, "Contexts", out JsonElement contextsElement ) ) {
				mask = ParseContextMaskValue( contextsElement, contextSchema );
			}

			return mask;
		}

		private static uint ParseContextMaskValue( JsonElement element, IReadOnlyDictionary<string, uint> contextSchema )
		{
			switch ( element.ValueKind ) {
				case JsonValueKind.Number:
					return element.GetUInt32();
				case JsonValueKind.String:
					return ResolveContextToken( element.GetString()!, contextSchema );
				case JsonValueKind.Array: {
						uint mask = 0;
						foreach ( JsonElement item in element.EnumerateArray() ) {
							mask |= ParseContextMaskValue( item, contextSchema );
						}
						return mask;
					}
				default:
					throw new Exception( "Input context masks must be numbers, strings, or arrays." );
			}
		}

		private static uint ResolveContextToken( string token, IReadOnlyDictionary<string, uint> contextSchema )
		{
			if ( contextSchema.TryGetValue( token, out uint mask ) ) {
				return mask;
			}
			if ( token.StartsWith( "0x", StringComparison.OrdinalIgnoreCase ) ) {
				return Convert.ToUInt32( token[2..], 16 );
			}
			if ( uint.TryParse( token, out mask ) ) {
				return mask;
			}
			throw new Exception( $"Unknown input context '{token}'." );
		}

		/*
		===============
		InferBindingKind
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindingElement"></param>
		/// <returns></returns>
		private static InputBindingKind InferBindingKind( JsonElement bindingElement )
		{
			string deviceId = GetRequiredString( bindingElement, "DeviceId" );

			if ( deviceId.Equals( Constants.MOUSE_MOTION_DEVICE_ID, StringComparison.OrdinalIgnoreCase ) ) {
				return InputBindingKind.Delta2D;
			}
			return InputBindingKind.Button;
		}

		/*
		===============
		ParseButtonBinding
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindingElement"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		private static ButtonBinding ParseButtonBinding( JsonElement bindingElement )
		{
			InputDeviceSlot deviceId = ParseDeviceSlot( GetRequiredString( bindingElement, "DeviceId" ) );
			string controlName = GetRequiredString( bindingElement, "ControlId", GetOptionalString( bindingElement, "ButtonId" ) );

			ImmutableArray<InputControlId>.Builder modifiers = ImmutableArray.CreateBuilder<InputControlId>();
			if ( JsonLoader.TryGetProperty( bindingElement, "Modifiers", out JsonElement modifiersElement ) ) {
				if ( modifiersElement.ValueKind != JsonValueKind.Array ) {
					throw new Exception( "Binding modifiers must be an array." );
				}
				foreach ( JsonElement modifierElement in modifiersElement.EnumerateArray() ) {
					if ( modifierElement.ValueKind != JsonValueKind.String ) {
						throw new Exception( "Binding modifiers must be strings." );
					}
					modifiers.Add( ParseKeyboardControl( modifierElement.GetString()! ) );
				}
			}

			return new ButtonBinding(
				deviceId: deviceId,
				controlId: ParseButtonControl( deviceId, controlName ),
				modifiers: modifiers.ToImmutable()
			);
		}

		/*
		===============
		ParseAxis1DBinding
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindingElement"></param>
		/// <returns></returns>
		private static Axis1DBinding ParseAxis1DBinding( JsonElement bindingElement )
		{
			InputDeviceSlot deviceId = ParseDeviceSlot( GetRequiredString( bindingElement, "DeviceId" ) );
			return new Axis1DBinding(
				deviceId: deviceId,
				controlId: ParseAnalogControl( deviceId, GetRequiredString( bindingElement, "ControlId" ) ),
				deadzone: JsonLoader.GetOptional<float>( bindingElement, "Deadzone", 0.0f ),
				sensitivity: JsonLoader.GetOptional<float>( bindingElement, "Sensitivity", 1.0f ),
				scale: JsonLoader.GetOptional<float>( bindingElement, "Scale", 1.0f ),
				invert: JsonLoader.GetOptional<bool>( bindingElement, "Invert", false ),
				responseCurve: JsonLoader.GetOptional<ResponseCurve>( bindingElement, "ResponseCurve", ResponseCurve.Linear )
			);
		}

		/*
		===============
		ParseAxis1DCompositeBinding
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindingElement"></param>
		/// <returns></returns>
		private static Axis1DCompositeBinding ParseAxis1DCompositeBinding( JsonElement bindingElement )
		{
			return new Axis1DCompositeBinding(
				negative: ParseKeyboardControl( GetRequiredString( bindingElement, "Negative" ) ),
				positive: ParseKeyboardControl( GetRequiredString( bindingElement, "Positive" ) ),
				scale: JsonLoader.GetOptional<float>( bindingElement, "Scale", 1.0f ),
				normalize: JsonLoader.GetOptional<bool>( bindingElement, "Normalize", true )
			);
		}

		/*
		===============
		ParseAxis2DBinding
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindingElement"></param>
		/// <returns></returns>
		private static Axis2DBinding ParseAxis2DBinding( JsonElement bindingElement )
		{
			InputDeviceSlot deviceId = ParseDeviceSlot( GetRequiredString( bindingElement, "DeviceId" ) );
			return new Axis2DBinding(
				deviceId: deviceId,
				controlId: ParseAnalogControl( deviceId, GetRequiredString( bindingElement, "ControlId" ) ),
				deadzone: JsonLoader.GetOptional<float>( bindingElement, "Deadzone", 0.0f ),
				sensitivity: JsonLoader.GetOptional<float>( bindingElement, "Sensitivity", 1.0f ),
				scaleX: JsonLoader.GetOptional<float>( bindingElement, "ScaleX", 1.0f ),
				scaleY: JsonLoader.GetOptional<float>( bindingElement, "ScaleY", 1.0f ),
				invertX: JsonLoader.GetOptional<bool>( bindingElement, "InvertX", false ),
				invertY: JsonLoader.GetOptional<bool>( bindingElement, "InvertY", false ),
				responseCurve: JsonLoader.GetOptional<ResponseCurve>( bindingElement, "ResponseCurve", ResponseCurve.Linear )
			);
		}

		/*
		===============
		ParseAxis2DCompositeBinding
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindingElement"></param>
		/// <returns></returns>
		private static Axis2DCompositeBinding ParseAxis2DCompositeBinding( JsonElement bindingElement )
		{
			return new Axis2DCompositeBinding(
				up: ParseKeyboardControl( GetRequiredString( bindingElement, "Up" ) ),
				down: ParseKeyboardControl( GetRequiredString( bindingElement, "Down" ) ),
				left: ParseKeyboardControl( GetRequiredString( bindingElement, "Left" ) ),
				right: ParseKeyboardControl( GetRequiredString( bindingElement, "Right" ) ),
				normalize: JsonLoader.GetOptional<bool>( bindingElement, "Normalize", true ),
				scaleX: JsonLoader.GetOptional<float>( bindingElement, "ScaleX", 1.0f ),
				scaleY: JsonLoader.GetOptional<float>( bindingElement, "ScaleY", 1.0f )
			);
		}

		/*
		===============
		ParseDelta2DBinding
		===============
		*/
		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindingElement"></param>
		/// <returns></returns>
		private static Delta2DBinding ParseDelta2DBinding( JsonElement bindingElement )
		{
			InputDeviceSlot deviceId = ParseDeviceSlot( GetRequiredString( bindingElement, "DeviceId" ) );
			return new Delta2DBinding(
				deviceId: deviceId,
				controlId: ParseAnalogControl( deviceId, GetRequiredString( bindingElement, "ControlId", InputControlId.Delta.ToString() ) ),
				sensitivity: JsonLoader.GetOptional<float>( bindingElement, "Sensitivity", 1.0f ),
				scaleX: JsonLoader.GetOptional<float>( bindingElement, "ScaleX", 1.0f ),
				scaleY: JsonLoader.GetOptional<float>( bindingElement, "ScaleY", 1.0f ),
				invertX: JsonLoader.GetOptional<bool>( bindingElement, "InvertX", false ),
				invertY: JsonLoader.GetOptional<bool>( bindingElement, "InvertY", false )
			);
		}

		private static InputDeviceSlot ParseDeviceSlot( string deviceId )
		{
			if ( deviceId.Equals( Constants.KEYBOARD_DEVICE_ID, StringComparison.OrdinalIgnoreCase ) ) {
				return InputDeviceSlot.Keyboard;
			}

			if ( deviceId.Equals( Constants.MOUSE_BUTTON_DEVICE_ID, StringComparison.OrdinalIgnoreCase )
				|| deviceId.Equals( Constants.MOUSE_MOTION_DEVICE_ID, StringComparison.OrdinalIgnoreCase )
				|| deviceId.Equals( "Mouse", StringComparison.OrdinalIgnoreCase ) ) {
				return InputDeviceSlot.Mouse;
			}

			if ( deviceId.Equals( Constants.GAMEPAD_DEVICE_ID, StringComparison.OrdinalIgnoreCase )
				|| deviceId.Equals( nameof( InputDeviceSlot.Gamepad0 ), StringComparison.OrdinalIgnoreCase ) ) {
				return InputDeviceSlot.Gamepad0;
			}
			if ( deviceId.Equals( nameof( InputDeviceSlot.Gamepad1 ), StringComparison.OrdinalIgnoreCase ) ) {
				return InputDeviceSlot.Gamepad1;
			}
			if ( deviceId.Equals( nameof( InputDeviceSlot.Gamepad2 ), StringComparison.OrdinalIgnoreCase ) ) {
				return InputDeviceSlot.Gamepad2;
			}
			if ( deviceId.Equals( nameof( InputDeviceSlot.Gamepad3 ), StringComparison.OrdinalIgnoreCase ) ) {
				return InputDeviceSlot.Gamepad3;
			}
			throw new Exception( $"Invalid DeviceId '{deviceId}' in bindings file." );
		}

		private static InputControlId ParseButtonControl( InputDeviceSlot deviceId, string controlName )
		{
			return deviceId switch {
				InputDeviceSlot.Keyboard => ParseKeyboardControl( controlName ),
				InputDeviceSlot.Mouse => ParseMouseButtonControl( controlName ),
				InputDeviceSlot.Gamepad0 or InputDeviceSlot.Gamepad1 or InputDeviceSlot.Gamepad2 or InputDeviceSlot.Gamepad3 => ParseGamepadButtonControl( controlName ),
				_ => throw new Exception( $"Unsupported button device '{deviceId}'." )
			};
		}

		private static InputControlId ParseAnalogControl( InputDeviceSlot deviceId, string controlName )
		{
			return deviceId switch {
				InputDeviceSlot.Keyboard => ParseKeyboardControl( controlName ),
				InputDeviceSlot.Mouse => ParseInputControlId( controlName ),
				InputDeviceSlot.Gamepad0 or InputDeviceSlot.Gamepad1 or InputDeviceSlot.Gamepad2 or InputDeviceSlot.Gamepad3 => ParseInputControlId( controlName ),
				_ => throw new Exception( $"Unsupported analog device '{deviceId}'." )
			};
		}

		private static InputControlId ParseKeyboardControl( string controlName )
		{
			if ( Enum.TryParse( controlName, true, out KeyNum keyNum ) ) {
				return keyNum.ToControlId();
			}
			if ( Enum.TryParse( controlName, true, out InputControlId controlId ) ) {
				return controlId;
			}
			throw new Exception( $"Invalid keyboard control '{controlName}' in bindings file." );
		}

		private static InputControlId ParseMouseButtonControl( string controlName )
		{
			if ( Enum.TryParse( controlName, true, out MouseButton mouseButton ) ) {
				return mouseButton.ToControlId();
			}
			if ( Enum.TryParse( controlName, true, out InputControlId controlId ) ) {
				return controlId;
			}
			throw new Exception( $"Invalid mouse control '{controlName}' in bindings file." );
		}

		private static InputControlId ParseGamepadButtonControl( string controlName )
		{
			if ( Enum.TryParse( controlName, true, out GamepadButton gamepadButton ) ) {
				return gamepadButton.ToControlId();
			}
			if ( Enum.TryParse( controlName, true, out InputControlId controlId ) ) {
				return controlId;
			}
			throw new Exception( $"Invalid gamepad control '{controlName}' in bindings file." );
		}

		private static InputControlId ParseInputControlId( string controlName )
		{
			if ( Enum.TryParse( controlName, true, out InputControlId controlId ) ) {
				return controlId;
			}
			throw new Exception( $"Invalid control '{controlName}' in bindings file." );
		}

		private static string GetRequiredString( JsonElement element, string propertyName, string? fallback = null )
		{
			if ( element.ValueKind == JsonValueKind.String ) {
				return JsonLoader.Read<string>( element, propertyName );
			}
			if ( JsonLoader.TryGet( element, propertyName, out string? value ) ) {
				return value;
			}
			if ( fallback != null ) {
				return fallback;
			}
			throw new Exception( $"Bindings file is missing string property '{propertyName}'." );
		}

		private static string? GetOptionalString( JsonElement element, string propertyName )
		{
			return JsonLoader.TryGet( element, propertyName, out string? value ) ? value : null;
		}

		private sealed class ActionBuilder
		{
			public string Name { get; }
			public string Id { get; }
			public InputValueType ValueType { get; }
			public List<InputBindingDefinition> Bindings { get; } = new();

			public ActionBuilder( string name, string id, InputValueType valueType )
			{
				Name = name;
				Id = id;
				ValueType = valueType;
			}

			public InputActionDefinition Build()
			{
				return new InputActionDefinition( Name, Id, ValueType, Bindings.ToImmutableArray() );
			}
		}
	};
};
