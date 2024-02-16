//-----------------------------------------------------------------------------
// FILE:	    RoslynExtensions.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:	Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
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
