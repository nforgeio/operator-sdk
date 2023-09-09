using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Neon.Operator.Analyzers
{
    internal static class RoslynExtensions
    {
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

            try
            {
                var xmlMeta    = typeSymbol.GetDocumentationCommentXml();

                if (string.IsNullOrEmpty(xmlMeta))
                {
                    return null;
                }

                XElement root = XElement.Parse(xmlMeta);
                var summary = root.Elements("summary");

                var sb = new StringBuilder();
                foreach (var item in summary)
                {
                    sb.AppendLine(item.Value);
                }

                var result = sb.ToString().Trim();

                if (!string.IsNullOrEmpty(result))
                {
                    return result;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
