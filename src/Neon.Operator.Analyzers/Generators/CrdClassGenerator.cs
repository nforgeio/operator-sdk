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
using System.Linq;
using System.Text;

using k8s;
using k8s.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Newtonsoft.Json.Linq;

using YamlDotNet.Core;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Neon.Operator.Analyzers.Generators
{
    [Generator]
    public class CrdClassGenerator : ISourceGenerator
    {
        private HashSet<string> sources;
        private GeneratorExecutionContext context;

        private static HashSet<string> usings = new HashSet<string>()
        {
            "System.Collections.Generic",
            "System.ComponentModel",
            "System.ComponentModel.DataAnnotations",
            "Neon.Operator.Attributes",
            "Neon.Operator.Resources",
            "k8s",
            "k8s.Models",
        };

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
                    crd = KubernetesYaml.Deserialize<V1CustomResourceDefinition>(file.GetText()?.ToString());
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

                    compilation = compilation.AddUsings(usings.Select(u => UsingDirective(IdentifierName(u))).ToArray());

                    var crdClass = ClassDeclaration(className)
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                        .AddBaseListTypes(
                            SimpleBaseType(ParseTypeName("IKubernetesObject<V1ObjectMeta>")))
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
                            PropertyDeclaration(ParseTypeName("string"), nameof(KubernetesEntityAttribute.ApiVersion))
                                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                .AddAccessorListAccessors(
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))),
                            PropertyDeclaration(ParseTypeName("string"), nameof(KubernetesEntityAttribute.Kind))
                                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                .AddAccessorListAccessors(
                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken))),
                            PropertyDeclaration(ParseTypeName(nameof(V1ObjectMeta)), "Metadata")
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
                        var refTypeName = $"global::Neon.Operator.Resources.{specClassName}";

                        crdClass = crdClass
                            .AddBaseListTypes(
                                SimpleBaseType(ParseTypeName($"ISpec<{refTypeName}>")))
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
                            version.Schema.OpenAPIV3Schema.Properties["spec"]);
                    }

                    if (version.Schema.OpenAPIV3Schema.Properties.ContainsKey("status"))
                    {
                        var statusClassName = GetStatusClassName(version: version.Name, kind: crd.Spec.Names.Kind);
                        var refTypeName = $"global::Neon.Operator.Resources.{statusClassName}";

                        crdClass = crdClass
                            .AddBaseListTypes(
                                SimpleBaseType(ParseTypeName($"IStatus<{refTypeName}>")))
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
                            version.Schema.OpenAPIV3Schema.Properties["status"]);
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
            bool required)
        {
            switch (properties.Type)
            {
                case Constants.BooleanTypeString:
                case Constants.IntegerTypeString:
                case Constants.NumberTypeString:
                case Constants.Int32TypeString:
                case Constants.Int64TypeString:
                case Constants.FloatTypeString:
                case Constants.DoubleTypeString:

                    return PropertyDeclaration(GetSimpleTypeSyntax(properties.Type, required), FirstLetterToUpper(name))
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

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

                                typeName = nameof(DateTime);

                                break;

                            case "duration":

                                typeName = nameof(TimeSpan);

                                break;
                        }
                    }

                    return PropertyDeclaration(ParseTypeName(typeName), FirstLetterToUpper(name))
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                case Constants.ArrayTypeString:

                    if (properties.Items.GetType().IsArray)
                    {
                        break;
                    }
                    if (properties.Items.GetType() == typeof(Dictionary<object, object>))
                    {
                        var items = ((Dictionary<object, object>)properties.Items).ToJsonSchemaProps();

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
                                return PropertyDeclaration(ParseTypeName($"List<{arrayType}>"), FirstLetterToUpper(name))
                                                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                                .AddAccessorListAccessors(
                                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                            case Constants.ObjectTypeString:

                                var arrayTypeName = FirstLetterToUpper(name).TrimEnd('s');
                                var arrayReferenceType = $"global::Neon.Operator.Resources.{arrayTypeName}";

                                AddObject(
                                   arrayTypeName,
                                   items);

                                return PropertyDeclaration(ParseTypeName($"List<{arrayReferenceType}>"), FirstLetterToUpper(name))
                                                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                                .AddAccessorListAccessors(
                                                    AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                                    AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                        }
                    }

                    break;

                case Constants.ObjectTypeString:

                    if (properties.Properties == null)
                    {
                        return PropertyDeclaration(ParseTypeName("Dictionary<string, string>"), FirstLetterToUpper(name))
                                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                        .AddAccessorListAccessors(
                                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                            AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                    }

                    var objTypeName = FirstLetterToUpper(name);
                    var objReferenceType = $"global::Neon.Operator.Resources.{objTypeName}";

                    AddObject(name, properties);

                    return PropertyDeclaration(ParseTypeName(objReferenceType), objTypeName)
                                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                    .AddAccessorListAccessors(
                                        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));

                default:

                    if (properties.AnyOf != null)
                    {
                        var anyOfType = properties.AnyOf.First();

                        var anyOfTypeName = FirstLetterToUpper(name);
                        var anyOfReferenceType = $"global::Neon.Operator.Resources.{anyOfTypeName}";

                        if (anyOfType.Type == Constants.ObjectTypeString)
                        {
                            AddObject(name, anyOfType);

                            return PropertyDeclaration(ParseTypeName(anyOfReferenceType), anyOfTypeName)
                                            .AddModifiers(Token(SyntaxKind.PublicKeyword))
                                            .AddAccessorListAccessors(
                                                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
                                                AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
                        }
                        else
                        {
                            return AddProperty(name, anyOfType, required);
                        }
                    }
                    break;
            }

            return null;
        }

        private void AddObject(
            string name,
            V1JSONSchemaProps properties)
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

                compilation = compilation.AddUsings(usings.Select(u => UsingDirective(IdentifierName(u))).ToArray());

                var classDeclaration = ClassDeclaration(FirstLetterToUpper(name))
                        .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)));

                foreach (var p in properties.Properties)
                {
                    var required = properties.Required?.Contains(p.Key) ?? false;
                    var property = AddProperty(p.Key, p.Value, required);

                    if (property != null)
                    {
                        classDeclaration = classDeclaration.AddMembers(property);
                    }
                }

                compilation = compilation
                       .WithMembers(SingletonList<MemberDeclarationSyntax>(
                           NamespaceDeclaration(ParseName("Neon.Operator.Resources"))
                           .AddMembers(classDeclaration)));


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
            Constants.StringTypeString => ParseTypeName("string"),
            Constants.BooleanTypeString => ParseTypeName($"bool{(required ? "" : "?")}"),
            Constants.IntegerTypeString => ParseTypeName($"long{(required ? "" : "?")}"),
            Constants.NumberTypeString => ParseTypeName($"double{(required ? "" : "?")}"),
            Constants.Int32TypeString => ParseTypeName($"int{(required ? "" : "?")}"),
            Constants.Int64TypeString => ParseTypeName($"long{(required ? "" : "?")}"),
            Constants.FloatTypeString => ParseTypeName($"float{(required ? "" : "?")}"),
            Constants.DoubleTypeString => ParseTypeName($"double{(required ? "" : "?")}"),
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
                name: ParseName(nameof(KubernetesEntityAttribute)),
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
    }
}
