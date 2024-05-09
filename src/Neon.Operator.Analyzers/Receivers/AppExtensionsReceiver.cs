// -----------------------------------------------------------------------------
// FILE:	    AppExtensionsReceiver.cs
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Neon.Operator.Analyzers.Receivers
{
    internal class AppExtensionsReceiver : ISyntaxReceiver
    {
        //---------------------------------------------------------------------
        // Static members

        private static string[] classAttributes = 
        {
            "Webhook"
        };

        //---------------------------------------------------------------------
        // Instance members

        public List<ClassDeclarationSyntax> ClassesToRegister { get; } = new List<ClassDeclarationSyntax>();
        public bool DoesAddOperator { get; private set; } = false;

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is InvocationExpressionSyntax)
            {
                try
                {
                    InvocationExpressionSyntax methodSyntax = (InvocationExpressionSyntax)syntaxNode;

                    if (((MemberAccessExpressionSyntax)methodSyntax.Expression).Name.Identifier.ValueText == "UseKubernetesOperator")
                    {
                        DoesAddOperator = true;
                    }
                }
                catch
                {
                    // Intentionally ignored.
                }
            }

            if (syntaxNode is ClassDeclarationSyntax)
            {
                var attributeSyntaxes = syntaxNode.DescendantNodes().OfType<AttributeSyntax>();

                foreach (var attribute in attributeSyntaxes)
                {
                    var name       = attribute.Name;
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
