// -----------------------------------------------------------------------------
// FILE:	    ValidatingWebhookGenerator.cs
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
using System.Text.RegularExpressions;

using k8s;
using k8s.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Neon.Operator.Analyzers.Receivers;
using Neon.Common;
using Neon.Operator.Attributes;
using Neon.Operator.Webhooks;
using Neon.Roslyn;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers
{
    [Generator]
    public class ValidatingWebhookGenerator : ISourceGenerator
    {
        private Dictionary<string, StringBuilder> logs;


        public Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);
            var assembly     = (Assembly)null;

            try
            {
                var runtimeDependencies = Directory.GetFiles(RuntimeEnvironment.GetRuntimeDirectory(), "*.dll");
                var targetAssembly      = runtimeDependencies.FirstOrDefault(ass => Path.GetFileNameWithoutExtension(ass).Equals(assemblyName.Name, StringComparison.InvariantCultureIgnoreCase));

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
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
            if (Environment.GetEnvironmentVariable("VALIDATINGWEBHOOK_GENERATOR_DEBUG") == "1")
            {
                System.Diagnostics.Debugger.Launch();
            }
#pragma warning restore RS1035 // Do not use APIs banned for analyzers

            logs = new Dictionary<string, StringBuilder>();

            var metadataLoadContext = new MetadataLoadContext(context.Compilation);
            var ValidatingWebhooks  = ((ValidatingWebhookReceiver)context.SyntaxReceiver)?.ValidatingWebhooks;
            var attributes          = ((ValidatingWebhookReceiver)context.SyntaxReceiver)?.Attributes;
            var nameAttribute       = RoslynExtensions.GetAttribute<NameAttribute>(metadataLoadContext, context.Compilation, attributes);

            if (ValidatingWebhooks.Count == 0)
            {
                return;
            }

            var namedTypeSymbols          = context.Compilation.GetNamedTypeSymbols();
            bool certManagerDisabled      = false;
            bool autoRegisterWebhooks     = false;
            string operatorName           = Regex.Replace(context.Compilation.AssemblyName, @"([a-z])([A-Z])", "$1-$2").ToLower();
            string operatorNamespace      = null;
            string webhookOutputDirectory = null;

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.MSBuildProjectDirectory", out var projectDirectory))
            {
                webhookOutputDirectory = projectDirectory;
            }
            else if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir))
            {
                webhookOutputDirectory = projectDir;
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorManifestOutputDir", out var manifestOutDir))
            {
                if (!string.IsNullOrEmpty(manifestOutDir))
                {
                    if (Path.IsPathRooted(manifestOutDir))
                    {
                        webhookOutputDirectory = manifestOutDir;
                    }
                    else
                    {
                        webhookOutputDirectory = Path.Combine(projectDirectory, manifestOutDir);
                    }
                }
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorWebhookOutputDir", out var webhookOutDir))
            {
                if (!string.IsNullOrEmpty(webhookOutDir))
                {
                    if (Path.IsPathRooted(webhookOutDir))
                    {
                        webhookOutputDirectory = webhookOutDir;
                    }
                    else
                    {
                        webhookOutputDirectory = Path.Combine(projectDirectory, webhookOutDir);
                    }
                }
            }

            if (string.IsNullOrEmpty(webhookOutputDirectory))
            {
                throw new Exception("Webhook output directory not defined.");
            }

            Directory.CreateDirectory(webhookOutputDirectory);

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorName", out var oName))
            {
                if (!string.IsNullOrEmpty(oName))
                {
                    operatorName = oName;
                }
            }

            if (nameAttribute != null)
            {
                operatorName = nameAttribute.Name;
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorNamespace", out var opNamespace))
            {
                if (!string.IsNullOrEmpty(opNamespace))
                {
                    operatorNamespace = opNamespace;
                }
            }

            if (string.IsNullOrEmpty(operatorName))
            {
                throw new Exception("[OperatorName] not defined.");
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorCertManagerDisabled", out var certManagerString))
            {
                if (bool.TryParse(certManagerString, out var certManagerBool))
                {
                    certManagerDisabled = certManagerBool;
                }
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorAutoRegisterWebhooks", out var autoRegisterWebhooksString))
            {
                if (bool.TryParse(autoRegisterWebhooksString, out var autoRegisterWebhooksBool))
                {
                    autoRegisterWebhooks = autoRegisterWebhooksBool;
                }
            }

            if (ValidatingWebhooks.Any())
            {
                foreach (var webhook in ValidatingWebhooks)
                {
                    Log(context, $"webook: {webhook.Identifier.ValueText}");

                    try
                    {
                        var webhookNs         = webhook.GetNamespace();
                        var webhookSystemType = metadataLoadContext.ResolveType($"{webhookNs}.{webhook.Identifier.ValueText}");
                        var assemblySymbol    = context.Compilation.SourceModule.ReferencedAssemblySymbols.Last();
                        var members           = assemblySymbol.GlobalNamespace.GetNamespaceMembers();

                        //var webhookAttribute = webhookSystemType.GetCustomAttributes();

                        //if (webhookAttribute.Ignore)
                        //{
                        //    return null;
                        //}

                        var typeMembers = context.Compilation.SourceModule.ReferencedAssemblySymbols
                            .SelectMany(ras => ras.GlobalNamespace.GetNamespaceMembers())
                            .SelectMany(nsm => nsm.GetTypeMembers());

                        var webhookEntityType = webhook
                            .DescendantNodes()?
                            .OfType<BaseListSyntax>()?
                            .Where(dn => dn.DescendantNodes()?.OfType<GenericNameSyntax>()?
                                .Any(gns => gns.Identifier.ValueText.EndsWith("IValidatingWebhook") || gns.Identifier.ValueText.EndsWith("ValidatingWebhookBase")) == true)
                            .FirstOrDefault();

                        var webhookTypeIdentifier           = webhookEntityType.DescendantNodes().OfType<IdentifierNameSyntax>().Single();
                        var sdf                             = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
                        var webhookTypeIdentifierNamespace  = webhookTypeIdentifier.GetNamespace();
                        var webhookEntityTypeIdentifier     = namedTypeSymbols.Where(ntm => ntm.MetadataName == webhookTypeIdentifier.Identifier.ValueText).SingleOrDefault();
                        var webhookEntityFullyQualifiedName = webhookEntityTypeIdentifier.ToDisplayString(sdf);
                        var entitySystemType                = metadataLoadContext.ResolveType(webhookEntityTypeIdentifier);

                        CreateYaml(
                            operatorName:                    operatorName,
                            operatorNamespace:               operatorNamespace,
                            webhook:                         webhook,
                            webhookEntityTypeIdentifier:     webhookEntityTypeIdentifier,
                            webhookSystemType:               webhookSystemType,
                            entitySystemType:                entitySystemType,
                            webhookEntityFullyQualifiedName: webhookEntityFullyQualifiedName,
                            certManagerDisabled:             certManagerDisabled,
                            webhookOutputDirectory:          webhookOutputDirectory);

                        GenerateController(
                            context:                         context,
                            webhook:                         webhook,
                            webhookEntityTypeIdentifier:     webhookEntityTypeIdentifier,
                            webhookSystemType:               webhookSystemType,
                            entitySystemType:                entitySystemType,
                            webhookEntityFullyQualifiedName: webhookEntityFullyQualifiedName);
                    }
                    catch (Exception e)
                    {
                        Log(context, e.Message);
                    }
                }
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorAnalyzerLoggingEnabled", out var logEnabledString))
            {
                if (bool.TryParse(logEnabledString, out var logEnabledbool))
                {
                    if (!logs.ContainsKey(context.Compilation.AssemblyName))
                    {
                        return;
                    }

                    var log                = logs[context.Compilation.AssemblyName];
                    var logOutputDirectory = projectDirectory;

                    if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorAnalyzerLoggingDir", out var logsOutDir))
                    {
                        if (!string.IsNullOrEmpty(logsOutDir))
                        {
                            if (Path.IsPathRooted(logsOutDir))
                            {
                                logOutputDirectory = Path.Combine(logsOutDir, nameof(ValidatingWebhookGenerator));
                            }
                            else
                            {
                                logOutputDirectory = Path.Combine(projectDirectory, logsOutDir, nameof(ValidatingWebhookGenerator));
                            }
                        }
                    }

                    Directory.CreateDirectory(logOutputDirectory);

                    var outputPath = Path.Combine(logOutputDirectory, $"{context.Compilation.AssemblyName}.log");

                    AnalyzerHelper.WriteFileWhenDifferent(outputPath, log.ToString());
                }
            }
        }

        private void Log(GeneratorExecutionContext context, string message)
        {
            if (!logs.ContainsKey(context.Compilation.AssemblyName))
            {
                logs[context.Compilation.AssemblyName] = new StringBuilder();
            }

            logs[context.Compilation.AssemblyName].AppendLine(message);
        }

        private void Log(GeneratorExecutionContext context, Exception e)
        {
            Log(context, e.Message);
            Log(context, e.StackTrace);
        }


        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ValidatingWebhookReceiver());
        }

        private string CreateEndpoint(Type entityType, Type webhookImplementation)
        {
            var metadata = entityType.GetCustomAttribute<KubernetesEntityAttribute>();
            var builder  = new StringBuilder();

            if (!string.IsNullOrEmpty(metadata.Group))
            {
                builder.Append($"/{metadata.Group}");
            }

            if (!string.IsNullOrEmpty(metadata.ApiVersion))
            {
                builder.Append($"/{metadata.ApiVersion}");
            }

            if (!string.IsNullOrEmpty(metadata.PluralName))
            {
                builder.Append($"/{metadata.PluralName}");
            }

            builder.Append($"/{webhookImplementation.Name}");
            builder.Append("/validate");

            return builder.ToString().ToLowerInvariant();
        }

        private void CreateYaml(
            string                 operatorName,
            string                 operatorNamespace,
            ClassDeclarationSyntax webhook,
            INamedTypeSymbol       webhookEntityTypeIdentifier,
            Type                   webhookSystemType,
            Type                   entitySystemType,
            string                 webhookEntityFullyQualifiedName,
            bool                   certManagerDisabled,
            string                 webhookOutputDirectory)
        {
            var webhookAttribute     = webhookSystemType.GetCustomAttribute<WebhookAttribute>();
            var webhookConfiguration = new V1ValidatingWebhookConfiguration().Initialize();

            webhookConfiguration.Metadata.Name = webhookAttribute.Name;

            if (!certManagerDisabled)
            {
                webhookConfiguration.Metadata.Annotations = webhookConfiguration.Metadata.EnsureAnnotations();

                if (!string.IsNullOrEmpty(operatorNamespace))
                {
                    webhookConfiguration.Metadata.Annotations.Add("cert-manager.io/inject-ca-from", $"{operatorNamespace}/{operatorName}");
                }
                else
                {
                    webhookConfiguration.Metadata.Annotations.Add("cert-manager.io/inject-ca-from", $"{{{{ .Release.Namespace }}}}/{operatorName}");
                }
            }

            var clientConfig = new Admissionregistrationv1WebhookClientConfig()
            {
                Service = new Admissionregistrationv1ServiceReference()
                {
                    Name              = operatorName,
                    Path              = CreateEndpoint(entitySystemType, this.GetType())
                }
            };

            if (!string.IsNullOrEmpty(operatorNamespace))
            {
                clientConfig.Service.NamespaceProperty = operatorNamespace;
            }
            else
            {
                clientConfig.Service.NamespaceProperty = "{{ .Release.Namespace }}";
            }

            var validatingWebhook = new V1ValidatingWebhook()
            {
                Name                    = webhookAttribute.Name,
                Rules                   = new List<V1RuleWithOperations>(),
                ClientConfig            = clientConfig,
                AdmissionReviewVersions = webhookAttribute.AdmissionReviewVersions,
                FailurePolicy           = webhookAttribute.FailurePolicy.ToMemberString(),
                SideEffects             = webhookAttribute.SideEffects.ToMemberString(),
                TimeoutSeconds          = webhookAttribute.TimeoutSeconds,
                MatchPolicy             = webhookAttribute.MatchPolicy.ToMemberString(),
            };

            var namespaceSelectorExpressions =  webhookSystemType.GetCustomAttributes<NamespaceSelectorExpressionAttribute>();
            var namespaceSelectorLabels      =  webhookSystemType.GetCustomAttributes<NamespaceSelectorLabelAttribute>();

            if (namespaceSelectorExpressions.Any() || namespaceSelectorLabels.Any())
            {
                validatingWebhook.NamespaceSelector                  = new V1LabelSelector();
                validatingWebhook.NamespaceSelector.MatchExpressions = new List<V1LabelSelectorRequirement>();
                validatingWebhook.NamespaceSelector.MatchLabels      = new Dictionary<string, string>();

                foreach (var selector in namespaceSelectorExpressions)
                {
                    validatingWebhook.NamespaceSelector.MatchExpressions.Add(new V1LabelSelectorRequirement()
                    {
                        Key              = selector.Key,
                        OperatorProperty = selector.Operator.ToString(),
                        Values           = selector.Values.Split(',')
                    });
                }

                foreach (var selector in namespaceSelectorLabels)
                {
                    validatingWebhook.NamespaceSelector.MatchLabels.Add(selector.Key, selector.Value);
                }
            }

            var objectSelectorExpressions = webhookSystemType.GetCustomAttributes<ObjectSelectorExpressionAttribute>();
            var objectSelectorLabels      = webhookSystemType.GetCustomAttributes<ObjectSelectorLabelAttribute>();

            if (objectSelectorExpressions.Any() || objectSelectorLabels.Any())
            {
                validatingWebhook.ObjectSelector                  = new V1LabelSelector();
                validatingWebhook.ObjectSelector.MatchExpressions = new List<V1LabelSelectorRequirement>();
                validatingWebhook.ObjectSelector.MatchLabels      = new Dictionary<string, string>();

                foreach (var selector in objectSelectorExpressions)
                {
                    validatingWebhook.ObjectSelector.MatchExpressions.Add(new V1LabelSelectorRequirement()
                    {
                        Key              = selector.Key,
                        OperatorProperty = selector.Operator.ToString(),
                        Values           = selector.Values.Split(',')
                    });
                }

                foreach (var selector in objectSelectorLabels)
                {
                    validatingWebhook.ObjectSelector.MatchLabels.Add(selector.Key, selector.Value);
                }
            }

            webhookConfiguration.Webhooks = new List<V1ValidatingWebhook>
            {
                validatingWebhook
            };

            var rules = webhookSystemType.GetCustomAttributes<WebhookRuleAttribute>();

            foreach (var rule in rules)
            {
                webhookConfiguration.Webhooks.FirstOrDefault().Rules.Add(
                    new V1RuleWithOperations()
                    {
                        ApiGroups   = rule.ApiGroups,
                        ApiVersions = rule.ApiVersions,
                        Operations  = rule.Operations.ToList(),
                        Resources   = rule.Resources,
                        Scope       = rule.Scope
                    }
                );
            }

            var outputPath = Path.Combine(webhookOutputDirectory, $"{webhookConfiguration.Name()}{Constants.GeneratedYamlExtension}");

            AnalyzerHelper.WriteFileWhenDifferent(outputPath, KubernetesYaml.Serialize(webhookConfiguration));
        }

        private void GenerateController(
            GeneratorExecutionContext context,
            ClassDeclarationSyntax    webhook,
            INamedTypeSymbol          webhookEntityTypeIdentifier,
            Type                      webhookSystemType,
            Type                      entitySystemType,
            string                    webhookEntityFullyQualifiedName)
        {
            var metadata = webhookEntityTypeIdentifier.GetAttributes();
            var builder  = new StringBuilder();
            var k8sattr  = metadata.Where(attr => attr.AttributeClass.MetadataName.EndsWith("KubernetesEntityAttribute")).Single();
            var attrDict = new Dictionary<string, string>();

            foreach (var kvp in k8sattr.NamedArguments)
            {
                attrDict.Add(kvp.Key, kvp.Value.Value.ToString());
            }

            if (attrDict.TryGetValue("Group", out var group))
            {
                if (!string.IsNullOrEmpty(group))
                {
                    builder.Append($"{group}/");
                }
            }

            if (attrDict.TryGetValue("ApiVersion", out var apiVersion))
            {
                if (!string.IsNullOrEmpty(apiVersion))
                {
                    builder.Append($"{apiVersion}/");
                }
            }

            if (attrDict.TryGetValue("PluralName", out var pluralName))
            {
                if (!string.IsNullOrEmpty(pluralName))
                {
                    builder.Append($"{pluralName}/");
                }
            }

            builder.Append($"{webhookSystemType.Name}/");

            builder.Append("mutate");

            var route = builder.ToString().ToLowerInvariant();

            var usings = new HashSet<string>()
            {
                "System",
                "System.Diagnostics",
                "System.Threading.Tasks",
                "k8s",
                "k8s.Models",
                "Microsoft.AspNetCore.Http",
                "Microsoft.AspNetCore.Mvc",
                "Microsoft.Extensions.Logging",
                "Neon.Diagnostics",
                "Neon.Operator",
                "Neon.Operator.Webhooks",
                "Prometheus",
                entitySystemType.Namespace,
                webhookEntityFullyQualifiedName.TrimEnd('.').Remove(webhookEntityFullyQualifiedName.LastIndexOf('.') + 1).TrimEnd('.'),
                webhook.GetNamespace()
            };

            var sb = new StringBuilder();

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

            var controllerClassName = $"{webhook.Identifier.ValueText}Controller";
            var controllerNamespace = webhook.GetNamespace();

            sb.AppendLine($@"
namespace {controllerNamespace}.Controllers
{{
    /// <summary>
    /// Auto-generated implementation of {controllerClassName}.
    /// </summary>
    [ApiController]
    public class {controllerClassName} : ControllerBase
    {{
        private WebhookMetrics<{webhookEntityTypeIdentifier.Name}> metrics;
        private {webhook.Identifier.ValueText} webhook;
        private OperatorSettings operatorSettings;
        private ILogger<{controllerClassName}> logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        public {controllerClassName}(
            {webhook.Identifier.ValueText} webhook,
            WebhookMetrics<{webhookEntityTypeIdentifier.Name}> metrics,
            OperatorSettings operatorSettings,
            ILogger<{controllerClassName}> logger = null)
        {{
            this.webhook = webhook;
            this.metrics = metrics;
            this.operatorSettings = operatorSettings;
            this.logger = logger;
        }}

        /// <summary>
        /// Auto-generated implementation of {webhookEntityTypeIdentifier.Name}Webhook
        /// </summary>
        /// <param name=""admissionRequest"">The admission request</param>
        /// <returns>The validation result</returns>
        [HttpPost(""{route}"")]
        public async Task<ActionResult<ValidationResult>> {webhookEntityTypeIdentifier.Name}WebhookAsync([FromBody] AdmissionReview<{webhookEntityTypeIdentifier.Name}> admissionRequest)
        {{
            using var activity = Activity.Current;
            using var inFlight = metrics.RequestsInFlight.TrackInProgress();
            using var timer    = metrics.LatencySeconds.NewTimer();

            AdmissionResponse response;

            try
            {{

                var @object   = KubernetesJson.Deserialize<{webhookEntityTypeIdentifier.Name}>(KubernetesJson.Serialize(admissionRequest.Request.Object));
                var oldObject = KubernetesJson.Deserialize<{webhookEntityTypeIdentifier.Name}>(KubernetesJson.Serialize(admissionRequest.Request.OldObject));

                logger?.LogInformationEx(() => @$""Admission with method """"{{admissionRequest.Request.Operation}}""""."");

                ValidationResult result;

                switch (admissionRequest.Request.Operation)
                {{
                    case ""CREATE"":

                        result = await webhook.CreateAsync(@object, admissionRequest.Request.DryRun);

                        break;

                    case ""UPDATE"":

                        result = await webhook.UpdateAsync(oldObject, @object, admissionRequest.Request.DryRun);
                        break;

                    case ""DELETE"":

                        result = await webhook.DeleteAsync(@object, admissionRequest.Request.DryRun);
                        break;

                    default:
                        throw new InvalidOperationException();
                }}

                response = webhook.TransformResult(result, admissionRequest.Request);

            }}
            catch (Exception e)
            {{
                logger?.LogErrorEx(e, ""An error happened during admission."");

                response = new AdmissionResponse()
                {{
                    Allowed = false,
                    Status = new()
                    {{
                        Code = StatusCodes.Status500InternalServerError,
                        Message = ""There was an internal server error."",
                    }},
                }};
            }}

            admissionRequest.Response = response;
            admissionRequest.Response.Uid = admissionRequest.Request.Uid;

            logger?.LogInformationEx(() => @$""AdmissionHook """"{{webhook.Name}}"""" did return """"{{admissionRequest.Response?.Allowed}}"""" for """"{{admissionRequest.Request.Operation}}""""."");
            admissionRequest.Request = null;

            metrics.RequestsTotal.WithLabels(new string[] {{ operatorSettings.Name, webhook.GetEndpoint(), response.Status?.Code.ToString() }}).Inc();

            return Ok(admissionRequest);
        }}
    }}
}}");
            var result = sb.ToString();

            context.AddSource($"{controllerClassName}.g.cs", SourceText.From(result, Encoding.UTF8, SourceHashAlgorithm.Sha256));
        }
    }
}
