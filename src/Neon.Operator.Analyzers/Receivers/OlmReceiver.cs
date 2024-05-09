// -----------------------------------------------------------------------------
// FILE:	    OlmReceiver.cs
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
using Neon.Operator.OperatorLifecycleManager;

namespace Neon.Operator.Analyzers.Receivers
{
    public class OlmReceiver : ISyntaxReceiver
    {
        public List<ClassDeclarationSyntax> ClassesToRegister { get; }  = new();
        public List<ClassDeclarationSyntax> ControllersToRegister { get; } = new();
        public List<ClassDeclarationSyntax> Webhooks { get; } = new();
        public bool HasMutatingWebhooks { get; set; } = false;
        public bool HasValidatingWebhooks { get; set; } = false;

        private static string[] attributes = 
        {
            "RbacRule",
            "Webhook",
            "WebhookRule",
        };

        private List<string> baseNames = new List<string>()
        {
            "IResourceController",
            "ResourceControllerBase",
        };

        private List<string> mutatingWebhookBaseNames = new List<string>()
        {
            "IMutatingWebhook",
            "MutatingWebhookBase",
        };

        private List<string> validatingWebhookBaseNames = new List<string>()
        {
            "IValidatingWebhook",
            "ValidatingWebhookBase",
        };

        public List<AttributeSyntax> Attributes { get; } = new List<AttributeSyntax>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is CompilationUnitSyntax)
            {
                try
                {
                    var attributeList = ((CompilationUnitSyntax)syntaxNode).AttributeLists;

                    foreach (var a in attributeList)
                    {
                        var attributes = a.DescendantNodes().OfType<AttributeSyntax>();

                        foreach (var attr in attributes)
                        {
                            var name       = attr.Name;
                            var nameString = name.ToFullString();

                            if (Constants.AssemblyAttributeNames.Contains(nameString) ||
                                nameString.StartsWith("OwnedEntity") ||
                                nameString.StartsWith("RequiredEntity"))
                            {
                                Attributes.Add(attr);
                            }
                        }
                    }
                }
                catch
                {
                    // Intentionally ignored
                }
            }

            if (syntaxNode is ClassDeclarationSyntax)
            {
                try
                {
                    var bases = syntaxNode
                        .DescendantNodes()
                        .OfType<AttributeSyntax>()?
                        .Where(@base => @base.DescendantNodes().OfType<GenericNameSyntax>()
                            .Any(gns => attributes.Contains(gns.Identifier.ValueText)) || attributes.Contains(@base.Name.ToString()));

                    if (bases.Count() > 0)
                    {
                        ClassesToRegister.Add((ClassDeclarationSyntax)syntaxNode);
                    }
                }
                catch
                {
                    // Intentionally ignored
                }

                try
                {
                    var bases = syntaxNode
                        .DescendantNodes()
                        .OfType<BaseListSyntax>()?
                        .Where(@base => @base.DescendantNodes().OfType<GenericNameSyntax>()
                            .Any(gns => baseNames.Contains(gns.Identifier.ValueText)));


                    if (bases.Count() > 0)
                    {
                        ControllersToRegister.Add((ClassDeclarationSyntax)syntaxNode);
                    }
                }
                catch
                {
                    // Intentionally ignored
                }
            }

            if (!HasMutatingWebhooks)
            {
                if (syntaxNode is ClassDeclarationSyntax)
                {
                    try
                    {
                        var bases = syntaxNode
                            .DescendantNodes()
                            .OfType<BaseListSyntax>()?
                            .Where(@base => @base.DescendantNodes().OfType<GenericNameSyntax>()
                                .Any(gns => mutatingWebhookBaseNames.Contains(gns.Identifier.ValueText)));

                        if (bases.Count() > 0)
                        {
                            HasMutatingWebhooks = true;
                            Webhooks.Add((ClassDeclarationSyntax)syntaxNode);
                        }
                    }
                    catch
                    {
                        // Intyentionally ignored
                    }
                }
            }

            if (!HasValidatingWebhooks)
            {
                if (syntaxNode is ClassDeclarationSyntax)
                {
                    try
                    {
                        var bases = syntaxNode
                            .DescendantNodes()
                            .OfType<BaseListSyntax>()?
                            .Where(@base => @base.DescendantNodes().OfType<GenericNameSyntax>()
                                .Any(gns => validatingWebhookBaseNames.Contains(gns.Identifier.ValueText)));


                        if (bases.Count() > 0)
                        {
                            HasValidatingWebhooks = true;
                            Webhooks.Add((ClassDeclarationSyntax)syntaxNode);
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
}
