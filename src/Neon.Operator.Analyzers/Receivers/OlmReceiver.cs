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

using Neon.Kubernetes.Resources.OperatorLifecycleManager;

namespace Neon.Operator.Analyzers.Receivers
{
    public class OlmReceiver : ISyntaxReceiver
    {
        private List<string> attributeNames = [
            "OperatorName",
            nameof(OperatorNameAttribute),
            "OperatorDisplayName",
            nameof(OperatorDisplayNameAttribute),
            "OperatorDescription",
            ];

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
                            var name = attr.Name;
                            var nameString = name.ToFullString();

                            if (attributeNames.Contains(nameString))
                            {
                                Attributes.Add(attr);
                            }
                        }
                    }
                }
                catch { }
            }
        }
    }
}
