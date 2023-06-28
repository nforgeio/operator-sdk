using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Neon.Operator.Analyzers.Receivers
{
    internal class ServiceExtensionsReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClassesToRegister { get; } = new();
        private List<string> baseNames = new List<string>()
        {
            "IMutatingWebhook",
            "MutatingWebhookBase",
            "IValidatingWebhook",
            "ValidatingWebhookBase",
            "IResourceController",
            "ResourceControllerBase",
            "IResourceFinalizer",
            "ResourceFinalizerBase",
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
                        ClassesToRegister.Add((ClassDeclarationSyntax)syntaxNode);
                    }
                }
                catch { }
            }
        }
    }
}
