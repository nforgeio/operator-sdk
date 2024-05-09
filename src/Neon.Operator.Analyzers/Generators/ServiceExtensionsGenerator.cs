// -----------------------------------------------------------------------------
// FILE:	    ServiceExtensionsGenerator.cs
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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Neon.Operator.Analyzers.Receivers;
using Neon.Operator.Attributes;
using Neon.Operator.Controllers;
using Neon.Operator.Finalizers;
using Neon.Operator.Webhooks;

using Neon.Roslyn;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers
{
    [Generator]
    public class ServiceExtensionsGenerator : ISourceGenerator
    {
        private List<Type> baseNames;
        public void Initialize(GeneratorInitializationContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new ServiceExtensionsReceiver());

            baseNames = new List<Type>()
            {
                typeof(IMutatingWebhook<>),
                typeof(MutatingWebhookBase<>),
                typeof(IValidatingWebhook<>),
                typeof(ValidatingWebhookBase<>),
                typeof(IResourceController),
                typeof(IResourceController),
                typeof(IResourceController),
                typeof(IResourceController<>),
                typeof(ResourceControllerBase<>),
                typeof(IResourceFinalizer<>),
                typeof(ResourceFinalizerBase<>)
            };
        }

        public Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            Assembly assembly = null;

            try
            {
                var runtimeDependencies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
                var targetAssembly = runtimeDependencies
                    .FirstOrDefault(assemblyPath => Path.GetFileNameWithoutExtension(assemblyPath).Equals(assemblyName.Name, StringComparison.InvariantCultureIgnoreCase));

                if (!String.IsNullOrEmpty(targetAssembly))
                {
                    assembly = Assembly.LoadFrom(targetAssembly);
                }
            }
            catch (Exception)
            {
                // Intentionally ignored.
            }

            return assembly;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.IsTestProject", out var isTestProject))
            {
                if (bool.TryParse(isTestProject, out var isTestProjectBool))
                {
                    if (isTestProjectBool == true)
                    {
                        return;
                    }
                }
            }

            var metadataLoadContext = new MetadataLoadContext(context.Compilation);
            var namedTypeSymbols    = context.Compilation.GetNamedTypeSymbols();

            if (((ServiceExtensionsReceiver)context.SyntaxReceiver)?.ClassesToRegister.Any() == true)
            {
                var sb = new StringBuilder();

                var usings = new HashSet<string>()
                {
                    "System",
                    "Microsoft.Extensions.DependencyInjection",
                    "Neon.Operator",
                    "Neon.Operator.Builder",
                    "Neon.Operator.ResourceManager"
            };

                sb.Append($@"
namespace Neon.Operator
{{
    /// <summary>
    /// Kubernetes operator <see cref=""IServiceCollection""/> extension methods.
    /// </summary>
    public static class ServiceCollectionExtensions
    {{
        /// <summary>
        /// Adds Kubernetes operator to the service collection.
        /// </summary>
        /// <param name=""services"">The <see cref=""IServiceCollection""/>.</param>
        /// <returns>The <see cref=""OperatorBuilder""/>.</returns>
        public static IOperatorBuilder AddKubernetesOperator(this IServiceCollection services)
        {{
            services.AddControllers();

            return new OperatorBuilder(services).AddOperatorBase().AddServiceComponents();
        }}

        /// <summary>
        /// Adds Kubernetes operator to the service collection.
        /// </summary>
        /// <param name=""services"">The <see cref=""IServiceCollection""/>.</param>
        /// <param name=""options"">Optional options</param>
        /// <returns>The <see cref=""OperatorBuilder""/>.</returns>
        public static IOperatorBuilder AddKubernetesOperator(this IServiceCollection services, Action<OperatorSettings> options)
        {{
            var settings = new OperatorSettings();
            options?.Invoke(settings);

            services.AddSingleton(settings);
            services.AddControllers();

            return new OperatorBuilder(services).AddOperatorBase().AddServiceComponents();
        }}

        /// <summary>
        /// Adds the service components to the operator.
        /// </summary>
        /// <param name=""builder"">The <see cref=""IOperatorBuilder""/>.</param>
        /// <returns>The <see cref=""OperatorBuilder""/>.</returns>
        public static IOperatorBuilder AddServiceComponents(this IOperatorBuilder builder)
        {{");
                foreach (var component in ((ServiceExtensionsReceiver)context.SyntaxReceiver)?.ClassesToRegister)
                {
                    var componentNs = component.GetNamespace();

                    usings.Add(componentNs);

                    var componentSystemType = metadataLoadContext.ResolveType($"{componentNs}.{component.Identifier.ValueText}");

                    var ignoreAttribute = componentSystemType.GetCustomAttribute<IgnoreAttribute>();

                    if (ignoreAttribute != null)
                    {
                        continue;
                    }

                    var interfaces = componentSystemType.GetInterfaces().ToList();

                    if (componentSystemType.BaseType != null)
                    {
                        Type baseType = componentSystemType.BaseType;
                            
                        while (baseType != null)
                        {
                            interfaces.AddRange(baseType.GetInterfaces());
                            baseType = baseType.BaseType;
                        }
                            
                    }

                    var componentInterfaceType = (OperatorComponentType)interfaces
                        .Where(@interface =>@interface.IsGenericType && baseNames.Any(bn => bn.FullName == @interface.FullName))
                        .Select(type => type.CustomAttributes?
                            .Where(a => a.AttributeType.Equals(typeof(OperatorComponentAttribute)))
                            .FirstOrDefault().NamedArguments.First().TypedValue)
                        .FirstOrDefault().Value.Value;

                    var assemblySymbol = context.Compilation.SourceModule.ReferencedAssemblySymbols.Last();
                    var members        = assemblySymbol.GlobalNamespace.GetNamespaceMembers();

                    var typeMembers = context.Compilation.SourceModule.ReferencedAssemblySymbols
                        .SelectMany(ras => ras.GlobalNamespace.GetNamespaceMembers())
                        .SelectMany(nsm => nsm.GetTypeMembers());

                    var componentEntityType = component
                        .DescendantNodes()?
                        .OfType<BaseListSyntax>()?
                        .Where(dn => dn.DescendantNodes()?
                            .OfType<GenericNameSyntax>()?
                            .Any(gns => gns.Identifier.ValueText.EndsWith(typeof(IMutatingWebhook<>).Name.Replace("`1", "")) ||
                            gns.Identifier.ValueText.EndsWith(typeof(MutatingWebhookBase<>).Name.Replace("`1", "")) ||
                            gns.Identifier.ValueText.EndsWith(typeof(IValidatingWebhook<>).Name.Replace("`1", "")) ||
                            gns.Identifier.ValueText.EndsWith(typeof(ValidatingWebhookBase<>).Name.Replace("`1", "")) ||
                            gns.Identifier.ValueText.EndsWith(typeof(IResourceController).Name) ||
                            gns.Identifier.ValueText.EndsWith(typeof(IResourceController<>).Name.Replace("`1", "")) ||
                            gns.Identifier.ValueText.EndsWith(typeof(ResourceControllerBase<>).Name.Replace("`1", "")) ||
                            gns.Identifier.ValueText.EndsWith(typeof(IResourceFinalizer<>).Name.Replace("`1", "")) ||
                            gns.Identifier.ValueText.EndsWith(typeof(ResourceFinalizerBase<>).Name.Replace("`1", ""))) == true)
                        .FirstOrDefault();

                    var componentTypeIdentifier           = componentEntityType.DescendantNodes().OfType<IdentifierNameSyntax>().Single();
                    var componentTypeIdentifierNamespace  = componentTypeIdentifier.GetNamespace();
                    var componentEntityTypeIdentifier     = namedTypeSymbols.Where(ntm => ntm.MetadataName == componentTypeIdentifier.Identifier.ValueText).SingleOrDefault();
                    var componentEntityFullyQualifiedName = componentEntityTypeIdentifier.ToDisplayString(DisplayFormat.NameAndContainingTypesAndNamespaces);
                    var entitySystemType                  = metadataLoadContext.ResolveType(componentEntityTypeIdentifier);

                    usings.Add(entitySystemType.Namespace);

                    switch (componentInterfaceType)
                    {
                        case OperatorComponentType.Controller:

                            var controllerAttribute = componentSystemType.GetCustomAttribute<ResourceControllerAttribute>();

                            if (controllerAttribute != null)
                            {
                                if (controllerAttribute.Ignore)
                                {
                                    break;
                                }

                                sb.Append($@"
            builder.AddController<{componentSystemType.Name}, {componentEntityTypeIdentifier.Name}>(
                        options: new ResourceManagerOptions()
                        {{
                            AutoRegisterFinalizers          = {controllerAttribute.AutoRegisterFinalizers.ToString().ToLower()},
                            ManageCustomResourceDefinitions = {controllerAttribute.ManageCustomResourceDefinitions.ToString().ToLower()},
                            LabelSelector                   = ""{controllerAttribute.LabelSelector}"",
                            FieldSelector                   = ""{controllerAttribute.FieldSelector}"",
                            ErrorMinRequeueInterval         = TimeSpan.FromSeconds({controllerAttribute.ErrorMinRequeueIntervalSeconds}),
                            ErrorMaxRequeueInterval         = TimeSpan.FromSeconds({controllerAttribute.ErrorMaxRequeueIntervalSeconds}),
                            MaxConcurrentReconciles         = {controllerAttribute.MaxConcurrentReconciles},
                            MaxConcurrentFinalizers         = {controllerAttribute.MaxConcurrentFinalizers}
                        }});");
                            }
                            else
                            {
                                sb.Append($@"
            builder.AddController<{componentSystemType.Name}, {componentEntityTypeIdentifier.Name}>();");
                            }

                            break;

                        case OperatorComponentType.Finalizer:

                            var finalizerAttribute = componentSystemType.GetCustomAttribute<ResourceFinalizerAttribute>();

                            if (finalizerAttribute != null && finalizerAttribute.Ignore)
                            {
                                break;
                            }

                            sb.Append($@"
            builder.AddFinalizer<{componentSystemType.Name}, {componentEntityTypeIdentifier.Name}>();");

                            break;

                        case OperatorComponentType.MutationWebhook:

                            var mutatingWebhookAttribute = componentSystemType.GetCustomAttribute<MutatingWebhookAttribute>();

                            if (mutatingWebhookAttribute != null && mutatingWebhookAttribute.Ignore)
                            {
                                break;
                            }

                            sb.Append($@"
            builder.AddMutatingWebhook<{componentSystemType.Name}, {componentEntityTypeIdentifier.Name}>();");

                            break;

                        case OperatorComponentType.ValidationWebhook:

                            var validatingWebhookAttribute = componentSystemType.GetCustomAttribute<ValidatingWebhookAttribute>();

                            if (validatingWebhookAttribute != null && validatingWebhookAttribute.Ignore)
                            {
                                break;
                            }

                            sb.Append($@"
            builder.AddValidatingWebhook<{componentSystemType.Name}, {componentEntityTypeIdentifier.Name}>();");

                            break;

                        default:

                            break;
                    }
                }
                sb.Append($@"
            return builder;");

                

                sb.Append($@"
        }}
    }}
}}
");
                var classSource = sb.ToString();

                sb.Clear();
                sb.AppendLine(Constants.AutoGeneratedCodeHeader);
                sb.AppendLine();

                var lastUsingRoot = "";

                foreach (var @using in usings)
                {
                    var usingRoot = @using.Split('.').First();

                    if (!string.IsNullOrEmpty(lastUsingRoot) && usingRoot != lastUsingRoot)
                    {
                        sb.AppendLine();
                    }

                    lastUsingRoot = usingRoot;

                    sb.AppendLine($"using {@using};");
                }

                var source = sb.ToString() + classSource;

                context.AddSource($"ServiceCollectionExtensions.g.cs", SourceText.From(source, Encoding.UTF8, SourceHashAlgorithm.Sha256));
            }
        }
    }
}
