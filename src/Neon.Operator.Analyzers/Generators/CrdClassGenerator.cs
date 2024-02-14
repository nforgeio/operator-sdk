// -----------------------------------------------------------------------------
// FILE:	    CrdClassGenerator.cs
// CONTRIBUTOR: NEONFORGE Team
// COPYRIGHT:   Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;

using k8s;
using k8s.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Neon.K8s.Core;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Neon.Operator.Analyzers
{
    [Generator]
    public class CrdClassGenerator : ISourceGenerator
    {
        private HashSet<string> sources;
        private GeneratorExecutionContext context;

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //System.Diagnostics.Debugger.Launch();

            this.sources = new HashSet<string>();
            this.context = context;

            foreach (var file in context.AdditionalFiles)
            {
                V1CustomResourceDefinition crd;

                try
                {
                    crd = KubernetesHelper.YamlDeserialize<V1CustomResourceDefinition>(file.GetText()?.ToString(), stringTypeDeserialization: false);
                }
                catch (Exception e)
                {
                    // not a valid CRD.
                    continue;
                }

                var basename = crd.Spec.Names.Kind;
                foreach (var version in crd.Spec.Versions)
                {
                    var className = GetClassName(version: version.Name, kind: crd.Spec.Names.Kind);
                    var compilation = CompilationUnit();

                    var crdClass = ClassDeclaration(className)
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword)))
                        .AddBaseListTypes(
                            SimpleBaseType(ParseTypeName(typeof(IKubernetesObject<V1ObjectMeta>).GetGlobalTypeName())))
                        .AddAttributeLists(AttributeList(SeparatedList(nodes: [CreateKubernetesEntityAttribute(crd.Spec.Group, crd.Spec.Names.Kind, version.Name, crd.Spec.Names.Plural)])))
                        .AddMembers(
                            ConstructorDeclaration(GetClassName(version: version.Name, kind: crd.Spec.Names.Kind))
                                .WithBody(Block(
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName("ApiVersion"),
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal($"{crd.Spec.Group}/{version.Name}")))),
                                    ExpressionStatement(
                                        AssignmentExpression(
                                            SyntaxKind.SimpleAssignmentExpression,
                                            IdentifierName("Kind"),
                                            LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                Literal(crd.Spec.Names.Kind))))))
                                .AddModifiers(Token(SyntaxKind.PublicKeyword)),
                            PropertyDeclaration(
                                type:       ParseTypeName(typeof(string).GetGlobalTypeName()),
                                identifier: nameof(KubernetesEntityAttribute.ApiVersion))
                                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                .AddAccessorListAccessors(
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))),
                            PropertyDeclaration(
                                type:       ParseTypeName(typeof(string).GetGlobalTypeName()),
                                identifier: nameof(KubernetesEntityAttribute.Kind))
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                        .AddAccessorListAccessors(
                                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                            AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))),
                            PropertyDeclaration(
                                type:       ParseTypeName(typeof(V1ObjectMeta).GetGlobalTypeName()),
                                identifier: "Metadata")
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))
                            );

                    if (version.Schema.OpenAPIV3Schema.Properties.ContainsKey("spec"))
                    {
                        var specClassName = GetSpecClassName(version: version.Name, kind: crd.Spec.Names.Kind);
                        var refTypeName = $"global::Neon.Operator.Resources.{className}.{specClassName}";

                        crdClass = crdClass
                            .AddBaseListTypes(
                                SimpleBaseType(ParseTypeName($"global::{typeof(ISpec<>).Namespace}.ISpec<{refTypeName}>")))
                            .AddMembers(
                                PropertyDeclaration(ParseTypeName(refTypeName), "Spec")
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))));

                        AddObject(
                            specClassName,
                            version.Schema.OpenAPIV3Schema.Properties["spec"],
                            className);
                    }

                    if (version.Schema.OpenAPIV3Schema.Properties.ContainsKey("status"))
                    {
                        var statusClassName = GetStatusClassName(version: version.Name, kind: crd.Spec.Names.Kind);
                        var refTypeName = $"global::Neon.Operator.Resources.{className}.{statusClassName}";

                        crdClass = crdClass
                            .AddBaseListTypes(
                                SimpleBaseType(ParseTypeName($"global::{typeof(IStatus<>).Namespace}.IStatus<{refTypeName}>")))
                            .AddMembers(
                                PropertyDeclaration(ParseTypeName(refTypeName), "Status")
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))));

                        AddObject(
                            statusClassName,
                            version.Schema.OpenAPIV3Schema.Properties["status"],
                            className);
                    }

                    compilation = compilation
                        .WithMembers(SingletonList<MemberDeclarationSyntax>(
                            NamespaceDeclaration(ParseName("Neon.Operator.Resources"))
                            .AddMembers(crdClass)));

                    var compilationString = compilation.NormalizeWhitespace().ToString();

                    context.AddSource(
                        $"{className}.g.cs",
                        SourceText.From(compilationString, Encoding.UTF8, SourceHashAlgorithm.Sha256)
                        );
                }

            }
        }

        private PropertyDeclarationSyntax AddProperty(
            string name,
            V1JSONSchemaProps properties,
            bool required,
            string baseClassName = null)
        {
            PropertyDeclarationSyntax propertyDeclaration = null;

            switch (properties.Type)
            {
                case Constants.BooleanTypeString:
                case Constants.IntegerTypeString:
                case Constants.NumberTypeString:
                case Constants.Int32TypeString:
                case Constants.Int64TypeString:
                case Constants.FloatTypeString:
                case Constants.DoubleTypeString:

                    propertyDeclaration = PropertyDeclaration(GetSimpleTypeSyntax(properties.Type, required), FirstLetterToUpper(name))
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                    break;

                case Constants.StringTypeString:

                    string typeName = string.Empty;

                    if (string.IsNullOrEmpty(properties.Format))
                    {
                        typeName = "string";
                    }
                    else
                    {
                        switch (properties.Format)
                        {
                            case "date":
                            case "datetime":

                                typeName = typeof(DateTime).GetGlobalTypeName();

                                break;

                            case "duration":

                                typeName = typeof(TimeSpan).GetGlobalTypeName();

                                break;
                        }
                    }

                    if (properties.EnumProperty?.Count > 0)
                    {
                        AddEnum(name, properties.EnumProperty, baseClassName);

                        typeName = FirstLetterToUpper(name) + "Type";
                    }

                    propertyDeclaration = PropertyDeclaration(ParseTypeName(typeName), FirstLetterToUpper(name))
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                    break;

                case Constants.ArrayTypeString:

                    if (properties.Items.GetType().IsArray)
                    {
                        break;
                    }
                    V1JSONSchemaProps items = null;

                    if (properties.Items.GetType() == typeof(Dictionary<object, object>))
                    {
                        items = ((Dictionary<object, object>)properties.Items).ToJsonSchemaProps();
                    }
                    else if (properties.Items.GetType() == typeof(V1JSONSchemaProps))
                    {
                        items = ((V1JSONSchemaProps)properties.Items);

                    }
                    switch (items.Type)
                    {
                        case Constants.BooleanTypeString:
                        case Constants.IntegerTypeString:
                        case Constants.NumberTypeString:
                        case Constants.Int32TypeString:
                        case Constants.Int64TypeString:
                        case Constants.FloatTypeString:
                        case Constants.DoubleTypeString:
                        case Constants.StringTypeString:

                            var arrayType = GetSimpleTypeSyntax(items.Type, true);
                            propertyDeclaration = PropertyDeclaration(
                                type:       ParseTypeName($"global::{typeof(List<>).Namespace}.List<{arrayType}>"),
                                identifier: FirstLetterToUpper(name))
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                            break;

                        case Constants.ObjectTypeString:

                            var arrayTypeName = FirstLetterToUpper(name).TrimEnd('s');
                            var arrayReferenceType = $"global::Neon.Operator.Resources.{baseClassName}.{arrayTypeName}";

                            AddObject(
                                arrayTypeName,
                                items,
                                baseClassName);

                            propertyDeclaration = PropertyDeclaration(
                                type:       ParseTypeName($"global::{typeof(List<>).Namespace}.List<{arrayReferenceType}>"),
                                identifier: FirstLetterToUpper(name))
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                            break;

                        default:

                            break;
                    }
                    break;

                case Constants.ObjectTypeString:

                    if (properties.Properties == null)
                    {
                        propertyDeclaration = PropertyDeclaration(
                            type:       ParseTypeName($"global::{typeof(Dictionary<,>).Namespace}.Dictionary<string, string>"),
                            identifier: FirstLetterToUpper(name))
                                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                .AddAccessorListAccessors(
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                        break;

                    }

                    var objTypeName = FirstLetterToUpper(name);
                    var objReferenceType = $"global::Neon.Operator.Resources.{baseClassName}.{objTypeName}";

                    AddObject(
                        name,
                        properties,
                        baseClassName);

                    propertyDeclaration = PropertyDeclaration(
                        type:       ParseTypeName(objReferenceType),
                        identifier: objTypeName)
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                    break;

                default:

                    if (properties.AnyOf != null)
                    {
                        var anyOfType = properties.AnyOf.First();

                        var anyOfTypeName = FirstLetterToUpper(name);
                        var anyOfReferenceType = $"global::Neon.Operator.Resources.{baseClassName}.{anyOfTypeName}";

                        if (anyOfType.Type == Constants.ObjectTypeString)
                        {
                            AddObject(
                                name,
                                anyOfType,
                                baseClassName);

                            propertyDeclaration = PropertyDeclaration(
                                type:       ParseTypeName(anyOfReferenceType),
                                identifier: anyOfTypeName)
                                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                            break;
                        }
                        else
                        {
                            return AddProperty(name, anyOfType, required);
                        }
                    }
                    break;
            }

            if (required)
            {
                propertyDeclaration = propertyDeclaration?
                    .AddAttributeLists(
                        AttributeList(
                            SeparatedList(nodes:
                            [
                                Attribute(name: ParseName(typeof(RequiredAttribute).GetGlobalTypeName()))
                            ])));
            }
            else
            {
                propertyDeclaration = propertyDeclaration?
                        .AddAttributeLists(AttributeList(SeparatedList(nodes: [CreateDefaultNullAttribute()])));
            }

            propertyDeclaration = propertyDeclaration?
                    .AddAttributeLists(AttributeList(SeparatedList(nodes: [CreateJsonPropertyNameAttribute(name)])));

            return propertyDeclaration;
        }

        private void AddEnum(string name, IList<object> properties, string baseClassName)
        {
            name = FirstLetterToUpper(name) + "Type";

            if (this.sources.Contains(name))
            {
                return;
            }

            this.sources.Add(name);

            var stringProperties = properties.Select(s => (string)s).ToList();
            var stringGroups = stringProperties.GroupBy(s => s.ToLower());

            var compilation = CompilationUnit();
            
            var classDeclaration = EnumDeclaration(name)
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                        .AddAttributeLists(AttributeList(SeparatedList(nodes:
                        [
                            Attribute(
                                name: ParseName(typeof(JsonConverterAttribute).GetGlobalTypeName()),
                                argumentList: AttributeArgumentList(
                                    SeparatedList<AttributeArgumentSyntax>(nodes:
                                    [
                                        AttributeArgument(
                                            expression: LiteralExpression(
                                                kind: SyntaxKind.StringLiteralExpression,
                                                token: Token(
                                                    leading: SyntaxTriviaList.Empty,
                                                    kind: SyntaxKind.StringLiteralToken,
                                                    text: $@"typeof({typeof(JsonStringEnumMemberConverter).GetGlobalTypeName()})",
                                                    valueText: nameof(JsonStringEnumMemberConverter),
                                                    trailing: SyntaxTriviaList.Empty)))

                                        ])
                                    )
                                )
                        ])));

            foreach (var value in properties)
            {
                var enumValue = (string)value;

                if (enumValue.ToLower() == enumValue
                    && stringGroups.Where(g => g.Key == enumValue).First().Count() > 1)
                {
                    continue;
                }

                classDeclaration = classDeclaration
                    .AddMembers(
                        EnumMemberDeclaration(FirstLetterToUpper(enumValue))
                            .AddAttributeLists(AttributeList(SeparatedList(nodes: [CreateEnumMemberAttribute(enumValue)]))
                            )
                    );
            }

            compilation = compilation
                   .WithMembers(SingletonList<MemberDeclarationSyntax>(
                       NamespaceDeclaration(ParseName($"Neon.Operator.Resources"))
                       .AddMembers(ClassDeclaration(baseClassName)
                            .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword)))
                            .AddMembers(classDeclaration))));


            var compilationString = compilation.NormalizeWhitespace().ToString();

            context.AddSource(
                $"{name}.g.cs",
                SourceText.From(compilationString, Encoding.UTF8, SourceHashAlgorithm.Sha256)
                );
        }

        private void AddObject(
            string name,
            V1JSONSchemaProps properties,
            string baseClassName = null)
        {
            try
            {
                name = FirstLetterToUpper(name);

                if (this.sources.Contains(name))
                {
                    return;
                }

                this.sources.Add(name);

                var compilation = CompilationUnit();

                var classDeclaration = ClassDeclaration(name)
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));

                foreach (var p in properties.Properties)
                {
                    var required = properties.Required?.Contains(p.Key) ?? false;
                    var property = AddProperty(p.Key, p.Value, required, baseClassName);

                    if (property != null)
                    {
                        classDeclaration = classDeclaration.AddMembers(property);
                    }
                }

                compilation = compilation
                       .WithMembers(SingletonList<MemberDeclarationSyntax>(
                           NamespaceDeclaration(ParseName($"Neon.Operator.Resources"))
                           .AddMembers(ClassDeclaration(baseClassName)
                                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.PartialKeyword)))
                                .AddMembers(classDeclaration))));


                var compilationString = compilation.NormalizeWhitespace().ToString();

                context.AddSource(
                    $"{name}.g.cs",
                    SourceText.From(compilationString, Encoding.UTF8, SourceHashAlgorithm.Sha256)
                    );
            }
            catch (Exception e)
            {
                return;
                // ignore for now.
            }

        }

        private TypeSyntax GetSimpleTypeSyntax(string type, bool required) => type switch
        {
            Constants.StringTypeString => ParseTypeName(typeof(String).GetGlobalTypeName()),
            Constants.BooleanTypeString => ParseTypeName($"{typeof(bool).GetGlobalTypeName()}{(required ? "" : "?")}"),
            Constants.IntegerTypeString => ParseTypeName($"{typeof(long).GetGlobalTypeName()}{(required ? "" : "?")}"),
            Constants.NumberTypeString => ParseTypeName($"{typeof(double).GetGlobalTypeName()}{(required ? "" : "?")}"),
            Constants.Int32TypeString => ParseTypeName($"{typeof(int).GetGlobalTypeName()}{(required ? "" : "?")}"),
            Constants.Int64TypeString => ParseTypeName($"{typeof(long).GetGlobalTypeName()}{(required ? "" : "?")}"),
            Constants.FloatTypeString => ParseTypeName($"{typeof(float).GetGlobalTypeName()}{(required ? "" : "?")}"),
            Constants.DoubleTypeString => ParseTypeName($"{typeof(double).GetGlobalTypeName()}{(required ? "" : "?")}"),
            _ => throw new ArgumentException()
        };

        private string GetClassName(string version, string kind)
        {
            return $"{FirstLetterToUpper(version)}{FirstLetterToUpper(kind)}";
        }

        private string GetSpecClassName(string version, string kind)
        {
            return $"{FirstLetterToUpper(version)}{FirstLetterToUpper(kind)}Spec";
        }

        private string GetStatusClassName(string version, string kind)
        {
            return $"{FirstLetterToUpper(version)}{FirstLetterToUpper(kind)}Status";
        }

        public string FirstLetterToUpper(string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        private AttributeSyntax CreateKubernetesEntityAttribute(
            string group,
            string kind,
            string version,
            string plural)
        {
            return Attribute(
                name: ParseName(typeof(KubernetesEntityAttribute).GetGlobalTypeName()),
                argumentList: AttributeArgumentList(
                    SeparatedList<AttributeArgumentSyntax>(nodes:
                    [
                        AttributeArgument(
                            nameEquals: NameEquals(IdentifierName(nameof(KubernetesEntityAttribute.Group))),
                            nameColon: null,
                            expression: LiteralExpression(
                                kind: SyntaxKind.StringLiteralExpression,
                                token: Token(
                                    leading: SyntaxTriviaList.Empty,
                                    kind: SyntaxKind.StringLiteralToken,
                                    text: $@"""{group}""",
                                    valueText: group,
                                    trailing: SyntaxTriviaList.Empty))),
                        AttributeArgument(
                            nameEquals: NameEquals(IdentifierName(nameof(KubernetesEntityAttribute.Kind))),
                            nameColon: null,
                            expression: LiteralExpression(
                                kind: SyntaxKind.StringLiteralExpression,
                                token: Token(
                                    leading: SyntaxTriviaList.Empty,
                                    kind: SyntaxKind.StringLiteralToken,
                                    text: $@"""{kind}""",
                                    valueText: kind,
                                    trailing: SyntaxTriviaList.Empty))),
                        AttributeArgument(
                            nameEquals: NameEquals(IdentifierName(nameof(KubernetesEntityAttribute.ApiVersion))),
                            nameColon: null,
                            expression: LiteralExpression(
                                kind: SyntaxKind.StringLiteralExpression,
                                token: Token(
                                    leading: SyntaxTriviaList.Empty,
                                    kind: SyntaxKind.StringLiteralToken,
                                    text: $@"""{version}""",
                                    valueText: version,
                                    trailing: SyntaxTriviaList.Empty))),
                        AttributeArgument(
                            nameEquals: NameEquals(IdentifierName(nameof(KubernetesEntityAttribute.PluralName))),
                            nameColon: null,
                            expression: LiteralExpression(
                                kind: SyntaxKind.StringLiteralExpression,
                                token: Token(
                                    leading: SyntaxTriviaList.Empty,
                                    kind: SyntaxKind.StringLiteralToken,
                                    text: $@"""{plural}""",
                                    valueText: plural,
                                    trailing: SyntaxTriviaList.Empty))),

                        ])
                    )
                );
        }

        private AttributeSyntax CreateEnumMemberAttribute(
            string value)
        {
            return Attribute(
                name: ParseName(typeof(EnumMemberAttribute).GetGlobalTypeName()),
                argumentList: AttributeArgumentList(
                    SeparatedList<AttributeArgumentSyntax>(nodes:
                    [
                        AttributeArgument(
                            nameEquals: NameEquals(IdentifierName(nameof(EnumMemberAttribute.Value))),
                            nameColon: null,
                            expression: LiteralExpression(
                                kind: SyntaxKind.StringLiteralExpression,
                                token: Token(
                                    leading: SyntaxTriviaList.Empty,
                                    kind: SyntaxKind.StringLiteralToken,
                                    text: $@"""{value}""",
                                    valueText: value,
                                    trailing: SyntaxTriviaList.Empty)))

                        ])
                    )
                );
        }

        private AttributeSyntax CreateDefaultNullAttribute()
        {
            return Attribute(
                name: ParseName(typeof(DefaultValueAttribute).GetGlobalTypeName()),
                argumentList: AttributeArgumentList(
                    SeparatedList<AttributeArgumentSyntax>(nodes:
                    [
                        AttributeArgument(
                            expression: LiteralExpression(
                                kind: SyntaxKind.NullLiteralExpression,
                                token: Token(
                                    leading: SyntaxTriviaList.Empty,
                                    kind: SyntaxKind.NullKeyword,
                                    text: "null",
                                    valueText: "null",
                                    trailing: SyntaxTriviaList.Empty)))

                        ])
                    )
                ); ;
        }

        private AttributeSyntax CreateJsonPropertyNameAttribute(
            object value)
        {
            return Attribute(
                name: ParseName(typeof(JsonPropertyNameAttribute).GetGlobalTypeName()),
                argumentList: AttributeArgumentList(
                    SeparatedList<AttributeArgumentSyntax>(nodes:
                    [
                        AttributeArgument(
                            expression: LiteralExpression(
                                kind: SyntaxKind.StringLiteralExpression,
                                token: Token(
                                    leading: SyntaxTriviaList.Empty,
                                    kind: SyntaxKind.StringLiteralToken,
                                    text: $@"""{value}""",
                                    valueText: (string)value,
                                    trailing: SyntaxTriviaList.Empty)))

                        ])
                    )
                );
        }
    }
}
