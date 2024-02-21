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
using System.Diagnostics;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Neon.Operator.Analyzers.Generators;
using Neon.Roslyn;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers
{
    internal static class RoslynExtensions
    {
        public static object GetExpressionValue(this ExpressionSyntax syntax, MetadataLoadContext metadataLoadContext)
        {
            if (syntax is LiteralExpressionSyntax)
            {
                return ((LiteralExpressionSyntax)syntax).Token.Value;
            }
            if (syntax is InvocationExpressionSyntax)
            {
                var expression = ((InvocationExpressionSyntax)syntax).Expression;

                if (expression is IdentifierNameSyntax)
                {
                    if (((IdentifierNameSyntax)expression).Identifier.ValueText == "nameof")
                    {
                        var arg = ((InvocationExpressionSyntax)syntax).ArgumentList.Arguments.First();
                        return arg.GetLastToken().Value;
                    }
                }

                return null;
            }
            if (syntax is MemberAccessExpressionSyntax)
            {
                var s = (MemberAccessExpressionSyntax)syntax;

                var c = metadataLoadContext.ResolveType((IdentifierNameSyntax)s.Expression);

                var member = c.GetMembers().Where(m => m.Name == s.Name.Identifier.ValueText).FirstOrDefault();

                return ((RoslynFieldInfo)member).FieldSymbol.ConstantValue;
            }
            if (syntax is BinaryExpressionSyntax
                    && ((BinaryExpressionSyntax)syntax).Kind() == SyntaxKind.BitwiseOrExpression)
            {
                return ((BinaryExpressionSyntax)syntax).GetEnumValue(metadataLoadContext);
            }

            return null;
        }

        public static T GetExpressionValue<T>(this ExpressionSyntax syntax, MetadataLoadContext metadataLoadContext)
        {
            return (T)syntax.GetExpressionValue(metadataLoadContext);
        }

        public static int GetEnumValue(this BinaryExpressionSyntax s, MetadataLoadContext metadataLoadContext)
        {
            int left;
            int right;
            if (s.Left is BinaryExpressionSyntax)
            {
                left = ((BinaryExpressionSyntax)s.Left).GetEnumValue(metadataLoadContext);
            }
            else
            {
                left = s.Left.GetExpressionValue<int>(metadataLoadContext);
            }
            if (s.Right is BinaryExpressionSyntax)
            {
                right = ((BinaryExpressionSyntax)s.Right).GetEnumValue(metadataLoadContext);
            }
            else
            {
                right = s.Right.GetExpressionValue<int>(metadataLoadContext);
            }

            return left | right;
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

        public static T GetAttribute<T>(
            MetadataLoadContext metadataLoadContext,
            Compilation compilation,
            List<AttributeSyntax> attributes)
        {
            try
            {
                AttributeSyntax syntax = null;

                syntax = attributes
                    .Where(a => a.Name.ToFullString() == typeof(T).Name)
                    .FirstOrDefault();

                if (syntax == null)
                {
                    var name = typeof(T).Name.Replace("Attribute", "");

                    syntax = attributes
                        .Where(a => a.Name.ToFullString() == name)
                        .FirstOrDefault();
                }

                if (syntax == null)
                {
                    return default(T);
                }

                return syntax.GetCustomAttribute<T>(metadataLoadContext, compilation);
            }
            catch
            {
                return default(T);
            }
        }

        public static IEnumerable<T> GetAttributes<T>(
            MetadataLoadContext   metadataLoadContext,
            Compilation           compilation,
            List<AttributeSyntax> attributes)
        {
            try
            {
                IEnumerable<AttributeSyntax> syntax = null;

                syntax = attributes
                    .Where(a => a.Name.ToFullString() == typeof(T).Name);

                if (syntax == null || syntax.Count() == 0)
                {
                    var name = typeof(T).Name.Replace("Attribute", "");

                    syntax = attributes
                        .Where(a => a.Name.ToFullString() == name);
                }

                if (syntax == null || syntax.Count() == 0)
                {
                    return null;
                }

                return syntax.Select(s => s.GetCustomAttribute<T>(metadataLoadContext, compilation));
            }
            catch
            {
                return Enumerable.Empty<T>();
            }
        }
    }
}
