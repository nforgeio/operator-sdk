using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using MetadataLoadContext = Neon.Roslyn.MetadataLoadContext;

namespace Neon.Operator.Analyzers
{
    [Generator]
    public class MutatingWebhookGenerator : ISourceGenerator
    {
        private List<ClassDeclarationSyntax> mutatingWebhooks = new List<ClassDeclarationSyntax>();
        private MetadataLoadContext metadataLoadContext { get; set; }
        private GeneratorExecutionContext context { get; set; }

        private IEnumerable<INamedTypeSymbol> namedTypeSymbols { get; set; }

        private StringBuilder logString { get; set; }

        private Dictionary<Type, Type> webhookSystemTypes = new Dictionary<Type, Type>();
        public void Execute(GeneratorExecutionContext context)
        {
#pragma warning disable RS1035 // Do not use APIs banned for analyzers
            if (Environment.GetEnvironmentVariable("MUTATINGWEBHOOK_GENERATOR_DEBUG") == "1")
            {
                System.Diagnostics.Debugger.Launch();
            }
#pragma warning restore RS1035 // Do not use APIs banned for analyzers

            this.context = context;
            this.metadataLoadContext = new MetadataLoadContext(context.Compilation);
            this.mutatingWebhooks = ((MutatingWebhookReceiver)context.SyntaxReceiver)?.MutatingWebhooks;
            this.namedTypeSymbols = context.Compilation.GetNamedTypeSymbols();
            logString = new StringBuilder();
            bool hasErrors = false;

            if (mutatingWebhooks.Any())
            {
                foreach (var webhook in mutatingWebhooks)
                {
                    Log($"webook: {webhook.Identifier.ValueText}");

                    try
                    {
                        var webhookNs = webhook.GetNamespace();

                        var webhookSystemType = metadataLoadContext.ResolveType($"{webhookNs}.{webhook.Identifier.ValueText}");

                        IAssemblySymbol assemblySymbol = context.Compilation.SourceModule.ReferencedAssemblySymbols.Last();
                        var members = assemblySymbol.GlobalNamespace.
                             GetNamespaceMembers();

                        //var webhookAttribute = webhookSystemType.GetCustomAttributes();

                        //if (webhookAttribute.Ignore)
                        //{
                        //    return null;
                        //}

                        var typeMembers = context.Compilation.SourceModule.ReferencedAssemblySymbols.SelectMany(
                            ras => ras.GlobalNamespace.GetNamespaceMembers())
                            .SelectMany(nsm => nsm.GetTypeMembers());

                        var webhookEntityType = webhook
                            .DescendantNodes()?
                            .OfType<BaseListSyntax>()?
                            .Where(dn => dn.DescendantNodes()?.OfType<GenericNameSyntax>()?.Any(gns =>
                                gns.Identifier.ValueText.EndsWith("IMutatingWebhook") ||
                                gns.Identifier.ValueText.EndsWith("MutatingWebhookBase")) == true).FirstOrDefault();

                        var webhookTypeIdentifier          = webhookEntityType.DescendantNodes().OfType<IdentifierNameSyntax>().Single();

                        var sdf = new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

                        var webhookTypeIdentifierNamespace = webhookTypeIdentifier.GetNamespace();

                        var webhookEntityTypeIdentifier     = namedTypeSymbols.Where(ntm => ntm.MetadataName == webhookTypeIdentifier.Identifier.ValueText).SingleOrDefault();
                        var webhookEntityFullyQualifiedName = webhookEntityTypeIdentifier.ToDisplayString(sdf);

                        var entitySystemType = metadataLoadContext.ResolveType(webhookEntityTypeIdentifier);

                        GenerateController(
                            webhook: webhook,
                            webhookEntityTypeIdentifier: webhookEntityTypeIdentifier,
                            webhookSystemType: webhookSystemType,
                            entitySystemType: entitySystemType,
                            webhookEntityFullyQualifiedName: webhookEntityFullyQualifiedName);
                    }
                    catch (Exception e)
                    {
                        Log(e.Message);
                        hasErrors = true;
                    }
                }
            }
            if (hasErrors)
            {
                context.AddSource("log", logString.ToString());
            }


        }

        private void Log(string message)
        {
            logString.AppendLine($"// {message}");
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new MutatingWebhookReceiver());
        }



        private void GenerateController(
            ClassDeclarationSyntax webhook,
            INamedTypeSymbol webhookEntityTypeIdentifier,
            Type webhookSystemType,
            Type entitySystemType,
            string webhookEntityFullyQualifiedName)
        {
            webhookSystemTypes.Add(webhookSystemType, entitySystemType);

            var metadata = webhookEntityTypeIdentifier.GetAttributes();

            var builder  = new StringBuilder();

            var k8sattr = metadata.Where(attr => attr.AttributeClass.MetadataName.EndsWith("KubernetesEntityAttribute")).Single();
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
                webhookEntityFullyQualifiedName.TrimEnd('.').Remove(webhookEntityFullyQualifiedName.LastIndexOf('.') + 1).TrimEnd('.')
            };

            var sb = new StringBuilder();

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

            var controllerClassName = $"{webhook.Identifier.ValueText}Controller";
            var controllerNamespace = webhook.GetNamespace();

            sb.AppendLine($@"
namespace {controllerNamespace}.Controllers
{{
    [ApiController]
    public class {controllerClassName} : ControllerBase
    {{
        private WebhookMetrics<{webhookEntityTypeIdentifier.Name}> metrics;
        private IAdmissionWebhook<{webhookEntityTypeIdentifier.Name}, MutationResult> webhook;
        private OperatorSettings operatorSettings;
        private ILogger<{controllerClassName}> logger;

        public {controllerClassName}(
            IAdmissionWebhook<{webhookEntityTypeIdentifier.Name}, MutationResult> webhook,
            WebhookMetrics<{webhookEntityTypeIdentifier.Name}> metrics,
            OperatorSettings operatorSettings,
            ILogger<{controllerClassName}> logger = null)
        {{
            this.webhook = webhook;
            this.metrics = metrics;
            this.operatorSettings = operatorSettings;
            this.logger = logger;
        }}


        [HttpPost(""{route}"")]
        public async Task<ActionResult<MutationResult>> {webhookEntityTypeIdentifier.Name}WebhookAsync([FromBody] AdmissionReview<{webhookEntityTypeIdentifier.Name}> admissionRequest)
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

                MutationResult result;

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

            metrics.RequestsTotal.WithLabels(new string[] {{ operatorSettings.Name, webhook.Endpoint, response.Status?.Code.ToString() }}).Inc();

            return Ok(admissionRequest);
        }}
    }}
}}");
            var result = sb.ToString();

            context.AddSource($"{controllerClassName}.g.cs", SourceText.From(result, Encoding.UTF8, SourceHashAlgorithm.Sha256));
        }
    }
}
