////-----------------------------------------------------------------------------
//// FILE:	    ServiceCollectionExtensions.cs
//// CONTRIBUTOR: Marcus Bowyer
//// COPYRIGHT:	Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
////
//// Licensed under the Apache License, Version 2.0 (the "License");
//// you may not use this file except in compliance with the License.
//// You may obtain a copy of the License at
////
////     http://www.apache.org/licenses/LICENSE-2.0
////
//// Unless required by applicable law or agreed to in writing, software
//// distributed under the License is distributed on an "AS IS" BASIS,
//// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//// See the License for the specific language governing permissions and
//// limitations under the License.

//using Microsoft.Extensions.DependencyInjection;
//using Neon.Operator.Builder;
//using System;

//namespace Neon.Operator
//{
//    /// <summary>
//    /// Kubernetes operator <see cref="IServiceCollection"/> extension methods.
//    /// </summary>
//    public static class ServiceCollectionExtensions
//    {
//        /// <summary>
//        /// Adds Kubernetes operator to the service collection.
//        /// </summary>
//        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
//        /// <returns>The <see cref="OperatorBuilder"/>.</returns>
//        public static IOperatorBuilder AddKubernetesOperator(this IServiceCollection services)
//        {
//            return new OperatorBuilder(services).AddOperatorBase().AddServiceComponents();
//        }

//        /// <summary>
//        /// Adds Kubernetes operator to the service collection.
//        /// </summary>
//        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
//        /// <param name="options">Optional options</param>
//        /// <returns>The <see cref="OperatorBuilder"/>.</returns>
//        public static IOperatorBuilder AddKubernetesOperator(this IServiceCollection services, Action<OperatorSettings> options)
//        {
//            var settings = new OperatorSettings();
//            options?.Invoke(settings);

//            services.AddSingleton(settings);

//            return new OperatorBuilder(services).AddOperatorBase().AddServiceComponents();
//        }
//    }
//}
