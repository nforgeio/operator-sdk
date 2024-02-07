// -----------------------------------------------------------------------------
// FILE:	    OperatorAnalyzerConfigOptionsProvider.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Test.Analyzers
{
    public class OperatorAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private AnalyzerConfigOptions _options;
        public override AnalyzerConfigOptions GlobalOptions => _options;

        public void SetOptions(AnalyzerConfigOptions options)
        {
            _options = options;
        }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
        {
            return GlobalOptions;
        }

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
        {
            return GlobalOptions;
        }
    }

    public class OperatorAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
        {
            return Options.TryGetValue(key, out value);
        }
    }
}
