//-----------------------------------------------------------------------------
// FILE:	    RoslynExtensions.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
using System.Xml;

using Microsoft.CodeAnalysis;

using Neon.Roslyn;

using Neon.Roslyn;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers
{
    internal static class RoslynExtensions
    {
        public static T GetExpressionValue<T>(this ExpressionSyntax syntax, MetadataLoadContext metadataLoadContext)
        {
            if (syntax is LiteralExpressionSyntax)
            {
                return (T)((LiteralExpressionSyntax)syntax).Token.Value;
            }
            if (syntax is InvocationExpressionSyntax)
            {
                var expression = ((InvocationExpressionSyntax)syntax).Expression;

                if (expression is IdentifierNameSyntax)
                {
                    if (((IdentifierNameSyntax)expression).Identifier.ValueText == "nameof")
                    {
                        var arg = ((InvocationExpressionSyntax)syntax).ArgumentList.Arguments.First();
                        return (T)arg.GetLastToken().Value;
                    }
                }
                return default;
            }
            if (syntax is MemberAccessExpressionSyntax)
            {
                var s = (MemberAccessExpressionSyntax)syntax;
                var ns = s.GetNamespace();
                var fullName = s.ToFullString();
                var className = fullName.Substring(0, fullName.LastIndexOf('.'));
                var propName = s.Name.Identifier.ValueText;

                var c = metadataLoadContext.ResolveType($"{className}");

                if (c == null)
                {
                    var usings = s
                        .Ancestors()
                        .OfType<CompilationUnitSyntax>()
                        .FirstOrDefault()?
                        .DescendantNodes()
                        .OfType<UsingDirectiveSyntax>()
                        .ToList();

                    foreach (var u in usings)
                    {
                        var usingNs = ((UsingDirectiveSyntax)u).NamespaceOrType.ToFullString();
                        c = metadataLoadContext.ResolveType($"{usingNs}.{className}");

                        if (c != null)
                        {
                            break;
                        }
                    }
                }

                var member = c.GetMembers().Where(m => m.Name == propName).FirstOrDefault();
                return (T)((RoslynFieldInfo)member).FieldSymbol.ConstantValue;
            }

            return default;
        }

        public static string GetFullMetadataName(this ISymbol s)
        {
            if (s == null || IsRootNamespace(s))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(s.MetadataName);
            var last = s;

            s = s.ContainingSymbol;

            while (!IsRootNamespace(s))
            {
                if (s is ITypeSymbol && last is ITypeSymbol)
                {
                    sb.Insert(0, '+');
                }
                else
                {
                    sb.Insert(0, '.');
                }

                sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                //sb.Insert(0, s.MetadataName);
                s = s.ContainingSymbol;
            }

            return sb.ToString();
        }

        private static bool IsRootNamespace(ISymbol symbol)
        {
            INamespaceSymbol s = null;
            return ((s = symbol as INamespaceSymbol) != null) && s.IsGlobalNamespace;
        }

        public static string GetNamespace(this SyntaxNode s) =>
            s.Parent switch
            {
                NamespaceDeclarationSyntax namespaceDeclarationSyntax => namespaceDeclarationSyntax.Name.ToString(),
                null => string.Empty, // or whatever you want to do
                _ => GetNamespace(s.Parent)
            };

        public static string GetSummary(this ISymbol typeSymbol)
        {
            if (typeSymbol == null)
            {
                return null;
            }
            var xml = typeSymbol.GetDocumentationCommentXml();

            var description = DocumentationComment.From(xml, Environment.NewLine).SummaryText.Trim();

            if (string.IsNullOrWhiteSpace(description))
            {
                return null;
            }

            return description;
        }
    }
}
