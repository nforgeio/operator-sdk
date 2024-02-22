// -----------------------------------------------------------------------------
// FILE:	    OlmGenerator.cs
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
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using k8s;
using k8s.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Neon.Common;
using Neon.Operator.Analyzers.Receivers;
using Neon.Operator.Attributes;
using Neon.Operator.OperatorLifecycleManager;
using Neon.Operator.Rbac;
using Neon.Operator.Webhooks;
using Neon.Roslyn;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers.Generators
{
    [Generator]
    public class OlmGenerator : ISourceGenerator
    {
        internal static readonly DiagnosticDescriptor InstallModeDuplicateError = new DiagnosticDescriptor(id: "NO11001",
                                                                                              title: "Each install mode may only be specified once",
                                                                                              messageFormat: "'{0}' has been specified multiple times",
                                                                                              category: "NeonOperatorSdk",
                                                                                              DiagnosticSeverity.Error,
                                                                                              isEnabledByDefault: true);

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new OlmReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //System.Diagnostics.Debugger.Launch();

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var projectDir);
            if (!context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.TargetDir", out var targetDir))
            {
                return;
            }

            context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.OlmChannels", out var olmChannels);

            var metadataLoadContext    = new MetadataLoadContext(context.Compilation);
            var attributes             = ((OlmReceiver)context.SyntaxReceiver)?.Attributes;
            var namedTypeSymbols       = context.Compilation.GetNamedTypeSymbols();
            var ownedEntities          = GetOwnedEntities(context, metadataLoadContext, attributes);
            var requiredEntities       = GetRequiredEntities(context, metadataLoadContext, attributes);
            var controllers            = ((OlmReceiver)context.SyntaxReceiver)?.ControllersToRegister;
            var hasMutatingWebhooks    = ((OlmReceiver)context.SyntaxReceiver)?.HasMutatingWebhooks ?? false;
            var hasValidatingWebhooks  = ((OlmReceiver)context.SyntaxReceiver)?.HasValidatingWebhooks ?? false;
            var classesWithRbac        = ((OlmReceiver)context.SyntaxReceiver)?.ClassesToRegister;
            var webhooks               = ((OlmReceiver)context.SyntaxReceiver)?.Webhooks;
            var rbacAttributes         = new List<IRbacRule>();
            var leaderElectionDisabled = false;

            var createdAt        = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddThh:mm:ss%K");
            var operatorName     = RoslynExtensions.GetAttribute<NameAttribute>(metadataLoadContext, context.Compilation, attributes);
            var displayName      = RoslynExtensions.GetAttribute<DisplayNameAttribute>(metadataLoadContext, context.Compilation, attributes);
            var description      = RoslynExtensions.GetAttribute<DescriptionAttribute>(metadataLoadContext, context.Compilation, attributes);
            var certified        = RoslynExtensions.GetAttribute<CertifiedAttribute>(metadataLoadContext, context.Compilation, attributes)?.Certified ?? false;
            var containerImage   = RoslynExtensions.GetAttribute<ContainerImageAttribute>(metadataLoadContext, context.Compilation, attributes);
            var capabilities     = RoslynExtensions.GetAttribute<CapabilitiesAttribute>(metadataLoadContext, context.Compilation, attributes);
            var repository       = RoslynExtensions.GetAttribute<RepositoryAttribute>(metadataLoadContext, context.Compilation, attributes);
            var maturity         = RoslynExtensions.GetAttribute<MaturityAttribute>(metadataLoadContext, context.Compilation, attributes);
            var provider         = RoslynExtensions.GetAttribute<ProviderAttribute>(metadataLoadContext, context.Compilation, attributes);
            var version          = RoslynExtensions.GetAttribute<VersionAttribute>(metadataLoadContext, context.Compilation, attributes);
            var minKubeVersion   = RoslynExtensions.GetAttribute<MinKubeVersionAttribute>(metadataLoadContext, context.Compilation, attributes);
            var categories       = RoslynExtensions.GetAttributes<CategoryAttribute>(metadataLoadContext, context.Compilation, attributes);
            var keywords         = RoslynExtensions.GetAttributes<KeywordAttribute>(metadataLoadContext, context.Compilation, attributes);
            var maintainers      = RoslynExtensions.GetAttributes<MaintainerAttribute>(metadataLoadContext, context.Compilation, attributes);
            var icons            = RoslynExtensions.GetAttributes<IconAttribute>(metadataLoadContext, context.Compilation, attributes);
            var installModeAttrs = RoslynExtensions.GetAttributes<InstallModeAttribute>(metadataLoadContext, context.Compilation, attributes);
            var defaultChannel   = RoslynExtensions.GetAttribute<DefaultChannelAttribute>(metadataLoadContext, context.Compilation,attributes);

            if (string.IsNullOrEmpty(olmChannels))
            {
                olmChannels = defaultChannel.DefaultChannel;
            }
            else
            {
                olmChannels = olmChannels.Replace(";", ",");
            }

            if (operatorName == null
                || displayName == null
                || description == null
                || containerImage == null
                || categories == null
                || capabilities == null
                || icons == null
                || keywords == null
                || maturity == null
                || maintainers == null
                || provider == null
                || minKubeVersion == null
                || version == null)
            {
                return;
            }

            if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.NeonOperatorLeaderElectionDisabled", out var leaderElectionString))
            {
                if (bool.TryParse(leaderElectionString, out var leaderElectionBool))
                {
                    leaderElectionDisabled = leaderElectionBool;
                }
            }

            rbacAttributes.Add(
                new RbacRule<V1CustomResourceDefinition>(
                    verbs: RbacVerb.Get | RbacVerb.List | RbacVerb.Watch,
                    scope: EntityScope.Cluster));

            if (!leaderElectionDisabled)
            {
                rbacAttributes.Add(
                    new RbacRule<V1Lease>(
                        verbs: RbacVerb.All,
                        scope: EntityScope.Cluster));
            }

            foreach (var rbacClass in classesWithRbac)
            {
                var classTypeIdentifiers = namedTypeSymbols.Where(ntm => ntm.MetadataName == rbacClass.Identifier.ValueText);
                var crSystemType         = metadataLoadContext.ResolveType(classTypeIdentifiers.FirstOrDefault());
                var crFullyQualifiedName = classTypeIdentifiers.FirstOrDefault().ToDisplayString(DisplayFormat.NameAndContainingTypesAndNamespaces);
                var rbacRuleAttr         = new List<RbacRuleAttribute>();

                foreach (var classTypeIdentifier in classTypeIdentifiers)
                {
                    try
                    {
                        rbacRuleAttr.AddRange(crSystemType.GetCustomAttributes<RbacRuleAttribute>());
                    }
                    catch
                    {
                        // no attributes
                    }
                }

                foreach (var attribute in rbacRuleAttr)
                {
                    rbacAttributes.Add(
                        new RbacRule(
                            apiGroup: attribute.ApiGroup,
                            resource: attribute.Resource,
                            verbs: attribute.Verbs,
                            scope: attribute.Scope,
                            @namespace: attribute.Namespace,
                            resourceNames: attribute.ResourceNames,
                            subResources: attribute.SubResources));
                }

                var rbacGenericAttr = crSystemType.CustomAttributes?
                        .Where(ca=>ca.AttributeType.IsGenericType)?
                        .Where(ca=>ca.AttributeType.GetGenericTypeDefinition().Equals(typeof(RbacRuleAttribute<>)));

                foreach (var r in rbacGenericAttr)
                {
                    var args       = r.NamedArguments;
                    var etype      = r.AttributeType.GenericTypeArguments.FirstOrDefault();
                    var entityType = metadataLoadContext.ResolveType(etype.FullName);
                    var k8sAttr    = entityType.GetCustomAttribute<KubernetesEntityAttribute>();
                    var apiGroup   = k8sAttr.Group;
                    var resource   = k8sAttr.PluralName;

                    var rule = new RbacRule(apiGroup, resource);
                    foreach (var p in r.NamedArguments)
                    {
                        var propertyInfo = typeof(RbacRule).GetProperty(p.MemberInfo.Name);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(rule, p.TypedValue.Value);
                            continue;
                        }

                        var fieldInfo = typeof(RbacRule).GetField(p.MemberInfo.Name);
                        if (fieldInfo != null)
                        {
                            fieldInfo.SetValue(rule, p.TypedValue.Value);
                            continue;
                        }

                        throw new Exception($"No field or property {p}");
                    }

                    rbacAttributes.Add(rule);
                }
            }

            var csv = new V1ClusterServiceVersion().Initialize();

            csv.Metadata = new k8s.Models.V1ObjectMeta();
            csv.Metadata.Name = $"{operatorName.Name}.v{version.Version}";
            csv.Metadata.Annotations = new Dictionary<string, string>
            {
                { "description", description.ShortDescription },
                { "certified", certified.ToString().ToLower() },
                { "createdAt", createdAt },
                { "capabilities", capabilities.Capability.ToString()},
                { "containerImage", $"{containerImage.Repository}:{containerImage.Tag}" },
                { "repository", repository.Repository },
                { "categories", string.Join(", ", categories.SelectMany(c => c.Category.ToStrings()).ToImmutableHashSet().OrderBy(x=>x)) },
            };


            csv.Spec = new V1ClusterServiceVersionSpec();
            csv.Spec.Icon = icons?.Select(i => i.ToIcon(projectDir)).ToList();
            csv.Spec.Keywords = keywords?.SelectMany(k => k.GetKeywords()).Distinct().ToList();
            csv.Spec.DisplayName = displayName.DisplayName;
            csv.Spec.Description = description.FullDescription;
            csv.Spec.Version = version.Version;
            csv.Spec.Maturity = maturity.Maturity;
            csv.Spec.MinKubeVersion = minKubeVersion.MinKubeVersion;
            csv.Spec.Provider = new Provider()
            {
                Name = provider.Name,
                Url = provider.Url
            };
            csv.Spec.Maintainers = maintainers?.Select(m => new Maintainer()
            {
                Name = m.Name,
                Email = m.Email
            }).ToList();
            csv.Spec.CustomResourceDefinitions = new CustomResourceDefinitions()
            {
                Owned = ownedEntities,
                Required = requiredEntities
            };

            var installModes = new List<InstallMode>();

            foreach (var mode in installModeAttrs)
            {
                foreach (var im in mode.Type.GetTypes())
                {
                    if (installModes.Any(i => i.Type == im))
                    {
                        context.ReportDiagnostic(
                                Diagnostic.Create(InstallModeDuplicateError,
                                Location.None,
                                im));
                    }
                    installModes.Add(new InstallMode()
                    {
                        Supported = mode.Supported,
                        Type = im
                    });
                }
            }
            csv.Spec.InstallModes = installModes;

            csv.Spec.Install = new NamedInstallStrategy();
            csv.Spec.Install.Strategy = "deployment";
            csv.Spec.Install.Spec = new StrategyDetailsDeployment();
            csv.Spec.Install.Spec.Permissions =
            [
                new StrategyDeploymentPermission()
                {
                    ServiceAccountName = operatorName.Name,
                    Rules = rbacAttributes.Where(attr =>
                        attr.Scope == EntityScope.Namespaced)
                            .GroupBy(
                                attr => new
                                {
                                    ApiGroups     = attr.ApiGroup,
                                    ResourceNames = attr.ResourceNames?.Split(',').Distinct().ToList(),
                                    Verbs         = attr.Verbs
                                })
                            .Select(
                                group => (
                                    Verbs:         group.Key.Verbs,
                                    ResourceNames: group.Key.ResourceNames,
                                    ApiGroups:     group.Select(attr => attr.ApiGroup).ToList(),
                                    Resources:     group.Select(attr => attr.Resource).ToList(),
                                    SubResources:  group.SelectMany(attr => (attr.SubResources?.Split(',')
                                                                                    .Distinct()
                                                                                    .Where(x => !string.IsNullOrEmpty(x)).Select(sr => $"{attr.Resource}/{sr}")) ?? Array.Empty<string>())))
                            .Select(
                                group => new V1PolicyRule
                                {
                                    ApiGroups     = group.ApiGroups.Distinct().OrderBy(x => x).ToList(),
                                    Resources     = group.Resources.Union(group.SubResources).Distinct().OrderBy(x => x).ToList(),
                                    ResourceNames = group.ResourceNames?.Count() > 0 ? group.ResourceNames.OrderBy(x => x).ToList() : null,
                                    Verbs         = group.Verbs.ToStrings(),
                                })
                            .Distinct(new PolicyRuleComparer())
                            .ToList()
                }
            ];

            csv.Spec.Install.Spec.ClusterPermissions =
            [
                new StrategyDeploymentPermission()
                {
                    ServiceAccountName = operatorName.Name,
                    Rules = rbacAttributes
                    .Where(attr => attr.Scope == EntityScope.Cluster)
                    .GroupBy(attr => new
                    {
                        ApiGroups     = attr.ApiGroup,
                        ResourceNames = attr.ResourceNames?.Split(',').Distinct().ToList(),
                        Verbs         = attr.Verbs,
                    })
                    .Select(
                        group => (
                            Verbs:         group.Key.Verbs,
                            ResourceNames: group.Key.ResourceNames,
                            ApiGroups:     group.Select(attr => attr.ApiGroup).ToList(),
                            Resources:     group.Select(attr => attr.Resource).ToList(),
                            SubResources:  group.SelectMany(attr => (attr.SubResources?.Split(',')
                                                                            .Distinct()
                                                                            .Where(x => !string.IsNullOrEmpty(x)).Select(sr => $"{attr.Resource}/{sr}")) ?? Array.Empty<string>())
                            ))
                    .Select(
                        group => new V1PolicyRule
                        {
                            ApiGroups     = group.ApiGroups.Distinct().OrderBy(x => x).ToList(),
                            Resources     = group.Resources.Union(group.SubResources).Distinct().OrderBy(x => x).ToList(),
                            ResourceNames = group.ResourceNames?.Count() > 0 ? group.ResourceNames.OrderBy(x => x).ToList() : null,
                            Verbs         = group.Verbs.ToStrings(),
                        })
                    .Distinct(new PolicyRuleComparer())
                    .ToList()
                }
            ];

            csv.Spec.Install.Spec.Deployments =
            [
                new StrategyDeploymentSpec()
                {
                    Name = operatorName.Name,
                    Label = new Dictionary<string, string>()
                    {
                        { "app.kubernetes.io/name", operatorName.Name },
                        { "app.kubernetes.io/version", version.Version },
                    },
                    Spec = new V1DeploymentSpec()
                    {
                        Replicas = 1,
                        Selector = new V1LabelSelector()
                        {
                            MatchLabels = new Dictionary<string, string>()
                            {
                                { "app.kubernetes.io/name", operatorName.Name },

                            }

                        },
                        Template = new V1PodTemplateSpec()
                        {
                            Metadata = new V1ObjectMeta()
                            {
                                Labels = new Dictionary<string, string>()
                                {
                                   { "app.kubernetes.io/name", operatorName.Name },

                                },
                                Annotations = new Dictionary<string, string>()
                                {
                                    { "prometheus.io/path", "/metrics" },
                                    { "prometheus.io/port", "9762" },
                                    { "prometheus.io/scheme", "http" },
                                    { "prometheus.io/scrape", "true" },
                                }

                            },
                            Spec = new V1PodSpec()
                            {
                                EnableServiceLinks = false,
                                ServiceAccountName = operatorName.Name,
                                Containers =
                                [
                                    new V1Container()
                                    {
                                        Name = operatorName.Name,
                                        Image = $"{containerImage.Repository}:{containerImage.Tag}",
                                        Env =
                                        [
                                            new V1EnvVar()
                                            {
                                                Name = "LISTEN_PORT",
                                                Value = "5000",
                                            },
                                            new V1EnvVar()
                                            {
                                                Name = "METRICS_PORT",
                                                Value = "9762",
                                            },
                                            new V1EnvVar()
                                            {
                                                Name = "KUBERNETES_NAMESPACE",
                                                ValueFrom = new V1EnvVarSource()
                                                {
                                                    FieldRef = new V1ObjectFieldSelector()
                                                    {
                                                        FieldPath = "metadata.namespace"
                                                    }
                                                }
                                            },
                                            new V1EnvVar()
                                            {
                                                Name = "WATCH_NAMESPACE",
                                                ValueFrom = new V1EnvVarSource()
                                                {
                                                    FieldRef = new V1ObjectFieldSelector()
                                                    {
                                                        FieldPath = "metadata.annotations['olm.targetNamespaces']"
                                                    }
                                                }

                                            },

                                         ],
                                        Ports =
                                        [
                                            new V1ContainerPort()
                                            {
                                                Name = "http",
                                                ContainerPort = 5000,
                                                Protocol = "TCP"
                                            },
                                            new V1ContainerPort()
                                            {
                                                Name = "http-metrics",
                                                ContainerPort = 9762,
                                                Protocol = "TCP"

                                            },
                                        ],
                                        LivenessProbe = new V1Probe()
                                        {
                                            HttpGet = new V1HTTPGetAction()
                                            {
                                                Path = "/healthz",
                                                Port = "http",

                                            },

                                        },
                                        ReadinessProbe = new V1Probe()
                                        {
                                            HttpGet = new V1HTTPGetAction()
                                            {
                                                Path ="/ready",
                                                Port = "http",
                                            }

                                        }
                                    }
                                ]
                            }
                        }
                    },
                }
            ];

            if (webhooks?.Count > 0)
            {
                csv.Spec.WebHookDefinitions = new List<WebHookDescription>();

                foreach (var webhook in webhooks)
                {
                    var webhookNs = webhook.GetNamespace();

                    var webhookSystemType = metadataLoadContext.ResolveType($"{webhookNs}.{webhook.Identifier.ValueText}");

                    IAssemblySymbol assemblySymbol = context.Compilation.SourceModule.ReferencedAssemblySymbols.Last();
                    var members = assemblySymbol.GlobalNamespace.
                             GetNamespaceMembers();

                    var webhookAttribute = webhookSystemType.GetCustomAttribute<WebhookAttribute>();
                    var webhookRules     = webhookSystemType.GetCustomAttributes<WebhookRuleAttribute>();
                    var typeMembers      = context
                        .Compilation
                        .SourceModule
                        .ReferencedAssemblySymbols
                        .SelectMany(
                            ras => ras.GlobalNamespace.GetNamespaceMembers())
                        .SelectMany(nsm => nsm.GetTypeMembers());

                    var webhookEntityType = webhook
                            .DescendantNodes()?
                            .OfType<BaseListSyntax>()?
                            .Where(dn => dn.DescendantNodes()?.OfType<GenericNameSyntax>()?.Any(gns =>
                                gns.Identifier.ValueText.EndsWith("IValidatingWebhook")
                                || gns.Identifier.ValueText.EndsWith("ValidatingWebhookBase") == true
                                || gns.Identifier.ValueText.EndsWith("IMutatingWebhook")
                                || gns.Identifier.ValueText.EndsWith("MutatingWebhookBase")) == true).FirstOrDefault();

                    var webhookTypeIdentifier          = webhookEntityType.DescendantNodes().OfType<IdentifierNameSyntax>().Single();

                    var sdf = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

                    var webhookTypeIdentifierNamespace = webhookTypeIdentifier.GetNamespace();

                    var webhookEntityTypeIdentifier     = namedTypeSymbols.Where(ntm => ntm.MetadataName == webhookTypeIdentifier.Identifier.ValueText).SingleOrDefault();
                    var webhookEntityFullyQualifiedName = webhookEntityTypeIdentifier.ToDisplayString(sdf);

                    var entitySystemType = metadataLoadContext.ResolveType(webhookEntityTypeIdentifier);

                    var componentType = webhookSystemType.GetCustomAttributes<OperatorComponentAttribute>(true).FirstOrDefault();

                    csv.Spec.WebHookDefinitions.Add
                        (
                            new WebHookDescription()
                            {
                                Type = ToWebhookAdmissionType(componentType.ComponentType),
                                GenerateName = webhookAttribute.Name,
                                AdmissionReviewVersions = webhookAttribute.AdmissionReviewVersions.ToList(),
                                ContainerPort = 5000,
                                TargetPort = 5000,
                                DeploymentName = operatorName.Name,
                                FailurePolicy = webhookAttribute.FailurePolicy.ToMemberString(),
                                SideEffects = webhookAttribute.SideEffects.ToMemberString(),

                                Rules = webhookRules.Select(r => new V1RuleWithOperations()
                                {
                                    ApiGroups = r.ApiGroups.ToList(),
                                    ApiVersions = r.ApiVersions.ToList(),
                                    Operations = r.Operations.ToList(),
                                    Resources = r.Resources.ToList(),
                                }).ToList(),
                                WebHookPath = CreateEndpoint(entitySystemType, webhookSystemType, ToWebhookAdmissionType(componentType.ComponentType)),
                            }
                        );
                }
            }

            var outputString = KubernetesYaml.Serialize(csv);
            var outputBaseDir = Path.Combine(targetDir, "OperatorLifecycleManager");
            var manifestDir = Path.Combine(outputBaseDir, "manifests");

            if (!Directory.Exists(manifestDir))
            {
                Directory.CreateDirectory(manifestDir);
            }

            var csvPath = Path.Combine(manifestDir, $"{operatorName.Name.ToLower()}.clusterserviceversion.yaml");
            File.WriteAllText(csvPath, outputString);

            var metadataDir = Path.Combine(outputBaseDir, "metadata");

            if (!Directory.Exists(metadataDir))
            {
                Directory.CreateDirectory(metadataDir);
            }

            var annotations = $@"annotations:
  operators.operatorframework.io.bundle.mediatype.v1: ""registry+v1""
  operators.operatorframework.io.bundle.manifests.v1: ""manifests/""
  operators.operatorframework.io.bundle.metadata.v1: ""metadata/""
  operators.operatorframework.io.bundle.package.v1: ""{operatorName.Name.ToLower()}""
  operators.operatorframework.io.bundle.channels.v1: ""stable""
  operators.operatorframework.io.bundle.channel.default.v1: ""stable""
";
            var annotationsPath = Path.Combine(metadataDir, "annotations.yaml");
            File.WriteAllText(annotationsPath, annotations);

            var dockerFile = $@"FROM scratch

LABEL operators.operatorframework.io.bundle.mediatype.v1=registry+v1
LABEL operators.operatorframework.io.bundle.manifests.v1=manifests/
LABEL operators.operatorframework.io.bundle.metadata.v1=metadata/
LABEL operators.operatorframework.io.bundle.package.v1={operatorName.Name.ToLower()}
LABEL operators.operatorframework.io.bundle.channels.v1=beta,stable
LABEL operators.operatorframework.io.bundle.channel.default.v1=stable

ADD ./manifests/*.yaml /manifests/
ADD ./metadata/annotations.yaml /metadata/annotations.yaml
";
            var dockerfilePath = Path.Combine(outputBaseDir, "Dockerfile");
            File.WriteAllText(dockerfilePath, dockerFile);


        }

        public static WebhookAdmissionType ToWebhookAdmissionType(OperatorComponentType componentType) => componentType switch
        {
            OperatorComponentType.ValidationWebhook => WebhookAdmissionType.ValidatingAdmissionWebhook,
            OperatorComponentType.MutationWebhook => WebhookAdmissionType.MutatingAdmissionWebhook,
            _ => throw new ArgumentException()
        };


        public List<CrdDescription> GetOwnedEntities(GeneratorExecutionContext context, MetadataLoadContext metadataLoadContext, List<AttributeSyntax> attributes)
        {
            try
            {
                var results = new List<CrdDescription>();

                var assemblyAttributes = context.Compilation.Assembly.GetAttributes();
                var ownedEntities = assemblyAttributes
                .Where(a => a.AttributeClass.GetFullMetadataName().StartsWith($"{typeof(OwnedEntityAttribute).Namespace}.OwnedEntity"));

                foreach (var entity in ownedEntities)
                {
                    var etype      = (INamedTypeSymbol)entity.AttributeClass.TypeArguments.FirstOrDefault();
                    var entityType = metadataLoadContext.ResolveType(etype);
                    var metadata   = entityType.GetCustomAttribute<KubernetesEntityAttribute>();

                    var ownedAttribute = attributes.Where(a => a.Name is GenericNameSyntax)
                    .Where(a => ((GenericNameSyntax)a.Name).Identifier.ValueText == "OwnedEntity"
                      && ((IdentifierNameSyntax)((GenericNameSyntax)a.Name).TypeArgumentList.Arguments.First()).Identifier.ValueText == entityType.Name);

                    var description = ownedAttribute
                    .First().ArgumentList.Arguments
                    .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(OwnedEntityAttribute.Description))
                    .FirstOrDefault()
                    ?.Expression.GetExpressionValue<string>(metadataLoadContext);

                    var displayName = ownedAttribute
                    .First().ArgumentList.Arguments
                    .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(OwnedEntityAttribute.DisplayName))
                    .FirstOrDefault()
                    ?.Expression.GetExpressionValue<string>(metadataLoadContext);

                    var dependents = entityType.CustomAttributes
                    .Where(a => a.AttributeType.GetGenericTypeDefinition().Equals(typeof(DependentResourceAttribute<>)))
                    .Select(a => a.AttributeType.GenericTypeArguments.First())
                    .ToList();

                    var crdDescription = new CrdDescription()
                    {
                        Name        = $"{metadata.PluralName}.{metadata.Group}",
                        Version     = metadata.ApiVersion,
                        Kind        = metadata.Kind,
                        DisplayName = displayName,
                        Description = description
                    };

                    crdDescription.Resources = dependents?.Select(d => new ApiResourceReference()
                    {
                        Kind = d.GetCustomAttribute<KubernetesEntityAttribute>().Kind,
                        Version = d.GetCustomAttribute<KubernetesEntityAttribute>().ApiVersion,
                        Name = d.GetCustomAttribute<KubernetesEntityAttribute>().PluralName
                    }).ToList();

                    results.Add(crdDescription);
                }

                return results;
            }
            catch
            {
                return default;
            }

        }

        public List<CrdDescription> GetRequiredEntities(GeneratorExecutionContext context, MetadataLoadContext metadataLoadContext, List<AttributeSyntax> attributes)
        {
            try
            {
                var results = new List<CrdDescription>();

                var assemblyAttributes = context.Compilation.Assembly.GetAttributes();
                var requiredEntities = assemblyAttributes
                .Where(a => a.AttributeClass.GetFullMetadataName().StartsWith($"{typeof(RequiredEntityAttribute).Namespace}.RequiredEntity"));

                foreach (var entity in requiredEntities)
                {
                    var etype      = (INamedTypeSymbol)entity.AttributeClass.TypeArguments.FirstOrDefault();
                    var entityType = metadataLoadContext.ResolveType(etype);
                    var metadata   = entityType.GetCustomAttribute<KubernetesEntityAttribute>();

                    var requiredAttribute = attributes.Where(a => a.Name is GenericNameSyntax)
                    .Where(a => ((GenericNameSyntax)a.Name).Identifier.ValueText == "RequiredEntity"
                      && ((IdentifierNameSyntax)((GenericNameSyntax)a.Name).TypeArgumentList.Arguments.First()).Identifier.ValueText == entityType.Name);

                    var description = requiredAttribute
                    .First().ArgumentList.Arguments
                    .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(RequiredEntityAttribute.Description))
                    .FirstOrDefault()
                    ?.Expression.GetExpressionValue<string>(metadataLoadContext);

                    var displayName = requiredAttribute
                    .First().ArgumentList.Arguments
                    .Where(a => a.NameEquals.Name.Identifier.ValueText == nameof(RequiredEntityAttribute.DisplayName))
                    .FirstOrDefault()
                    ?.Expression.GetExpressionValue<string>(metadataLoadContext);

                    var dependents = entityType.CustomAttributes
                    .Where(a => a.AttributeType.GetGenericTypeDefinition().Equals(typeof(DependentResourceAttribute<>)))
                    .Select(a => a.AttributeType.GenericTypeArguments.First())
                    .ToList();

                    var crdDescription = new CrdDescription()
                    {
                        Name        = $"{metadata.PluralName}.{metadata.Group}",
                        Version     = metadata.ApiVersion,
                        Kind        = metadata.Kind,
                        DisplayName = displayName,
                        Description = description
                    };

                    crdDescription.Resources = dependents?.Select(d => new ApiResourceReference()
                    {
                        Kind = d.GetCustomAttribute<KubernetesEntityAttribute>().Kind,
                        Version = d.GetCustomAttribute<KubernetesEntityAttribute>().ApiVersion,
                        Name = d.GetCustomAttribute<KubernetesEntityAttribute>().PluralName
                    }).ToList();

                    results.Add(crdDescription);
                }

                return results;
            }
            catch
            {
                return default;
            }
        }

        private string CreateEndpoint(Type entityType, Type webhookImplementation, WebhookAdmissionType webhookAdmissionType)
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

            switch (webhookAdmissionType)
            {
                case WebhookAdmissionType.MutatingAdmissionWebhook:
                    builder.Append("/mutate");
                    break;
                case WebhookAdmissionType.ValidatingAdmissionWebhook:
                    builder.Append("/validate");
                    break;
            }

            return builder.ToString().ToLowerInvariant();
        }
    }


    public static class OlmExtensions
    {
        public static T GetCustomAttribute<T>(
            this AttributeSyntax attributeData,
            MetadataLoadContext metadataLoadContext,
            Compilation compilation)
        {
            if (attributeData == null)
            {
                return default(T);
            }

            T attribute;

            // Check for constructor arguments
            if (attributeData.ArgumentList.Arguments.Any(a => a.NameEquals == null)
                && typeof(T).GetConstructors().Any(c => c.GetParameters().Length > 0))
            {
                // create instance with constructor args
                var actualArgs = attributeData.ArgumentList.Arguments
                    .Where(a => a.NameEquals == null)
                    .Select(a => a.Expression.GetExpressionValue(metadataLoadContext)).ToArray();

                attribute = (T)Activator.CreateInstance(typeof(T), actualArgs);
            }
            else
            {
                attribute = (T)Activator.CreateInstance(typeof(T));
            }

            // check and et named arguments
            foreach (var p in attributeData.ArgumentList.Arguments.Where(a => a.NameEquals != null))
            {
                var propertyName = p.NameEquals.Name.Identifier.ValueText;

                object value = null;
                //if (p.Expression is BinaryExpressionSyntax
                //    && ((BinaryExpressionSyntax)p.Expression).Kind() == SyntaxKind.BitwiseOrExpression)
                //{
                //    value = ((BinaryExpressionSyntax)p.Expression).GetEnumValue(metadataLoadContext);
                //}
                //else
                //{
                //}
                value = p.Expression.GetExpressionValue(metadataLoadContext);

                var propertyInfo = typeof(T).GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(attribute, value);
                    continue;
                }

                var fieldInfo = typeof(T).GetField(propertyName);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(attribute, value);
                    continue;
                }

                throw new Exception($"No field or property {p}");
            }
            return attribute;
        }




    }
}
