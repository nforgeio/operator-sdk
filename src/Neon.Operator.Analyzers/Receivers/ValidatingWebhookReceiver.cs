using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Neon.Operator.Analyzers
{
    public class ValidatingWebhookReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ValidatingWebhooks { get; }
            = new();

        private List<string> baseNames = new List<string>()
        {
            "IValidatingWebhook",
            "ValidatingWebhookBase",
        };

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax)
            {
                try
                {
                    var bases = syntaxNode
                        .DescendantNodes()
                        .OfType<BaseListSyntax>()?
                        .Where(@base => @base.DescendantNodes().OfType<GenericNameSyntax>()
                                .Any(gns => baseNames.Contains(gns.Identifier.ValueText)));


                    if (bases.Count() > 0)
                    {
                        ValidatingWebhooks.Add((ClassDeclarationSyntax)syntaxNode);
                    }
                }
                catch { }
            }
        }
    }
}
