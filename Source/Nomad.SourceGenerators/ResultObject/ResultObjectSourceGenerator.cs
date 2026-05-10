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
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nomad.SourceGenerators.ResultObject
{
    [Generator(LanguageNames.CSharp)]
    public sealed class ResultObjectSourceGenerator : IIncrementalGenerator
    {
        private const string ResultObjectAttributeMetadataName = "Nomad.Core.Util.ResultObjectAttribute";
        private const string ResultObjectPayloadAttributeMetadataName = "Nomad.Core.Util.ResultObjectPayloadAttribute";
        private const string ResultObjectSuccessAttributeMetadataName = "Nomad.Core.Util.ResultObjectSuccessAttribute";
        private const string ResultObjectFailureAttributeMetadataName = "Nomad.Core.Util.ResultObjectFailureAttribute";

        private const string DefaultSuccessMethodName = "Success";
        private const string DefaultFailureMethodName = "Failure";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ResultObjectModel?> models = context.SyntaxProvider
                .ForAttributeWithMetadataName(
                    ResultObjectAttributeMetadataName,
                    static (node, _) => node is MethodDeclarationSyntax,
                    static (ctx, ct) => CreateModel(ctx, ct))
                .Where(static model => model is not null);

            context.RegisterSourceOutput(models.Collect(), static (ctx, models) => Execute(ctx, models));
        }

        private static void Execute(SourceProductionContext context, ImmutableArray<ResultObjectModel?> nullableModels)
        {
            ResultObjectModel[] models = nullableModels
                .OfType<ResultObjectModel>()
                .OrderBy(static m => m.GeneratedNamespace, StringComparer.Ordinal)
                .ThenBy(static m => m.TypeName, StringComparer.Ordinal)
                .ThenBy(static m => m.MethodKey, StringComparer.Ordinal)
                .ToArray();

            ImmutableHashSet<string> duplicatedGeneratedTypeNames = models
                .GroupBy(static m => m.GeneratedFullName, StringComparer.Ordinal)
                .Where(static g => g.Count() > 1)
                .Select(static g => g.Key)
                .ToImmutableHashSet(StringComparer.Ordinal);

            foreach (ResultObjectModel model in models)
            {
                bool hasErrors = false;

                if (duplicatedGeneratedTypeNames.Contains(model.GeneratedFullName))
                {
                    hasErrors = true;
                    context.ReportDiagnostic(Diagnostic.Create(
                        Descriptors.DuplicateGeneratedType,
                        model.Location,
                        model.GeneratedFullName));
                }

                foreach (Diagnostic diagnostic in Validate(model))
                {
                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        hasErrors = true;
                    }

                    context.ReportDiagnostic(diagnostic);
                }

                if (hasErrors)
                {
                    continue;
                }

                string source = Render(model);
                context.AddSource(model.HintName, SourceText.From(source, Encoding.UTF8));
            }
        }

        private static ResultObjectModel? CreateModel(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (ctx.TargetSymbol is not IMethodSymbol method)
            {
                return null;
            }

            AttributeData? resultObjectAttribute = method.GetAttributes()
                .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == ResultObjectAttributeMetadataName);

            if (resultObjectAttribute is null)
            {
                return null;
            }

            string methodNamespace = method.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : method.ContainingNamespace.ToDisplayString();

            string containingTypeName = FlattenContainingTypeName(method.ContainingType);
            string defaultResultName = containingTypeName + method.Name + "Result";

            string? configuredName = GetString(resultObjectAttribute, constructorIndex: 0, propertyName: "Name");
            string typeName = string.IsNullOrWhiteSpace(configuredName)
                ? defaultResultName
                : configuredName!.Trim();

            bool isRecord = GetBool(resultObjectAttribute, constructorIndex: 1, propertyName: "IsRecord") ?? false;

            string? configuredNamespace = GetString(resultObjectAttribute, constructorIndex: null, propertyName: "Namespace");
            string generatedNamespace = configuredNamespace is null
                ? methodNamespace
                : configuredNamespace.Trim();

            ImmutableArray<ResultMemberModel> allMembers = ReadPayloadMembers(method)
                .OrderBy(static m => m.Order)
                .ThenBy(static m => m.PropertyName, StringComparer.Ordinal)
                .ToImmutableArray();

            ResultFactoryModel successFactory = ReadFactory(
                method,
                ResultObjectSuccessAttributeMetadataName,
                DefaultSuccessMethodName);

            ResultFactoryModel failureFactory = ReadFactory(
                method,
                ResultObjectFailureAttributeMetadataName,
                DefaultFailureMethodName);

            ImmutableArray<ResultMemberModel> successParameters = SelectFactoryParameters(allMembers, successFactory.FieldNames);
            ImmutableArray<ResultMemberModel> failureParameters = SelectFactoryParameters(allMembers, failureFactory.FieldNames);

            string methodKey = method.ToDisplayString(MethodKeyFormat);
            string hintName = BuildHintName(generatedNamespace, typeName, methodKey);
            Location? location = GetAttributeLocation(resultObjectAttribute) ?? method.Locations.FirstOrDefault();

            return new ResultObjectModel(
                generatedNamespace,
                typeName,
                isRecord,
                successFactory,
                failureFactory,
                methodKey,
                hintName,
                location,
                allMembers,
                successParameters,
                failureParameters);
        }

        private static ImmutableArray<ResultMemberModel> ReadPayloadMembers(IMethodSymbol method)
        {
            ImmutableArray<ResultMemberModel>.Builder builder = ImmutableArray.CreateBuilder<ResultMemberModel>();

            foreach (AttributeData attribute in method.GetAttributes().Where(a => a.AttributeClass?.ToDisplayString() == ResultObjectPayloadAttributeMetadataName))
            {
                string? configuredName = GetString(attribute, constructorIndex: 0, propertyName: "Name");
                string propertyName = string.IsNullOrWhiteSpace(configuredName)
                    ? string.Empty
                    : configuredName!.Trim();

                int order = GetInt(attribute, constructorIndex: 2, propertyName: "Order") ?? 0;
                bool isOptional = GetBool(attribute, constructorIndex: null, propertyName: "IsOptional") ?? false;

                string? configuredTypeName = GetString(attribute, constructorIndex: null, propertyName: "TypeName");
                bool hasConfiguredTypeName = !string.IsNullOrWhiteSpace(configuredTypeName);

                ITypeSymbol? typeSymbol = GetTypeSymbol(attribute);
                bool isOpenGeneric = typeSymbol is INamedTypeSymbol namedType && namedType.IsUnboundGenericType;

                string typeName = hasConfiguredTypeName
                    ? configuredTypeName!.Trim()
                    : typeSymbol?.ToDisplayString(FullyQualifiedNullableFormat) ?? "global::System.Object";

                builder.Add(new ResultMemberModel(
                    propertyName,
                    ToParameterName(propertyName),
                    typeName,
                    isOptional,
                    order,
                    isOpenGeneric,
                    hasConfiguredTypeName,
                    GetAttributeLocation(attribute) ?? method.Locations.FirstOrDefault()));
            }

            return builder.ToImmutable();
        }

        private static ResultFactoryModel ReadFactory(IMethodSymbol method, string metadataName, string defaultMethodName)
        {
            AttributeData? attribute = method.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == metadataName);

            if (attribute is null)
            {
                return new ResultFactoryModel(
                    defaultMethodName,
                    ImmutableArray<string>.Empty,
                    method.Locations.FirstOrDefault());
            }

            string methodName = GetMethodName(attribute, "MethodName", defaultMethodName);
            ImmutableArray<string> fieldNames = GetStringArray(attribute, constructorIndex: 0, propertyName: "FieldNames")
                .Select(static fieldName => fieldName?.Trim() ?? string.Empty)
                .ToImmutableArray();

            return new ResultFactoryModel(
                methodName,
                fieldNames,
                GetAttributeLocation(attribute) ?? method.Locations.FirstOrDefault());
        }

        private static ImmutableArray<ResultMemberModel> SelectFactoryParameters(
            ImmutableArray<ResultMemberModel> availableMembers,
            ImmutableArray<string> requestedFieldNames)
        {
            ImmutableHashSet<string> requestedFieldNameSet = requestedFieldNames
                .Where(static fieldName => !string.IsNullOrWhiteSpace(fieldName))
                .ToImmutableHashSet(StringComparer.Ordinal);

            if (requestedFieldNameSet.Count == 0)
            {
                return ImmutableArray<ResultMemberModel>.Empty;
            }

            return availableMembers
                .Where(member => requestedFieldNameSet.Contains(member.PropertyName))
                .OrderBy(static member => member.Order)
                .ThenBy(static member => member.PropertyName, StringComparer.Ordinal)
                .ToImmutableArray();
        }

        private static IEnumerable<Diagnostic> Validate(ResultObjectModel model)
        {
            if (!IsValidTypeIdentifier(model.TypeName))
            {
                yield return Diagnostic.Create(
                    Descriptors.InvalidResultObjectName,
                    model.Location,
                    model.TypeName);
            }

            if (!IsValidNamespace(model.GeneratedNamespace))
            {
                yield return Diagnostic.Create(
                    Descriptors.InvalidNamespace,
                    model.Location,
                    model.GeneratedNamespace);
            }

            if (!IsValidTypeIdentifier(model.SuccessFactory.MethodName))
            {
                yield return Diagnostic.Create(
                    Descriptors.InvalidFactoryMethodName,
                    model.SuccessFactory.Location,
                    model.SuccessFactory.MethodName,
                    model.GeneratedFullName);
            }

            if (!IsValidTypeIdentifier(model.FailureFactory.MethodName))
            {
                yield return Diagnostic.Create(
                    Descriptors.InvalidFactoryMethodName,
                    model.FailureFactory.Location,
                    model.FailureFactory.MethodName,
                    model.GeneratedFullName);
            }

            if (string.Equals(model.SuccessFactory.MethodName, model.FailureFactory.MethodName, StringComparison.Ordinal))
            {
                yield return Diagnostic.Create(
                    Descriptors.DuplicateFactoryMethodName,
                    model.SuccessFactory.Location ?? model.FailureFactory.Location ?? model.Location,
                    model.SuccessFactory.MethodName,
                    model.GeneratedFullName);
            }

            foreach (ResultMemberModel member in model.AllMembers)
            {
                if (!IsValidTypeIdentifier(member.PropertyName))
                {
                    yield return Diagnostic.Create(
                        Descriptors.InvalidMemberName,
                        member.Location,
                        member.PropertyName,
                        model.GeneratedFullName);
                }

                if (ReservedPropertyNames.Contains(member.PropertyName))
                {
                    yield return Diagnostic.Create(
                        Descriptors.ReservedMemberName,
                        member.Location,
                        member.PropertyName,
                        model.GeneratedFullName);
                }

                if (string.Equals(member.PropertyName, model.SuccessFactory.MethodName, StringComparison.Ordinal)
                    || string.Equals(member.PropertyName, model.FailureFactory.MethodName, StringComparison.Ordinal))
                {
                    yield return Diagnostic.Create(
                        Descriptors.MemberFactoryNameCollision,
                        member.Location,
                        member.PropertyName,
                        model.GeneratedFullName);
                }

                if (member.Order < 0)
                {
                    yield return Diagnostic.Create(
                        Descriptors.NegativeOrder,
                        member.Location,
                        member.PropertyName,
                        member.Order);
                }

                if (member.IsOpenGenericType && !member.HasExplicitTypeName)
                {
                    yield return Diagnostic.Create(
                        Descriptors.OpenGenericRequiresTypeName,
                        member.Location,
                        member.PropertyName,
                        member.TypeName);
                }
            }

            ImmutableHashSet<string> availableFieldNames = model.AllMembers
                .Select(static member => member.PropertyName)
                .ToImmutableHashSet(StringComparer.Ordinal);

            foreach (IGrouping<string, ResultMemberModel> duplicate in model.AllMembers.GroupBy(static m => m.PropertyName, StringComparer.Ordinal).Where(static g => g.Count() > 1))
            {
                foreach (ResultMemberModel member in duplicate)
                {
                    yield return Diagnostic.Create(
                        Descriptors.DuplicateMemberName,
                        member.Location,
                        member.PropertyName,
                        model.GeneratedFullName);
                }
            }

            foreach (Diagnostic diagnostic in ValidateFactoryFieldList(model.GeneratedFullName, model.SuccessFactory, availableFieldNames))
            {
                yield return diagnostic;
            }

            foreach (Diagnostic diagnostic in ValidateFactoryFieldList(model.GeneratedFullName, model.FailureFactory, availableFieldNames))
            {
                yield return diagnostic;
            }

            foreach (Diagnostic diagnostic in ValidateFactoryParameters(model.GeneratedFullName, model.SuccessFactory.MethodName, model.SuccessParameters))
            {
                yield return diagnostic;
            }

            foreach (Diagnostic diagnostic in ValidateFactoryParameters(model.GeneratedFullName, model.FailureFactory.MethodName, model.FailureParameters))
            {
                yield return diagnostic;
            }
        }

        private static IEnumerable<Diagnostic> ValidateFactoryFieldList(
            string generatedFullName,
            ResultFactoryModel factory,
            ImmutableHashSet<string> availableFieldNames)
        {
            var seenFieldNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (string fieldName in factory.FieldNames)
            {
                if (!IsValidTypeIdentifier(fieldName))
                {
                    yield return Diagnostic.Create(
                        Descriptors.InvalidFactoryFieldName,
                        factory.Location,
                        fieldName,
                        factory.MethodName,
                        generatedFullName);
                    continue;
                }

                if (!seenFieldNames.Add(fieldName))
                {
                    yield return Diagnostic.Create(
                        Descriptors.DuplicateFactoryFieldName,
                        factory.Location,
                        fieldName,
                        factory.MethodName,
                        generatedFullName);
                    continue;
                }

                if (!availableFieldNames.Contains(fieldName))
                {
                    yield return Diagnostic.Create(
                        Descriptors.UnknownFactoryFieldName,
                        factory.Location,
                        fieldName,
                        factory.MethodName,
                        generatedFullName);
                }
            }
        }

        private static IEnumerable<Diagnostic> ValidateFactoryParameters(
            string generatedFullName,
            string factoryName,
            ImmutableArray<ResultMemberModel> parameters)
        {
            bool optionalParameterSeen = false;
            var parameterNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (ResultMemberModel parameter in parameters)
            {
                if (!parameterNames.Add(parameter.ParameterName))
                {
                    yield return Diagnostic.Create(
                        Descriptors.DuplicateParameterName,
                        parameter.Location,
                        parameter.ParameterName,
                        factoryName,
                        generatedFullName);
                }

                if (optionalParameterSeen && !parameter.IsOptional)
                {
                    yield return Diagnostic.Create(
                        Descriptors.RequiredParameterAfterOptional,
                        parameter.Location,
                        parameter.PropertyName,
                        factoryName,
                        generatedFullName);
                }

                optionalParameterSeen |= parameter.IsOptional;
            }
        }

        private static string Render(ResultObjectModel model)
        {
            var sb = new StringBuilder(capacity: 4096);

            sb.AppendLine("// <auto-generated />");
            sb.AppendLine("#nullable enable");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(model.GeneratedNamespace))
            {
                sb.Append("namespace ").Append(model.GeneratedNamespace).AppendLine();
                sb.AppendLine("{");
                sb.AppendLine();
            }

            string indent = string.IsNullOrWhiteSpace(model.GeneratedNamespace) ? string.Empty : "    ";

            if (model.IsRecord)
            {
                RenderRecord(sb, model, indent);
            }
            else
            {
                RenderReadonlyStruct(sb, model, indent);
            }

            if (!string.IsNullOrWhiteSpace(model.GeneratedNamespace))
            {
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        private static void RenderReadonlyStruct(StringBuilder sb, ResultObjectModel model, string indent)
        {
            sb.Append(indent).AppendLine("[global::System.Diagnostics.DebuggerDisplay(\"{DebuggerDisplay,nq}\")]");
            sb.Append(indent).Append("public readonly partial struct ").Append(model.TypeName).AppendLine();
            sb.Append(indent).AppendLine("{");
            RenderMembers(sb, model, indent + "    ");
            sb.Append(indent).AppendLine("}");
        }

        private static void RenderRecord(StringBuilder sb, ResultObjectModel model, string indent)
        {
            sb.Append(indent).AppendLine("[global::System.Diagnostics.DebuggerDisplay(\"{DebuggerDisplay,nq}\")]");
            sb.Append(indent).Append("public sealed partial record ").Append(model.TypeName).AppendLine();
            sb.Append(indent).AppendLine("{");
            RenderMembers(sb, model, indent + "    ");
            sb.Append(indent).AppendLine("}");
        }

        private static void RenderMembers(StringBuilder sb, ResultObjectModel model, string indent)
        {
            RenderProperties(sb, model, indent);
            RenderConstructor(sb, model, indent);
            RenderFactory(sb, model, indent, model.SuccessFactory.MethodName, isSuccess: true, model.SuccessParameters);
            sb.AppendLine();
            RenderFactory(sb, model, indent, model.FailureFactory.MethodName, isSuccess: false, model.FailureParameters);
            sb.AppendLine();
            RenderUtilities(sb, model, indent);
        }

        private static void RenderProperties(StringBuilder sb, ResultObjectModel model, string indent)
        {
            sb.Append(indent).AppendLine("public bool IsSuccess { get; }");
            sb.Append(indent).AppendLine("public bool IsFailure => !IsSuccess;");

            foreach (ResultMemberModel member in model.AllMembers)
            {
                sb.Append(indent)
                    .Append("public ")
                    .Append(GetGeneratedTypeName(member))
                    .Append(' ')
                    .Append(member.PropertyName)
                    .AppendLine(" { get; }");
            }

            sb.AppendLine();
        }

        private static void RenderConstructor(StringBuilder sb, ResultObjectModel model, string indent)
        {
            sb.Append(indent)
                .Append("private ")
                .Append(model.TypeName)
                .Append("(bool isSuccess");

            foreach (ResultMemberModel member in model.AllMembers)
            {
                sb.Append(", ")
                    .Append(GetGeneratedTypeName(member))
                    .Append(' ')
                    .Append(EscapeIdentifier(member.ParameterName));
            }

            sb.AppendLine(")");
            sb.Append(indent).AppendLine("{");
            sb.Append(indent).AppendLine("    IsSuccess = isSuccess;");

            foreach (ResultMemberModel member in model.AllMembers)
            {
                sb.Append(indent)
                    .Append("    ")
                    .Append(member.PropertyName)
                    .Append(" = ")
                    .Append(EscapeIdentifier(member.ParameterName))
                    .AppendLine(";");
            }

            sb.Append(indent).AppendLine("}");
            sb.AppendLine();
        }

        private static void RenderFactory(
            StringBuilder sb,
            ResultObjectModel model,
            string indent,
            string methodName,
            bool isSuccess,
            ImmutableArray<ResultMemberModel> parameters)
        {
            sb.Append(indent)
                .Append("public static ")
                .Append(model.TypeName)
                .Append(' ')
                .Append(methodName)
                .Append('(');

            RenderParameterList(sb, parameters);

            sb.AppendLine(")");
            sb.Append(indent)
                .Append("    => new ")
                .Append(model.TypeName)
                .Append('(')
                .Append(isSuccess ? "true" : "false");

            foreach (ResultMemberModel member in model.AllMembers)
            {
                string valueExpression = parameters.Any(p => p.PropertyName == member.PropertyName)
                    ? EscapeIdentifier(member.ParameterName)
                    : DefaultExpression(member);

                sb.Append(", ").Append(valueExpression);
            }

            sb.AppendLine(");");
        }

        private static void RenderParameterList(StringBuilder sb, ImmutableArray<ResultMemberModel> parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                ResultMemberModel parameter = parameters[i];

                sb.Append(GetGeneratedTypeName(parameter))
                    .Append(' ')
                    .Append(EscapeIdentifier(parameter.ParameterName));

                if (parameter.IsOptional)
                {
                    sb.Append(" = ").Append(DefaultExpression(parameter));
                }
            }
        }

        private static void RenderUtilities(StringBuilder sb, ResultObjectModel model, string indent)
        {
            sb.Append(indent).AppendLine("public void Deconstruct(out bool isSuccess)");
            sb.Append(indent).AppendLine("{");
            sb.Append(indent).AppendLine("    isSuccess = IsSuccess;");
            sb.Append(indent).AppendLine("}");
            sb.AppendLine();
            sb.Append(indent).AppendLine("public override string ToString()");
            sb.Append(indent).AppendLine("    => DebuggerDisplay;");
            sb.AppendLine();
            sb.Append(indent).AppendLine("private string DebuggerDisplay");
            sb.Append(indent)
                .Append("    => IsSuccess ? ")
                .Append(ToStringLiteral(model.SuccessFactory.MethodName))
                .Append(" : ")
                .Append(ToStringLiteral(model.FailureFactory.MethodName))
                .AppendLine(";");
        }

        private static string GetGeneratedTypeName(ResultMemberModel member)
        {
            if (!member.IsOptional || IsNullableTypeName(member.TypeName))
            {
                return member.TypeName;
            }

            return member.TypeName + "?";
        }

        private static string DefaultExpression(ResultMemberModel member)
            => member.IsOptional ? "default" : "default!";

        private static bool IsNullableTypeName(string typeName)
            => typeName.EndsWith("?", StringComparison.Ordinal)
               || typeName.StartsWith("global::System.Nullable<", StringComparison.Ordinal)
               || typeName.StartsWith("System.Nullable<", StringComparison.Ordinal);

        private static ITypeSymbol? GetTypeSymbol(AttributeData attribute)
        {
            if (attribute.ConstructorArguments.Length > 1 && attribute.ConstructorArguments[1].Value is ITypeSymbol constructorTypeSymbol)
            {
                return constructorTypeSymbol;
            }

            return TryGetNamedArgument(attribute, "Type", out TypedConstant namedType)
                   && namedType.Value is ITypeSymbol namedTypeSymbol
                ? namedTypeSymbol
                : null;
        }

        private static Location? GetAttributeLocation(AttributeData attribute)
            => attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation();

        private static string? GetString(AttributeData attribute, int? constructorIndex, string propertyName)
        {
            if (constructorIndex is int index && attribute.ConstructorArguments.Length > index)
            {
                return attribute.ConstructorArguments[index].Value as string;
            }

            return TryGetNamedArgument(attribute, propertyName, out TypedConstant constant)
                ? constant.Value as string
                : null;
        }

        private static ImmutableArray<string> GetStringArray(AttributeData attribute, int? constructorIndex, string propertyName)
        {
            if (constructorIndex is int index && attribute.ConstructorArguments.Length > index)
            {
                return GetStringArray(attribute.ConstructorArguments[index]);
            }

            return TryGetNamedArgument(attribute, propertyName, out TypedConstant constant)
                ? GetStringArray(constant)
                : ImmutableArray<string>.Empty;
        }

        private static ImmutableArray<string> GetStringArray(TypedConstant constant)
        {
            ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();

            if (constant.Kind == TypedConstantKind.Array)
            {
                foreach (TypedConstant value in constant.Values)
                {
                    builder.Add(value.Value as string ?? string.Empty);
                }
            }
            else
            {
                builder.Add(constant.Value as string ?? string.Empty);
            }

            return builder.ToImmutable();
        }

        private static bool? GetBool(AttributeData attribute, int? constructorIndex, string propertyName)
        {
            if (constructorIndex is int index
                && attribute.ConstructorArguments.Length > index
                && attribute.ConstructorArguments[index].Value is bool value)
            {
                return value;
            }

            return TryGetNamedArgument(attribute, propertyName, out TypedConstant constant)
                   && constant.Value is bool namedValue
                ? namedValue
                : null;
        }

        private static int? GetInt(AttributeData attribute, int? constructorIndex, string propertyName)
        {
            if (constructorIndex is int index
                && attribute.ConstructorArguments.Length > index
                && attribute.ConstructorArguments[index].Value is int value)
            {
                return value;
            }

            return TryGetNamedArgument(attribute, propertyName, out TypedConstant constant)
                   && constant.Value is int namedValue
                ? namedValue
                : null;
        }

        private static bool TryGetNamedArgument(AttributeData attribute, string name, out TypedConstant value)
        {
            foreach (KeyValuePair<string, TypedConstant> pair in attribute.NamedArguments)
            {
                if (pair.Key == name)
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static string GetMethodName(AttributeData attribute, string propertyName, string fallback)
        {
            string? value = GetString(attribute, constructorIndex: null, propertyName: propertyName);
            return string.IsNullOrWhiteSpace(value) ? fallback : value!.Trim();
        }

        private static string FlattenContainingTypeName(INamedTypeSymbol type)
        {
            var stack = new Stack<string>();
            INamedTypeSymbol? current = type;

            while (current is not null)
            {
                stack.Push(current.Name);
                current = current.ContainingType;
            }

            return string.Concat(stack);
        }

        private static string ToParameterName(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return "value";
            }

            if (propertyName.Length == 1)
            {
                return propertyName.ToLowerInvariant();
            }

            if (propertyName.All(char.IsUpper))
            {
                return propertyName.ToLowerInvariant();
            }

            return char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);
        }

        private static string EscapeIdentifier(string identifier)
            => SyntaxFacts.GetKeywordKind(identifier) == SyntaxKind.None ? identifier : "@" + identifier;

        private static bool IsValidTypeIdentifier(string identifier)
            => !string.IsNullOrWhiteSpace(identifier)
               && SyntaxFacts.IsValidIdentifier(identifier)
               && SyntaxFacts.GetKeywordKind(identifier) == SyntaxKind.None;

        private static bool IsValidNamespace(string generatedNamespace)
        {
            if (generatedNamespace.Length == 0)
            {
                return true;
            }

            return generatedNamespace
                .Split('.')
                .All(IsValidTypeIdentifier);
        }

        private static string BuildHintName(string generatedNamespace, string typeName, string methodKey)
        {
            string stableKey = string.IsNullOrWhiteSpace(generatedNamespace)
                ? typeName
                : generatedNamespace + "." + typeName;

            return SanitizeHintName(stableKey) + "." + Hash32(methodKey).ToString("x8") + ".g.cs";
        }

        private static string SanitizeHintName(string value)
        {
            var sb = new StringBuilder(value.Length);

            foreach (char c in value)
            {
                sb.Append(char.IsLetterOrDigit(c) || c == '_' || c == '.' ? c : '_');
            }

            return sb.Length == 0 ? "ResultObject" : sb.ToString();
        }

        private static uint Hash32(string value)
        {
            unchecked
            {
                const uint offsetBasis = 2166136261;
                const uint prime = 16777619;

                uint hash = offsetBasis;
                foreach (char c in value)
                {
                    hash ^= c;
                    hash *= prime;
                }

                return hash;
            }
        }

        private static string ToStringLiteral(string value)
        {
            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');

            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        private static readonly ImmutableHashSet<string> ReservedPropertyNames = ImmutableHashSet.Create(
            StringComparer.Ordinal,
            "IsSuccess",
            "IsFailure",
            "DebuggerDisplay");

        private static readonly SymbolDisplayFormat FullyQualifiedNullableFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private static readonly SymbolDisplayFormat MethodKeyFormat = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions:
                SymbolDisplayMemberOptions.IncludeContainingType |
                SymbolDisplayMemberOptions.IncludeParameters |
                SymbolDisplayMemberOptions.IncludeType,
            parameterOptions:
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeName |
                SymbolDisplayParameterOptions.IncludeParamsRefOut,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
    }

    internal sealed class ResultObjectModel
    {
        public ResultObjectModel(
            string generatedNamespace,
            string typeName,
            bool isRecord,
            ResultFactoryModel successFactory,
            ResultFactoryModel failureFactory,
            string methodKey,
            string hintName,
            Location? location,
            ImmutableArray<ResultMemberModel> allMembers,
            ImmutableArray<ResultMemberModel> successParameters,
            ImmutableArray<ResultMemberModel> failureParameters)
        {
            GeneratedNamespace = generatedNamespace;
            TypeName = typeName;
            IsRecord = isRecord;
            SuccessFactory = successFactory;
            FailureFactory = failureFactory;
            MethodKey = methodKey;
            HintName = hintName;
            Location = location;
            AllMembers = allMembers;
            SuccessParameters = successParameters;
            FailureParameters = failureParameters;
        }

        public string GeneratedNamespace { get; }
        public string TypeName { get; }
        public bool IsRecord { get; }
        public ResultFactoryModel SuccessFactory { get; }
        public ResultFactoryModel FailureFactory { get; }
        public string MethodKey { get; }
        public string HintName { get; }
        public Location? Location { get; }
        public ImmutableArray<ResultMemberModel> AllMembers { get; }
        public ImmutableArray<ResultMemberModel> SuccessParameters { get; }
        public ImmutableArray<ResultMemberModel> FailureParameters { get; }

        public string GeneratedFullName => string.IsNullOrWhiteSpace(GeneratedNamespace)
            ? TypeName
            : GeneratedNamespace + "." + TypeName;
    }

    internal sealed class ResultFactoryModel
    {
        public ResultFactoryModel(string methodName, ImmutableArray<string> fieldNames, Location? location)
        {
            MethodName = methodName;
            FieldNames = fieldNames;
            Location = location;
        }

        public string MethodName { get; }
        public ImmutableArray<string> FieldNames { get; }
        public Location? Location { get; }
    }

    internal sealed class ResultMemberModel
    {
        public ResultMemberModel(
            string propertyName,
            string parameterName,
            string typeName,
            bool isOptional,
            int order,
            bool isOpenGenericType,
            bool hasExplicitTypeName,
            Location? location)
        {
            PropertyName = propertyName;
            ParameterName = parameterName;
            TypeName = typeName;
            IsOptional = isOptional;
            Order = order;
            IsOpenGenericType = isOpenGenericType;
            HasExplicitTypeName = hasExplicitTypeName;
            Location = location;
        }

        public string PropertyName { get; }
        public string ParameterName { get; }
        public string TypeName { get; }
        public bool IsOptional { get; }
        public int Order { get; }
        public bool IsOpenGenericType { get; }
        public bool HasExplicitTypeName { get; }
        public Location? Location { get; }
    }

    internal static class Descriptors
    {
        public static readonly DiagnosticDescriptor DuplicateGeneratedType = new(
            id: "ROG001",
            title: "Duplicate generated result object type",
            messageFormat: "More than one method generates the result object type '{0}'. Give each ResultObject a unique Name or Namespace.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidResultObjectName = new(
            id: "ROG002",
            title: "Invalid result object name",
            messageFormat: "'{0}' is not a valid generated result object type name. Use a simple C# type identifier and put namespaces in ResultObjectAttribute.Namespace.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidNamespace = new(
            id: "ROG003",
            title: "Invalid generated namespace",
            messageFormat: "'{0}' is not a valid generated namespace.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidMemberName = new(
            id: "ROG004",
            title: "Invalid result object member name",
            messageFormat: "'{0}' is not a valid generated member name for result object '{1}'.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor ReservedMemberName = new(
            id: "ROG005",
            title: "Reserved result object member name",
            messageFormat: "'{0}' is reserved by result object '{1}'. Choose a different payload name.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateMemberName = new(
            id: "ROG006",
            title: "Duplicate result object member name",
            messageFormat: "Result object '{1}' contains more than one generated member named '{0}'.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateParameterName = new(
            id: "ROG007",
            title: "Duplicate generated factory parameter name",
            messageFormat: "Generated parameter name '{0}' appears more than once in {1}(...) for result object '{2}'. Change one payload name.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor RequiredParameterAfterOptional = new(
            id: "ROG008",
            title: "Required generated parameter follows optional parameter",
            messageFormat: "Generated parameter for member '{0}' in {1}(...) for result object '{2}' is required but follows an optional parameter. Increase/decrease Order or mark it optional.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor OpenGenericRequiresTypeName = new(
            id: "ROG009",
            title: "Open generic payload type requires TypeName",
            messageFormat: "Member '{0}' uses open generic type '{1}'. Provide a closed C# type through TypeName.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor NegativeOrder = new(
            id: "ROG010",
            title: "Negative result object member order",
            messageFormat: "Member '{0}' has negative Order value {1}. Order must be zero or greater.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidFactoryMethodName = new(
            id: "ROG011",
            title: "Invalid result object factory method name",
            messageFormat: "'{0}' is not a valid factory method name for result object '{1}'.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateFactoryMethodName = new(
            id: "ROG012",
            title: "Duplicate result object factory method name",
            messageFormat: "Result object '{1}' uses '{0}' for both success and failure factory methods.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor MemberFactoryNameCollision = new(
            id: "ROG013",
            title: "Result object member conflicts with factory method name",
            messageFormat: "Member '{0}' conflicts with a generated factory method on result object '{1}'. Choose a different payload name or factory method name.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor UnknownFactoryFieldName = new(
            id: "ROG014",
            title: "Factory references unknown result object field",
            messageFormat: "Factory method '{1}' for result object '{2}' references field '{0}', but no ResultObjectPayload with that name exists.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor DuplicateFactoryFieldName = new(
            id: "ROG015",
            title: "Factory references result object field more than once",
            messageFormat: "Factory method '{1}' for result object '{2}' references field '{0}' more than once.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor InvalidFactoryFieldName = new(
            id: "ROG016",
            title: "Invalid factory field reference",
            messageFormat: "'{0}' is not a valid field reference in factory method '{1}' for result object '{2}'.",
            category: "ResultObjects",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}
