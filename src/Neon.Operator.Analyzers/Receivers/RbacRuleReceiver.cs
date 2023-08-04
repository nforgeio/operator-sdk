// -----------------------------------------------------------------------------
// FILE:	    RbacRuleReceiver.cs
// CONTRIBUTOR: NEONFORGE Team
// COPYRIGHT:   Copyright © 2005-2023 by NEONFORGE LLC.  All rights reserved.
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
using System.Text;

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using System.Linq;
using Neon.Operator.Rbac;
using Neon.Operator.Webhooks;

namespace Neon.Operator.Analyzers
{
    internal class RbacRuleReceiver : ISyntaxReceiver
    {
        public List<AttributeSyntax> AttributesToRegister { get; } = new List<AttributeSyntax>();
        public List<ClassDeclarationSyntax> ClassesToRegister { get; } = new List<ClassDeclarationSyntax>();
        public List<ClassDeclarationSyntax> ControllersToRegister { get; } = new List<ClassDeclarationSyntax>();
        public bool HasMutatingWebhooks { get; set; } = false;
        public bool HasValidatingWebhooks { get; set; } = false;

        private static string[] attributes = new string[]
        {
            "RbacRule"
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

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax)
            {
                try
                {
                    var bases = syntaxNode
                        .DescendantNodes()
                        .OfType<AttributeSyntax>()?
                        .Where(@base => @base.DescendantNodes().OfType<GenericNameSyntax>()
                                .Any(gns => attributes.Contains(gns.Identifier.ValueText))
                                || attributes.Contains(@base.Name.ToString()));


                    if (bases.Count() > 0)
                    {
                        ClassesToRegister.Add((ClassDeclarationSyntax)syntaxNode);
                    }
                }
                catch { }

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
                catch { }
            }

            //if (syntaxNode is ClassDeclarationSyntax)
            //{
            //    var attributeSyntaxes = syntaxNode
            //        .DescendantNodes()
            //        .OfType<AttributeSyntax>()?
            //        .Where(@base => @base.DescendantNodes().OfType<GenericNameSyntax>()
            //                .Any(gns => attributes.Contains(gns.Identifier.ValueText))
            //                || attributes.Contains(@base.Name.ToString()));

            //    AttributesToRegister.AddRange(attributeSyntaxes);

            //    var dns = syntaxNode.DescendantNodes();
            //    var attrs = dns.OfType<AttributeSyntax>();
            //}

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
                        }
                    }
                    catch { }
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
                        }
                    }
                    catch { }
                }
            }
        }
    }
}
