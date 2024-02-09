using System;

using Microsoft.CodeAnalysis;

using Neon.Roslyn;

namespace Neon.Operator.Analyzers
{
    internal static class RoslynExtensions
    {
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
