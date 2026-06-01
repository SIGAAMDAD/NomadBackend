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
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nomad.SourceGenerators.CVars
{
    /// <summary>
    /// Generates CVar registry classes from <c>CVarAttribute</c> declarations.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class CVarRegistryGenerator : IIncrementalGenerator
    {
        private const string CVarAttributeMetadataName = "Nomad.Core.CVars.CVarAttribute";
        private static readonly SymbolDisplayFormat FullyQualifiedTypeFormat =
            SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        private static readonly DiagnosticDescriptor CVarNameMissing = new(
            id: "NOMCVAR001",
            title: "CVar name is missing",
            messageFormat: "CVar declaration must provide a non-empty name",
            category: "Nomad.CVars",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor CVarGroupMissing = new(
            id: "NOMCVAR002",
            title: "CVar group is missing",
            messageFormat: "CVar '{0}' must provide a non-empty group",
            category: "Nomad.CVars",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor CVarDuplicateName = new(
            id: "NOMCVAR003",
            title: "Duplicate CVar declaration",
            messageFormat: "CVar '{0}' is declared more than once for registry '{1}'",
            category: "Nomad.CVars",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor CVarDuplicateAccessor = new(
            id: "NOMCVAR004",
            title: "Duplicate generated CVar accessor",
            messageFormat: "CVar accessor '{0}' is generated more than once for registry '{1}'",
            category: "Nomad.CVars",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// Registers the incremental source generation pipeline.
        /// </summary>
        /// <param name="context">The generator initialization context.</param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ImmutableArray<CVarDeclaration>> declarations =
                context.SyntaxProvider.ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: CVarAttributeMetadataName,
                    predicate: static (node, _) => IsCandidateDeclaration(node),
                    transform: static (syntaxContext, _) => CreateDeclarations(syntaxContext));

            context.RegisterSourceOutput(declarations.Collect(), static (sourceProductionContext, collected) =>
            {
                ImmutableArray<CVarDeclaration> flatDeclarations = collected
                    .SelectMany(static declarationSet => declarationSet)
                    .OrderBy(static declaration => declaration.NamespaceName, StringComparer.Ordinal)
                    .ThenBy(static declaration => declaration.Group, StringComparer.Ordinal)
                    .ThenBy(static declaration => declaration.Name, StringComparer.Ordinal)
                    .ToImmutableArray();

                foreach (CVarDeclaration declaration in flatDeclarations)
                {
                    ReportDeclarationDiagnostics(sourceProductionContext, declaration);
                }

                ImmutableHashSet<RegistryKey> invalidRegistries = GetInvalidRegistries(sourceProductionContext, flatDeclarations);

                foreach (IGrouping<RegistryKey, CVarDeclaration> registryGroup in flatDeclarations
                    .Where(static declaration => declaration.CanGenerate)
                    .GroupBy(static declaration => new RegistryKey(declaration.NamespaceName, declaration.Group)))
                {
                    if (invalidRegistries.Contains(registryGroup.Key))
                    {
                        continue;
                    }

                    string source = GenerateRegistrySource(registryGroup.Key, registryGroup.ToImmutableArray());
                    sourceProductionContext.AddSource(
                        CreateHintName(registryGroup.Key),
                        SourceText.From(source, Encoding.UTF8));
                }
            });
        }

        private static bool IsCandidateDeclaration(SyntaxNode node)
        {
            return node is TypeDeclarationSyntax ||
                   node is FieldDeclarationSyntax ||
                   node is VariableDeclaratorSyntax ||
                   node is PropertyDeclarationSyntax;
        }

        private static ImmutableArray<CVarDeclaration> CreateDeclarations(GeneratorAttributeSyntaxContext context)
        {
            ImmutableArray<CVarDeclaration>.Builder builder = ImmutableArray.CreateBuilder<CVarDeclaration>();

            foreach (AttributeData attribute in context.Attributes)
            {
                builder.Add(CreateDeclaration(context.TargetSymbol, attribute));
            }

            return builder.ToImmutable();
        }

        private static CVarDeclaration CreateDeclaration(ISymbol targetSymbol, AttributeData attribute)
        {
            string namespaceName = GetNamespaceName(targetSymbol);
            string name = GetStringConstructorArgument(attribute, 0) ?? string.Empty;
            TypedConstant defaultValue = attribute.ConstructorArguments.Length >= 2
                ? attribute.ConstructorArguments[1]
                : default;

            string group = GetStringNamedArgument(attribute, "Group") ?? "Default";
            string description = GetStringNamedArgument(attribute, "Description") ?? string.Empty;
            string? validatorExpression = GetStringNamedArgument(attribute, "ValidatorExpression");
            string? configuredAccessorName = GetStringNamedArgument(attribute, "AccessorName");
            ulong flagsValue = GetUnsignedIntegerNamedArgument(attribute, "Flags") ?? 0UL;
            ITypeSymbol? definitionValueType = TryGetCVarDefinitionValueType(targetSymbol);
            ITypeSymbol? valueType = definitionValueType ?? GetTypeNamedArgument(attribute, "ValueType") ?? InferDefaultValueType(defaultValue);
            Location? location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                ?? targetSymbol.Locations.FirstOrDefault();

            string typeName = valueType?.ToDisplayString(FullyQualifiedTypeFormat) ?? "global::System.Object";
            string defaultValueExpression = RenderDefaultValue(defaultValue, valueType);
            string accessorName = string.IsNullOrWhiteSpace(configuredAccessorName)
                ? CreateAccessorName(targetSymbol, name)
                : SanitizeTypeName(configuredAccessorName!);
            string? definitionExpression = definitionValueType is null ? null : GetDefinitionExpression(targetSymbol);

            return new CVarDeclaration(
                namespaceName,
                group,
                SanitizeTypeName(group) + "CVarRegistry",
                name,
                accessorName,
                definitionExpression,
                typeName,
                defaultValueExpression,
                description,
                flagsValue,
                validatorExpression,
                location);
        }

        private static void ReportDeclarationDiagnostics(SourceProductionContext context, CVarDeclaration declaration)
        {
            if (string.IsNullOrWhiteSpace(declaration.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(CVarNameMissing, declaration.Location));
            }

            if (string.IsNullOrWhiteSpace(declaration.Group))
            {
                context.ReportDiagnostic(Diagnostic.Create(CVarGroupMissing, declaration.Location, declaration.Name));
            }
        }

        private static ImmutableHashSet<RegistryKey> GetInvalidRegistries(
            SourceProductionContext context,
            ImmutableArray<CVarDeclaration> declarations)
        {
            ImmutableHashSet<RegistryKey>.Builder invalidRegistries = ImmutableHashSet.CreateBuilder<RegistryKey>();

            foreach (IGrouping<RegistryKey, CVarDeclaration> registryGroup in declarations
                .Where(static declaration => declaration.CanGenerate)
                .GroupBy(static declaration => new RegistryKey(declaration.NamespaceName, declaration.Group)))
            {
                foreach (IGrouping<string, CVarDeclaration> duplicateGroup in registryGroup
                    .GroupBy(static declaration => declaration.Name, StringComparer.Ordinal)
                    .Where(static group => group.Count() > 1))
                {
                    invalidRegistries.Add(registryGroup.Key);

                    foreach (CVarDeclaration declaration in duplicateGroup)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            CVarDuplicateName,
                            declaration.Location,
                            declaration.Name,
                            registryGroup.Key.RegistryTypeName));
                    }
                }

                foreach (IGrouping<string, CVarDeclaration> duplicateGroup in registryGroup
                    .GroupBy(static declaration => declaration.AccessorName, StringComparer.Ordinal)
                    .Where(static group => group.Count() > 1))
                {
                    invalidRegistries.Add(registryGroup.Key);

                    foreach (CVarDeclaration declaration in duplicateGroup)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            CVarDuplicateAccessor,
                            declaration.Location,
                            declaration.AccessorName,
                            registryGroup.Key.RegistryTypeName));
                    }
                }
            }

            return invalidRegistries.ToImmutable();
        }

        private static string GenerateRegistrySource(RegistryKey key, ImmutableArray<CVarDeclaration> declarations)
        {
            var builder = new StringBuilder();

            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("#nullable enable");
            builder.AppendLine();

            if (!string.IsNullOrWhiteSpace(key.NamespaceName))
            {
                builder.Append("namespace ").AppendLine(key.NamespaceName);
                builder.AppendLine("{");
            }

            builder.Append('\t').Append("internal static partial class ").AppendLine(key.RegistryTypeName);
            builder.Append('\t').AppendLine("{");
            builder.Append('\t').Append('\t').AppendLine("public static void RegisterCVars( global::Nomad.Core.CVars.ICVarSystemService cvarSystem )");
            builder.Append('\t').Append('\t').AppendLine("{");

            foreach (CVarDeclaration declaration in declarations)
            {
                AppendRegistration(builder, declaration);
            }

            builder.Append('\t').Append('\t').AppendLine("}");

            foreach (CVarDeclaration declaration in declarations)
            {
                AppendAccessor(builder, declaration);
            }

            builder.Append('\t').AppendLine("}");

            if (!string.IsNullOrWhiteSpace(key.NamespaceName))
            {
                builder.AppendLine("}");
            }

            return builder.ToString();
        }

        private static void AppendAccessor(StringBuilder builder, CVarDeclaration declaration)
        {
            string definitionReference = declaration.DefinitionExpression ?? declaration.AccessorName;

            builder.AppendLine();
            if (declaration.DefinitionExpression is null)
            {
                builder.Append('\t').Append('\t')
                    .Append("public static readonly global::Nomad.Core.CVars.CVarDefinition<")
                    .Append(declaration.TypeName)
                    .Append("> ")
                    .Append(declaration.AccessorName)
                    .Append(" = new global::Nomad.Core.CVars.CVarDefinition<")
                    .Append(declaration.TypeName)
                    .Append(">( ")
                    .Append(SymbolDisplay.FormatLiteral(declaration.Name, quote: true))
                    .AppendLine(" );");

                builder.AppendLine();
            }

            builder.Append('\t').Append('\t')
                .Append("public static global::Nomad.Core.CVars.ICVar<")
                .Append(declaration.TypeName)
                .Append("> Get")
                .Append(declaration.AccessorName)
                .AppendLine("( global::Nomad.Core.CVars.ICVarSystemService cvarSystem )");
            builder.Append('\t').Append('\t').AppendLine("{");
            builder.Append('\t').Append('\t').Append('\t')
                .Append("return cvarSystem.GetCVar<")
                .Append(declaration.TypeName)
                .Append(">( ")
                .Append(definitionReference)
                .Append(".Name ) ?? throw new global::Nomad.Core.Exceptions.CVarMissing( ")
                .Append(definitionReference)
                .AppendLine(".Name );");
            builder.Append('\t').Append('\t').AppendLine("}");
        }

        private static void AppendRegistration(StringBuilder builder, CVarDeclaration declaration)
        {
            builder.Append('\t').Append('\t').Append('\t').AppendLine("cvarSystem.Register(");
            builder.Append('\t').Append('\t').Append('\t').Append('\t')
                .Append("new global::Nomad.Core.CVars.CVarCreateInfo<")
                .Append(declaration.TypeName)
                .AppendLine("> {");
            builder.Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                .Append("Name = ")
                .Append(declaration.DefinitionExpression is null
                    ? SymbolDisplay.FormatLiteral(declaration.Name, quote: true)
                    : declaration.DefinitionExpression + ".Name")
                .AppendLine(",");
            builder.Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                .Append("DefaultValue = ")
                .Append(declaration.DefaultValueExpression)
                .AppendLine(",");
            builder.Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                .Append("Description = ")
                .Append(SymbolDisplay.FormatLiteral(declaration.Description, quote: true))
                .AppendLine(",");
            builder.Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                .Append("Group = ")
                .Append(SymbolDisplay.FormatLiteral(declaration.Group, quote: true))
                .AppendLine(",");
            builder.Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                .Append("Flags = ")
                .Append(RenderFlags(declaration.FlagsValue))
                .AppendLine(",");

            if (!string.IsNullOrWhiteSpace(declaration.ValidatorExpression))
            {
                builder.Append('\t').Append('\t').Append('\t').Append('\t').Append('\t')
                    .Append("Validator = ")
                    .Append(declaration.ValidatorExpression)
                    .AppendLine(",");
            }

            builder.Append('\t').Append('\t').Append('\t').Append('\t').AppendLine("}");
            builder.Append('\t').Append('\t').Append('\t').AppendLine(");");
        }

        private static string RenderDefaultValue(TypedConstant defaultValue, ITypeSymbol? valueType)
        {
            string literal = RenderLiteral(defaultValue);

            if (valueType is INamedTypeSymbol namedType && namedType.TypeKind == TypeKind.Enum)
            {
                return "(" + namedType.ToDisplayString(FullyQualifiedTypeFormat) + ")" + literal;
            }

            return literal;
        }

        private static string RenderLiteral(TypedConstant defaultValue)
        {
            if (defaultValue.Value is null)
            {
                return "null!";
            }

            switch (defaultValue.Value)
            {
                case bool value:
                    return value ? "true" : "false";
                case string value:
                    return SymbolDisplay.FormatLiteral(value, quote: true);
                case float value:
                    return value.ToString("R", CultureInfo.InvariantCulture) + "f";
                case double value:
                    return value.ToString("R", CultureInfo.InvariantCulture);
                case uint value:
                    return value.ToString(CultureInfo.InvariantCulture) + "u";
                case ulong value:
                    return value.ToString(CultureInfo.InvariantCulture) + "ul";
                case long value:
                    return value.ToString(CultureInfo.InvariantCulture) + "L";
                case byte value:
                    return value.ToString(CultureInfo.InvariantCulture);
                case sbyte value:
                    return value.ToString(CultureInfo.InvariantCulture);
                case short value:
                    return value.ToString(CultureInfo.InvariantCulture);
                case ushort value:
                    return value.ToString(CultureInfo.InvariantCulture);
                case int value:
                    return value.ToString(CultureInfo.InvariantCulture);
                default:
                    return Convert.ToString(defaultValue.Value, CultureInfo.InvariantCulture) ?? "null!";
            }
        }

        private static string RenderFlags(ulong flagsValue)
        {
            return flagsValue == 0UL
                ? "global::Nomad.Core.CVars.CVarFlags.None"
                : "(global::Nomad.Core.CVars.CVarFlags)" + flagsValue.ToString(CultureInfo.InvariantCulture) + "u";
        }

        private static string GetNamespaceName(ISymbol symbol)
        {
            INamespaceSymbol? namespaceSymbol = symbol switch
            {
                INamedTypeSymbol namedType => namedType.ContainingNamespace,
                IFieldSymbol field => field.ContainingType?.ContainingNamespace,
                IPropertySymbol property => property.ContainingType?.ContainingNamespace,
                _ => symbol.ContainingNamespace
            };

            return namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace
                ? string.Empty
                : namespaceSymbol.ToDisplayString();
        }

        private static string? GetStringConstructorArgument(AttributeData attribute, int index)
        {
            return attribute.ConstructorArguments.Length > index
                ? attribute.ConstructorArguments[index].Value as string
                : null;
        }

        private static string? GetStringNamedArgument(AttributeData attribute, string name)
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (argument.Key == name)
                {
                    return argument.Value.Value as string;
                }
            }

            return null;
        }

        private static ulong? GetUnsignedIntegerNamedArgument(AttributeData attribute, string name)
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (argument.Key != name || argument.Value.Value is null)
                {
                    continue;
                }

                return Convert.ToUInt64(argument.Value.Value, CultureInfo.InvariantCulture);
            }

            return null;
        }

        private static ITypeSymbol? GetTypeNamedArgument(AttributeData attribute, string name)
        {
            foreach (KeyValuePair<string, TypedConstant> argument in attribute.NamedArguments)
            {
                if (argument.Key == name && argument.Value.Value is ITypeSymbol typeSymbol)
                {
                    return typeSymbol;
                }
            }

            return null;
        }

        private static ITypeSymbol? InferDefaultValueType(TypedConstant defaultValue)
        {
            return defaultValue.Type?.SpecialType == SpecialType.System_Object
                ? null
                : defaultValue.Type;
        }

        private static ITypeSymbol? TryGetCVarDefinitionValueType(ISymbol symbol)
        {
            ITypeSymbol? symbolType = symbol switch
            {
                IFieldSymbol field => field.Type,
                IPropertySymbol property => property.Type,
                _ => null
            };

            if (symbolType is INamedTypeSymbol namedType &&
                namedType.ConstructedFrom.MetadataName == "CVarDefinition`1" &&
                namedType.ConstructedFrom.ContainingNamespace.ToDisplayString() == "Nomad.Core.CVars" &&
                namedType.TypeArguments.Length == 1)
            {
                return namedType.TypeArguments[0];
            }

            return null;
        }

        private static string? GetDefinitionExpression(ISymbol symbol)
        {
            return symbol switch
            {
                IFieldSymbol field => field.ToDisplayString(FullyQualifiedTypeFormat),
                IPropertySymbol property => property.ToDisplayString(FullyQualifiedTypeFormat),
                _ => null
            };
        }

        private static string SanitizeTypeName(string group)
        {
            if (string.IsNullOrWhiteSpace(group))
            {
                return "Default";
            }

            var builder = new StringBuilder(group.Length);

            for (int i = 0; i < group.Length; i++)
            {
                char c = group[i];

                if (i == 0)
                {
                    builder.Append(SyntaxFacts.IsIdentifierStartCharacter(c) ? c : '_');
                }
                else
                {
                    builder.Append(SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_');
                }
            }

            return builder.Length == 0 ? "Default" : builder.ToString();
        }

        private static string CreateAccessorName(ISymbol targetSymbol, string cvarName)
        {
            if (targetSymbol is IFieldSymbol or IPropertySymbol)
            {
                return SanitizeTypeName(targetSymbol.Name);
            }

            if (string.IsNullOrWhiteSpace(cvarName))
            {
                return "CVar";
            }

            string[] segments = cvarName
                .Split(new[] { '.', '-', '_', ' ', '/', '\\', ':' }, StringSplitOptions.RemoveEmptyEntries);

            string source = segments.Length == 0 ? cvarName : segments[segments.Length - 1];
            return SanitizeTypeName(source);
        }

        private static string CreateHintName(RegistryKey key)
        {
            return (string.IsNullOrWhiteSpace(key.NamespaceName)
                    ? key.RegistryTypeName
                    : key.NamespaceName.Replace('.', '_') + "_" + key.RegistryTypeName) +
                   ".g.cs";
        }

        private sealed class CVarDeclaration
        {
            public CVarDeclaration(
                string namespaceName,
                string group,
                string registryTypeName,
                string name,
                string accessorName,
                string? definitionExpression,
                string typeName,
                string defaultValueExpression,
                string description,
                ulong flagsValue,
                string? validatorExpression,
                Location? location)
            {
                NamespaceName = namespaceName;
                Group = group;
                RegistryTypeName = registryTypeName;
                Name = name;
                AccessorName = accessorName;
                DefinitionExpression = definitionExpression;
                TypeName = typeName;
                DefaultValueExpression = defaultValueExpression;
                Description = description;
                FlagsValue = flagsValue;
                ValidatorExpression = validatorExpression;
                Location = location;
            }

            public string NamespaceName { get; }
            public string Group { get; }
            public string RegistryTypeName { get; }
            public string Name { get; }
            public string AccessorName { get; }
            public string? DefinitionExpression { get; }
            public string TypeName { get; }
            public string DefaultValueExpression { get; }
            public string Description { get; }
            public ulong FlagsValue { get; }
            public string? ValidatorExpression { get; }
            public Location? Location { get; }

            public bool CanGenerate => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Group);
        }

        private readonly struct RegistryKey : IEquatable<RegistryKey>
        {
            public RegistryKey(string namespaceName, string group)
            {
                NamespaceName = namespaceName;
                Group = group;
                RegistryTypeName = SanitizeTypeName(group) + "CVarRegistry";
            }

            public string NamespaceName { get; }
            public string Group { get; }
            public string RegistryTypeName { get; }

            public bool Equals(RegistryKey other)
            {
                return string.Equals(NamespaceName, other.NamespaceName, StringComparison.Ordinal) &&
                       string.Equals(Group, other.Group, StringComparison.Ordinal);
            }

            public override bool Equals(object? obj)
            {
                return obj is RegistryKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((NamespaceName != null ? StringComparer.Ordinal.GetHashCode(NamespaceName) : 0) * 397) ^
                           (Group != null ? StringComparer.Ordinal.GetHashCode(Group) : 0);
                }
            }
        }
    }
}
