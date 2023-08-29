using System;
using System.Collections.Generic;
using System.Linq;
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

        public void Execute(GeneratorExecutionContext context)
        {
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
                        .Where(i =>
                            i.IsGenericType && baseNames.Any(bn => bn.FullName == i.FullName))
                        .Select(i => i.CustomAttributes?
                                        .Where(a => a.AttributeType.Equals(typeof(OperatorComponentAttribute)))
                                        .FirstOrDefault().ConstructorArguments.First().Value)
                        .FirstOrDefault();

                    IAssemblySymbol assemblySymbol = context.Compilation.SourceModule.ReferencedAssemblySymbols.Last();
                    var members = assemblySymbol.GlobalNamespace.
                            GetNamespaceMembers();

                    var typeMembers = context.Compilation.SourceModule.ReferencedAssemblySymbols.SelectMany(
                        ras => ras.GlobalNamespace.GetNamespaceMembers())
                        .SelectMany(nsm => nsm.GetTypeMembers());

                    var componentEntityType = component
                        .DescendantNodes()?
                        .OfType<BaseListSyntax>()?
                        .Where(dn => dn.DescendantNodes()
                                ?.OfType<GenericNameSyntax>()
                                ?.Any(gns => gns.Identifier.ValueText.EndsWith(typeof(IMutatingWebhook<>).Name.Replace("`1", ""))
                                            || gns.Identifier.ValueText.EndsWith(typeof(MutatingWebhookBase<>).Name.Replace("`1", ""))
                                            || gns.Identifier.ValueText.EndsWith(typeof(IValidatingWebhook<>).Name.Replace("`1", ""))
                                            || gns.Identifier.ValueText.EndsWith(typeof(ValidatingWebhookBase<>).Name.Replace("`1", ""))
                                            || gns.Identifier.ValueText.EndsWith(typeof(IResourceController).Name)
                                            || gns.Identifier.ValueText.EndsWith(typeof(IResourceController<>).Name.Replace("`1", ""))
                                            || gns.Identifier.ValueText.EndsWith(typeof(ResourceControllerBase<>).Name.Replace("`1", ""))
                                            || gns.Identifier.ValueText.EndsWith(typeof(IResourceFinalizer<>).Name.Replace("`1", ""))
                                            || gns.Identifier.ValueText.EndsWith(typeof(ResourceFinalizerBase<>).Name.Replace("`1", ""))
                                            ) == true).FirstOrDefault();

                    var componentTypeIdentifier          = componentEntityType.DescendantNodes().OfType<IdentifierNameSyntax>().Single();

                    var componentTypeIdentifierNamespace = componentTypeIdentifier.GetNamespace();

                    var componentEntityTypeIdentifier     = namedTypeSymbols.Where(ntm => ntm.MetadataName == componentTypeIdentifier.Identifier.ValueText).SingleOrDefault();
                    var componentEntityFullyQualifiedName = componentEntityTypeIdentifier.ToDisplayString(DisplayFormat.NameAndContainingTypesAndNamespaces);

                    var entitySystemType = metadataLoadContext.ResolveType(componentEntityTypeIdentifier);
                    usings.Add(entitySystemType.Namespace);

                    switch (componentInterfaceType)
                    {
                        case OperatorComponentType.Controller:

                            sb.Append($@"
            builder.AddController<{componentSystemType.Name}, {componentEntityTypeIdentifier.Name}>();");

                            break;

                        case OperatorComponentType.Finalizer:

                            sb.Append($@"
            builder.AddFinalizer<{componentSystemType.Name}, {componentEntityTypeIdentifier.Name}>();");

                            break;

                        case OperatorComponentType.MutationWebhook:

                            sb.Append($@"
            builder.AddMutatingWebhook<{componentSystemType.Name}, {componentEntityTypeIdentifier.Name}>();");

                            break;

                        case OperatorComponentType.ValidationWebhook:

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
                sb.AppendLine(Constants.AutoGeneratedHeader);
                sb.AppendLine();

                var lastUsingRoot = "";
                foreach (var u in usings)
                {
                    var usingRoot = u.Split('.').First();

                    if (!string.IsNullOrEmpty(lastUsingRoot) && usingRoot != lastUsingRoot)
                    {
                        sb.AppendLine();
                    }

                    lastUsingRoot = usingRoot;

                    sb.AppendLine($"using {u};");
                }
                var source = sb.ToString() + classSource;

                context.AddSource($"ServiceCollectionExtensions.g.cs", SourceText.From(source, Encoding.UTF8, SourceHashAlgorithm.Sha256));

            }
        }
    }
}
