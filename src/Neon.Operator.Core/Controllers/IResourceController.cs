//-----------------------------------------------------------------------------
// FILE:	    IResourceController.cs
// CONTRIBUTOR: Jeff Lill, Marcus Bowyer
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
using System.Threading.Tasks;

using Neon.Operator.Attributes;

namespace Neon.Operator.Controllers
{
    /// <summary>
    /// Describes the interface used to implement Neon based operator controllers.
    /// </summary>
    [OperatorComponent(ComponentType = OperatorComponentType.Controller)]
    public interface IResourceController
    {
        /// <summary>
        /// Another way to specify the Field selector.
        /// </summary>
        string FieldSelector { get; set; }

        /// <summary>
        /// Another way to specify the Label Selector.
        /// </summary>
        string LabelSelector { get; set; }

        /// <summary>
        /// Starts the controller.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public Task StartAsync(IServiceProvider serviceProvider);
    }
}
