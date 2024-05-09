// -----------------------------------------------------------------------------
// FILE:	    MutatingWebhookReceiver.cs
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

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Neon.Operator.Attributes;

namespace Neon.Operator.Analyzers
{
    public class MutatingWebhookReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> MutatingWebhooks { get; } = new();
        public List<AttributeSyntax> Attributes { get; } = new List<AttributeSyntax>();

        private List<string> baseNames = new List<string>()
        {
            "IMutatingWebhook",
            "MutatingWebhookBase",
        };

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax)
            {
                try
                {
                    var attributeSyntaxes = syntaxNode.DescendantNodes().OfType<AttributeSyntax>();

                    if (attributeSyntaxes.Any(attributeSyntax => attributeSyntax.Name.ToFullString() == nameof(IgnoreAttribute) ||
                        attributeSyntaxes.Any(attributeSyntax => attributeSyntax.Name.ToFullString() == nameof(IgnoreAttribute).Replace("Attribute", ""))))
                    {
                        return;
                    }

                    var bases = syntaxNode
                        .DescendantNodes()
                        .OfType<BaseListSyntax>()?
                        .Where(@base => @base.DescendantNodes().OfType<GenericNameSyntax>()
                            .Any(genericNameSyntax => baseNames.Contains(genericNameSyntax.Identifier.ValueText)));

                    if (bases.Count() > 0)
                    {
                        MutatingWebhooks.Add((ClassDeclarationSyntax)syntaxNode);
                    }
                }
                catch
                {
                    // Intentionally ignored
                }
            }

            if (syntaxNode is CompilationUnitSyntax)
            {
                try
                {
                    var attributeList = ((CompilationUnitSyntax)syntaxNode).AttributeLists;

                    foreach (var attributeSyntax in attributeList)
                    {
                        var attributes = attributeSyntax.DescendantNodes().OfType<AttributeSyntax>();

                        foreach (var attribyteSyntax in attributes)
                        {
                            var name       = attribyteSyntax.Name;
                            var nameString = name.ToFullString();

                            if (Constants.AssemblyAttributeNames.Contains(nameString) ||
                                nameString.StartsWith("OwnedEntity") ||
                                nameString.StartsWith("RequiredEntity"))
                            {
                                Attributes.Add(attribyteSyntax);
                            }
                        }
                    }
                }
                catch
                {
                    // Intentionally ignored
                }
            }
        }
    }
}
