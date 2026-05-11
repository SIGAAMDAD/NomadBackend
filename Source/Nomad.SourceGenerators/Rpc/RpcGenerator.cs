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
using System.Threading;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Nomad.SourceGenerators.Rpc
{
    [Generator(LanguageNames.CSharp)]
    public sealed class RpcGenerator : IIncrementalGenerator
    {
        private const string RpcMethodAttributeName = "RpcMethodAttribute";
        private const string RpcPayloadAttributeName = "RpcMethodPayloadAttribute";

        private static readonly SymbolDisplayFormat TypeDisplayFormat =
            SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
                SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        private static readonly DiagnosticDescriptor InvalidRpcName = new(
            id: "RPCGEN001",
            title: "Invalid RPC generated struct name",
            messageFormat: "RPC method name '{0}' is not a valid C# type identifier.",
            category: "RpcSourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor InvalidPayloadName = new(
            id: "RPCGEN002",
            title: "Invalid RPC payload field name",
            messageFormat: "RPC payload name '{0}' is not a valid C# field identifier.",
            category: "RpcSourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor DuplicatePayloadName = new(
            id: "RPCGEN003",
            title: "Duplicate RPC payload field name",
            messageFormat: "RPC payload field '{0}' is declared more than once for RPC struct '{1}'.",
            category: "RpcSourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor DuplicateRpcStruct = new(
            id: "RPCGEN004",
            title: "Duplicate generated RPC struct",
            messageFormat: "RPC struct '{0}' is generated more than once in namespace '{1}'.",
            category: "RpcSourceGeneration",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<RpcMethodModel?> rpcMethods =
                context.SyntaxProvider.CreateSyntaxProvider(
                        predicate: static (node, _) => IsCandidateMethod(node),
                        transform: static (ctx, ct) => GetRpcMethodModel(ctx, ct))
                    .Where(static model => model is not null);

            context.RegisterSourceOutput(
                rpcMethods.Collect(),
                static (productionContext, models) => Execute(productionContext, models));
        }

        private static bool IsCandidateMethod(SyntaxNode node)
        {
            return node is MethodDeclarationSyntax methodDeclaration &&
                   methodDeclaration.AttributeLists.Count > 0;
        }

        private static RpcMethodModel? GetRpcMethodModel(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (context.Node is not MethodDeclarationSyntax methodDeclaration)
            {
                return null;
            }

            if (context.SemanticModel.GetDeclaredSymbol(methodDeclaration, cancellationToken) is not IMethodSymbol methodSymbol)
            {
                return null;
            }

            AttributeData? rpcMethodAttribute = null;
            List<AttributeData> payloadAttributes = new();

            foreach (AttributeData attribute in methodSymbol.GetAttributes())
            {
                string? attributeName = attribute.AttributeClass?.Name;

                if (attributeName == RpcMethodAttributeName)
                {
                    rpcMethodAttribute = attribute;
                }
                else if (attributeName == RpcPayloadAttributeName)
                {
                    payloadAttributes.Add(attribute);
                }
            }

            if (rpcMethodAttribute is null)
            {
                return null;
            }

            string? structName = GetStringConstructorArgument(rpcMethodAttribute, index: 0);

            if (string.IsNullOrWhiteSpace(structName))
            {
                return new RpcMethodModel(
                    NamespaceName: GetNamespaceName(methodSymbol.ContainingType),
                    StructName: string.Empty,
                    Payloads: ImmutableArray<PayloadFieldModel>.Empty,
                    Location: methodDeclaration.Identifier.GetLocation());
            }

            ImmutableArray<PayloadFieldModel> payloads = payloadAttributes
                .Select(GetPayloadFieldModel)
                .Where(static payload => payload is not null)
                .Cast<PayloadFieldModel>()
                .OrderBy(static payload => payload.Order)
                .ThenBy(static payload => payload.Name, StringComparer.Ordinal)
                .ToImmutableArray();

            return new RpcMethodModel(
                NamespaceName: GetNamespaceName(methodSymbol.ContainingType),
                StructName: structName,
                Payloads: payloads,
                Location: methodDeclaration.Identifier.GetLocation());
        }

        private static PayloadFieldModel? GetPayloadFieldModel(AttributeData attribute)
        {
            string? fieldName = GetStringConstructorArgument(attribute, index: 0);

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return null;
            }

            if (attribute.ConstructorArguments.Length < 2)
            {
                return null;
            }

            TypedConstant typeArgument = attribute.ConstructorArguments[1];

            if (typeArgument.Value is not ITypeSymbol typeSymbol)
            {
                return null;
            }

            int order = 0;

            foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
            {
                if (namedArgument.Key == "Order" &&
                    namedArgument.Value.Value is int orderValue)
                {
                    order = orderValue;
                    break;
                }
            }

            return new PayloadFieldModel(
                Name: fieldName,
                TypeName: typeSymbol.ToDisplayString(TypeDisplayFormat),
                Order: order);
        }

        private static string? GetStringConstructorArgument(AttributeData attribute, int index)
        {
            if (attribute.ConstructorArguments.Length <= index)
            {
                return null;
            }

            return attribute.ConstructorArguments[index].Value as string;
        }

        private static string GetNamespaceName(INamedTypeSymbol owningType)
        {
            INamespaceSymbol? namespaceSymbol = owningType.ContainingNamespace;

            if (namespaceSymbol is null || namespaceSymbol.IsGlobalNamespace)
            {
                return string.Empty;
            }

            return namespaceSymbol.ToDisplayString();
        }

        private static void Execute(SourceProductionContext context, ImmutableArray<RpcMethodModel?> nullableModels)
        {
            ImmutableArray<RpcMethodModel> models = nullableModels
                .Where(static model => model is not null)
                .Cast<RpcMethodModel>()
                .ToImmutableArray();

            foreach (IGrouping<(string NamespaceName, string StructName), RpcMethodModel> group in models.GroupBy(
                         static model => (model.NamespaceName, model.StructName)))
            {
                RpcMethodModel first = group.First();

                if (!IsValidIdentifier(first.StructName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidRpcName,
                        first.Location,
                        first.StructName));

                    continue;
                }

                if (group.Count() > 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DuplicateRpcStruct,
                        first.Location,
                        first.StructName,
                        string.IsNullOrWhiteSpace(first.NamespaceName) ? "<global namespace>" : first.NamespaceName));

                    continue;
                }

                bool hasInvalidPayload = false;

                foreach (PayloadFieldModel payload in first.Payloads)
                {
                    if (!IsValidIdentifier(payload.Name))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            InvalidPayloadName,
                            first.Location,
                            payload.Name));

                        hasInvalidPayload = true;
                    }
                }

                if (hasInvalidPayload)
                {
                    continue;
                }

                foreach (IGrouping<string, PayloadFieldModel> duplicateFieldGroup in first.Payloads.GroupBy(static payload => payload.Name))
                {
                    if (duplicateFieldGroup.Count() > 1)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DuplicatePayloadName,
                            first.Location,
                            duplicateFieldGroup.Key,
                            first.StructName));

                        hasInvalidPayload = true;
                    }
                }

                if (hasInvalidPayload)
                {
                    continue;
                }

                string source = GenerateSource(first);
                string hintName = CreateHintName(first.NamespaceName, first.StructName);

                context.AddSource(hintName, SourceText.From(source, Encoding.UTF8));
            }
        }

        private static string GenerateSource(RpcMethodModel model)
        {
            string escapedStructName = EscapeIdentifier(model.StructName);

            StringBuilder builder = new();

            builder.AppendLine("// <auto-generated/>");
            builder.AppendLine("#nullable enable");
            builder.AppendLine();

            if (!string.IsNullOrWhiteSpace(model.NamespaceName))
            {
                builder.Append("namespace ");
                builder.Append(model.NamespaceName);
                builder.AppendLine(";");
                builder.AppendLine();
            }

            builder.AppendLine("[global::System.CodeDom.Compiler.GeneratedCode(\"TheNomad.SourceGeneration.RpcPayloadSourceGenerator\", \"1.0.0\")]");
            builder.Append("public readonly struct ");
            builder.AppendLine(escapedStructName);
            builder.AppendLine("{");

            foreach (PayloadFieldModel payload in model.Payloads)
            {
                builder.Append("    public readonly ");
                builder.Append(payload.TypeName);
                builder.Append(' ');
                builder.Append(EscapeIdentifier(payload.Name));
                builder.AppendLine(";");
            }

            if (model.Payloads.Length > 0)
            {
                builder.AppendLine();

                builder.Append("    public ");
                builder.Append(escapedStructName);
                builder.Append('(');

                for (int i = 0; i < model.Payloads.Length; i++)
                {
                    PayloadFieldModel payload = model.Payloads[i];

                    if (i > 0)
                    {
                        builder.Append(", ");
                    }

                    builder.Append(payload.TypeName);
                    builder.Append(' ');
                    builder.Append(ToParameterName(payload.Name));
                }

                builder.AppendLine(")");
                builder.AppendLine("    {");

                foreach (PayloadFieldModel payload in model.Payloads)
                {
                    builder.Append("        ");
                    builder.Append(EscapeIdentifier(payload.Name));
                    builder.Append(" = ");
                    builder.Append(ToParameterName(payload.Name));
                    builder.AppendLine(";");
                }

                builder.AppendLine("    }");
            }

            builder.AppendLine("}");

            return builder.ToString();
        }

        private static string ToParameterName(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
            {
                return fieldName;
            }

            string parameterName;

            if (fieldName.Length == 1)
            {
                parameterName = fieldName.ToLowerInvariant();
            }
            else
            {
                parameterName = char.ToLowerInvariant(fieldName[0]) + fieldName.Substring(1);
            }

            if (parameterName == fieldName)
            {
                parameterName = fieldName + "Value";
            }

            return EscapeIdentifier(parameterName);
        }

        private static string CreateHintName(string namespaceName, string structName)
        {
            string fullName = string.IsNullOrWhiteSpace(namespaceName)
                ? structName
                : namespaceName + "." + structName;

            StringBuilder builder = new(fullName.Length + 5);

            foreach (char c in fullName)
            {
                if (char.IsLetterOrDigit(c) || c is '.' or '_')
                {
                    builder.Append(c);
                }
                else
                {
                    builder.Append('_');
                }
            }

            builder.Append(".g.cs");
            return builder.ToString();
        }

        private static bool IsValidIdentifier(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
            {
                return false;
            }

            string escaped = EscapeIdentifier(identifier);
            return SyntaxFacts.IsValidIdentifier(escaped);
        }

        private static string EscapeIdentifier(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                return identifier;
            }

            SyntaxKind keywordKind = SyntaxFacts.GetKeywordKind(identifier);

            if (keywordKind != SyntaxKind.None)
            {
                return "@" + identifier;
            }

            return identifier;
        }

        private sealed record RpcMethodModel(
            string NamespaceName,
            string StructName,
            ImmutableArray<PayloadFieldModel> Payloads,
            Location? Location);

        private sealed record PayloadFieldModel(
            string Name,
            string TypeName,
            int Order);
    }
}
