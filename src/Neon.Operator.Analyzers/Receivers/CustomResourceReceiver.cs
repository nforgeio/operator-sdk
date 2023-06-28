using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using k8s.Models;

namespace Neon.Operator.Analyzers.Receivers
{
    internal class CustomResourceReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClassesToRegister { get; } = new List<ClassDeclarationSyntax>();
        private static string[] classAttributes = new string[]
        {
            nameof(KubernetesEntityAttribute),
            nameof(KubernetesEntityAttribute).Replace("Attribute", ""),

        };

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax)
            {
                var attributeSyntaxes = syntaxNode.DescendantNodes().OfType<AttributeSyntax>();

                foreach (var attribute in attributeSyntaxes)
                {
                    var name = attribute.Name;
                    var nameString = name.ToFullString();

                    if (classAttributes.Contains(nameString))
                    {
                        ClassesToRegister.Add((ClassDeclarationSyntax)syntaxNode);
                    }
                }
            }
        }
    }
}
