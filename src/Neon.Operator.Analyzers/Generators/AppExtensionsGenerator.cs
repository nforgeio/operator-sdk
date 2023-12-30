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
using Neon.Operator.Webhooks;

using Neon.Roslyn;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers
{
    [Generator]
    public class AppExtensionsGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            //System.Diagnostics.Debugger.Launch();
            context.RegisterForSyntaxNotifications(() => new AppExtensionsReceiver());
        }

        public Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            Assembly assembly = null;
            try
            {
                var runtimeDependencies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
                var targetAssembly = runtimeDependencies
                    .FirstOrDefault(ass => Path.GetFileNameWithoutExtension(ass).Equals(assemblyName.Name, StringComparison.InvariantCultureIgnoreCase));

                if (!String.IsNullOrEmpty(targetAssembly))
                    assembly = Assembly.LoadFrom(targetAssembly);
            }
            catch (Exception)
            {
            }
            return assembly;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var metadataLoadContext = new MetadataLoadContext(context.Compilation);
            var webhooks            = ((AppExtensionsReceiver)context.SyntaxReceiver)?.ClassesToRegister;
            var namedTypeSymbols    = context.Compilation.GetNamedTypeSymbols();
            var logString           = new StringBuilder();
            bool hasErrors          = false;

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.TargetFramework", out var targetFramework))
            {
                if (targetFramework.StartsWith("netstandard", StringComparison.InvariantCultureIgnoreCase))
                {
                    return;
                }
            }

            var sb = new StringBuilder();

            var usings = new HashSet<string>()
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Reflection",
                "System.Threading.Tasks",
                "k8s",
                "Microsoft.AspNetCore.Builder",
                "Microsoft.AspNetCore.Diagnostics.HealthChecks",
                "Microsoft.Extensions.DependencyInjection",
                "Microsoft.Extensions.Hosting",
                "Microsoft.Extensions.Logging",
                "Neon.Diagnostics",
                "Neon.Operator.Builder",
                "Neon.Operator.Webhooks",
                "Neon.Operator.Webhooks.Ngrok",
                "Neon.Tasks",
                "Prometheus",
        };

            sb.AppendLine($@"
namespace Neon.Operator
{{
    /// <summary>
    /// Extension methods to register Kubernetes operator components with the <see cref=""IApplicationBuilder""/>.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {{
        /// <summary>
        /// <para>
        /// Use the Kubernetes operator. Registers controllers and webhooks.
        /// </para>
        /// </summary>
        /// <param name=""app"">The <see cref=""IApplicationBuilder""/>.</param>
        public static void UseKubernetesOperator(this IApplicationBuilder app)
        {{
            app.UseRouting();

            var k8s              = app.ApplicationServices.GetRequiredService<IKubernetes>();
            var operatorSettings = app.ApplicationServices.GetRequiredService<OperatorSettings>();
            var logger           = app.ApplicationServices.GetService<ILoggerFactory>()?.CreateLogger(nameof(ApplicationBuilderExtensions));

            app.UseEndpoints(
                async endpoints =>
                {{
                    await SyncContext.Clear;

                    endpoints.MapControllers();
                   
                    var addedEndpoints = new HashSet<string>();

                    endpoints.MapMetrics(operatorSettings.MetricsEndpoint);
                    addedEndpoints.Add(operatorSettings.MetricsEndpoint);

                    if (!addedEndpoints.Contains(operatorSettings.StartupEndpooint))
                    {{
                        endpoints.MapHealthChecks(operatorSettings.StartupEndpooint, new HealthCheckOptions()
                        {{
                            Predicate = (healthCheck =>
                            {{
                                return healthCheck.Tags.Contains(OperatorBuilder.StartupHealthProbeTag);
                            }})
                        }});

                        addedEndpoints.Add(operatorSettings.StartupEndpooint);
                    }}

                    if (!addedEndpoints.Contains(operatorSettings.LivenessEndpooint))
                    {{
                        endpoints.MapHealthChecks(operatorSettings.LivenessEndpooint, new HealthCheckOptions()
                        {{
                            Predicate = (healthCheck =>
                            {{
                                return healthCheck.Tags.Contains(OperatorBuilder.LivenessHealthProbeTag);
                            }})
                        }});

                        addedEndpoints.Add(operatorSettings.LivenessEndpooint);
                    }}

                    if (!addedEndpoints.Contains(operatorSettings.ReadinessEndpooint))
                    {{
                        endpoints.MapHealthChecks(operatorSettings.ReadinessEndpooint, new HealthCheckOptions()
                        {{
                            Predicate = (healthCheck =>
                            {{
                                return healthCheck.Tags.Contains(OperatorBuilder.ReadinessHealthProbeTag);
                            }})
                        }});

                        addedEndpoints.Add(operatorSettings.ReadinessEndpooint);
                    }}

                    var tunnel = app.ApplicationServices.GetServices<IHostedService>()
                        .OfType<NgrokWebhookTunnel>()
                        .SingleOrDefault();

                    var componentRegistrar = app.ApplicationServices.GetRequiredService<ComponentRegister>();");

            foreach (var webhook in webhooks)
            {
                try
                {
                    var webhookNs = webhook.GetNamespace();

                    usings.Add(webhookNs);

                    var webhookSystemType = metadataLoadContext.ResolveType($"{webhookNs}.{webhook.Identifier.ValueText}");
                    var assemblySymbol    = context.Compilation.SourceModule.ReferencedAssemblySymbols.Last();
                    var members           = assemblySymbol.GlobalNamespace.GetNamespaceMembers();
                    
                    var typeMembers = context.Compilation.SourceModule.ReferencedAssemblySymbols
                        .SelectMany(ras => ras.GlobalNamespace.GetNamespaceMembers())
                        .SelectMany(nsm => nsm.GetTypeMembers());

                    var webhookEntityType = webhook
                        .DescendantNodes()?
                        .OfType<BaseListSyntax>()?
                        .Where(dn => dn.DescendantNodes()?.OfType<GenericNameSyntax>()?.Any(gns =>
                                gns.Identifier.ValueText.EndsWith(typeof(IMutatingWebhook<>).Name.Replace("`1", ""))
                                || gns.Identifier.ValueText.EndsWith(typeof(MutatingWebhookBase<>).Name.Replace("`1", ""))
                                || gns.Identifier.ValueText.EndsWith(typeof(IValidatingWebhook<>).Name.Replace("`1", ""))
                                || gns.Identifier.ValueText.EndsWith(typeof(ValidatingWebhookBase<>).Name.Replace("`1", ""))
                                ) == true).FirstOrDefault();

                    var webhookTypeIdentifier           = webhookEntityType.DescendantNodes().OfType<IdentifierNameSyntax>().Single();
                    var sdf                             = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
                    var webhookTypeIdentifierNamespace  = webhookTypeIdentifier.GetNamespace();
                    var webhookEntityTypeIdentifier     = namedTypeSymbols.Where(ntm => ntm.MetadataName == webhookTypeIdentifier.Identifier.ValueText).SingleOrDefault();
                    var webhookEntityFullyQualifiedName = webhookEntityTypeIdentifier.ToDisplayString(DisplayFormat.NameAndContainingTypesAndNamespaces);

                    var entitySystemType = metadataLoadContext.ResolveType(webhookEntityTypeIdentifier);

                    usings.Add(entitySystemType.Namespace);

                    var interfaces = webhookSystemType.GetInterfaces().ToList();

                    if (webhookSystemType.BaseType != null)
                    {
                        Type baseType = webhookSystemType.BaseType;

                        while (baseType != null)
                        {
                            interfaces.AddRange(baseType.GetInterfaces());
                            baseType = baseType.BaseType;
                        }
                    }

                    var webhookInterfaceType = interfaces
                        .Where(i => i.IsGenericType && 
                            (i.GetGenericTypeDefinition().Equals(typeof(IMutatingWebhook<>))
                            || i.GetGenericTypeDefinition().Equals(typeof(MutatingWebhookBase<>))
                            || i.GetGenericTypeDefinition().Equals(typeof(IValidatingWebhook<>))
                            || i.GetGenericTypeDefinition().Equals(typeof(ValidatingWebhookBase<>))
                            ))
                        .Select(i => i.Name.Replace("`1", ""))
                        .FirstOrDefault();

                    sb.Append($@"
                    try
                    {{
                        if (tunnel == null)
                        {{
                            logger?.LogInformationEx(() => $""Registering [{webhookSystemType.Name}] with Kubernetes API Server."");

                            var hook = ({webhookInterfaceType}<{webhookEntityTypeIdentifier.ToDisplayString(DisplayFormat.NameOnly)}>)app.ApplicationServices.GetRequiredService<{webhookSystemType.Name}>();

                            await hook.CreateAsync(app.ApplicationServices);
                        }}

                    }}
                    catch (Exception e)
                    {{
                        logger?.LogErrorEx(e);
                    }}");

                }
                catch (Exception e)
                {
                    logString.AppendLine(e.Message);
                }
            }

            sb.Append($@"
            }});");

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

            context.AddSource($"ApplicationExtensions.g.cs", SourceText.From(source, Encoding.UTF8, SourceHashAlgorithm.Sha256));

            if (hasErrors)
            {
                context.AddSource("log", logString.ToString());
            }
        }
    }
}
