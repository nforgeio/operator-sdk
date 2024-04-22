//-----------------------------------------------------------------------------
// FILE:	    KubernetesExtensions.cs
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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text.Json.Serialization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Microsoft.Extensions.Logging;

using Neon.Common;
using Neon.Retry;
using Neon.Tasks;

namespace Neon.K8s
{
    /// <summary>
    /// Kubernetes related extension methods.
    /// </summary>
    public static partial class KubernetesExtensions
    {
        //---------------------------------------------------------------------
        // Shared fields

        private static readonly JsonSerializerOptions serializeOptions;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static KubernetesExtensions()
        {
            serializeOptions = new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            serializeOptions.Converters.Add(new JsonStringEnumMemberConverter());
        }

        //---------------------------------------------------------------------
        // V1ObjectMeta extensions

        /// <summary>
        /// Sets a label within the metadata, constructing the label dictionary when necessary.
        /// </summary>
        /// <param name="metadata">The metadata instance.</param>
        /// <param name="name">The label name.</param>
        /// <param name="value">Optionally specifies a label value.  This defaults to an empty string.</param>
        public static void SetLabel(this V1ObjectMeta metadata, string name, string value = null)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));

            if (metadata.Labels == null)
            {
                metadata.Labels = new Dictionary<string, string>();
            }

            metadata.Labels[name] = value ?? string.Empty;
        }

        /// <summary>
        /// Sets a collection of labels within the metadata, constructing the label dictionary when necessary.
        /// </summary>
        /// <param name="metadata">The metadata instance.</param>
        /// <param name="labels">The dictionary of labels to set.</param>
        public static void SetLabels(this V1ObjectMeta metadata, Dictionary<string, string> labels)
        {
            Covenant.Requires<ArgumentNullException>(labels != null, nameof(labels));

            foreach (var label in labels)
            {
                metadata.SetLabel(label.Key, label.Value);
            }
        }

        /// <summary>
        /// Fetches the value of a label from the metadata.
        /// </summary>
        /// <param name="metadata">The metadata instance.</param>
        /// <param name="name">The label name.</param>
        /// <returns>The label value or <c>null</c> when the label doesn't exist.</returns>
        public static string GetLabel(this V1ObjectMeta metadata, string name)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));

            if (metadata.Labels == null)
            {
                return null;
            }

            if (metadata.Labels.TryGetValue(name, out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Sets an annotation within the metadata, constructing the label dictionary when necessary.
        /// </summary>
        /// <param name="metadata">The metadata instance.</param>
        /// <param name="name">The annotation name.</param>
        /// <param name="value">Optionally specifies a annotation value. This defaults to an empty string.</param>
        public static void SetAnnotation(this V1ObjectMeta metadata, string name, string value = null)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));

            if (metadata.Annotations == null)
            {
                metadata.Annotations = new Dictionary<string, string>();
            }

            metadata.Annotations[name] = value ?? string.Empty;
        }

        /// <summary>
        /// Sets a collection of annotations within the metadata, constructing the label dictionary when necessary.
        /// </summary>
        /// <param name="metadata">The metadata instance.</param>
        /// <param name="annotations">The dictionary of annotations to set.</param>
        public static void SetAnnotations(this V1ObjectMeta metadata, Dictionary<string, string> annotations)
        {
            Covenant.Requires<ArgumentNullException>(annotations != null, nameof(annotations));

            foreach (var annotation in annotations)
            {
                metadata.SetAnnotation(annotation.Key, annotation.Value);
            }
        }

        /// <summary>
        /// Fetches the value of a annotation from the metadata.
        /// </summary>
        /// <param name="metadata">The metadata instance.</param>
        /// <param name="name">The annotation name.</param>
        /// <returns>The label value or <c>null</c> when the label doesn't exist.</returns>
        public static string GetAnnotation(this V1ObjectMeta metadata, string name)
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));

            if (metadata.Annotations == null)
            {
                return null;
            }

            if (metadata.Annotations.TryGetValue(name, out var value))
            {
                return value;
            }
            else
            {
                return null;
            }
        }

        //---------------------------------------------------------------------
        // Deployment extensions

        /// <summary>
        /// Restarts a <see cref="V1Deployment"/>.
        /// </summary>
        /// <param name="deployment">The target deployment.</param>
        /// <param name="k8s">The <see cref="IKubernetes"/> client to be used for the operation.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task RestartAsync(this V1Deployment deployment, IKubernetes k8s, CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(k8s != null, nameof(k8s));

            // $todo(jefflill):
            //
            // Fish out the k8s client from the deployment so we don't have to pass it in as a parameter.

            var generation = deployment.Status.ObservedGeneration;

            var patchStr = $@"
{{
    ""spec"": {{
        ""template"": {{
            ""metadata"": {{
                ""annotations"": {{
                    ""kubectl.kubernetes.io/restartedAt"": ""{DateTime.UtcNow.ToString("s")}""
                }}
            }}
        }}
    }}
}}";

            await k8s.AppsV1.PatchNamespacedDeploymentAsync(
                body:               new V1Patch(patchStr, V1Patch.PatchType.MergePatch),
                name:               deployment.Name(),
                namespaceParameter: deployment.Namespace(),
                cancellationToken:  cancellationToken);

            await NeonHelper.WaitForAsync(
                async () =>
                {
                    try
                    {
                        var newDeployment = await k8s.AppsV1.ReadNamespacedDeploymentAsync(
                            name:               deployment.Name(),
                            namespaceParameter: deployment.Namespace(),
                            cancellationToken:  cancellationToken);

                        return newDeployment.Status.ObservedGeneration > generation;
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout:           TimeSpan.FromSeconds(300),
                pollInterval:      TimeSpan.FromMilliseconds(500),
                cancellationToken: cancellationToken);

            await NeonHelper.WaitForAsync(
                async () =>
                {
                    try
                    {
                        deployment = await k8s.AppsV1.ReadNamespacedDeploymentAsync(
                            name:               deployment.Name(),
                            namespaceParameter: deployment.Namespace(),
                            cancellationToken:  cancellationToken);

                        return (deployment.Status.Replicas == deployment.Status.AvailableReplicas) && deployment.Status.UnavailableReplicas == null;
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout:           TimeSpan.FromSeconds(300),
                pollInterval:      TimeSpan.FromMilliseconds(500),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Restarts a <see cref="V1StatefulSet"/>.
        /// </summary>
        /// <param name="statefulset">The deployment being restarted.</param>
        /// <param name="k8s">The <see cref="IKubernetes"/> client to be used for the operation.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task RestartAsync(this V1StatefulSet statefulset, IKubernetes k8s, CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(k8s != null, nameof(k8s));

            // $todo(jefflill):
            //
            // Fish out the k8s client from the statefulset so we don't have to pass it in as a parameter.

            var generation = statefulset.Status.ObservedGeneration;

            var patchStr = $@"
{{
    ""spec"": {{
        ""template"": {{
            ""metadata"": {{
                ""annotations"": {{
                    ""kubectl.kubernetes.io/restartedAt"": ""{DateTime.UtcNow.ToString("s")}""
                }}
            }}
        }}
    }}
}}";

            await k8s.AppsV1.PatchNamespacedStatefulSetAsync(
                body:               new V1Patch(patchStr, V1Patch.PatchType.MergePatch),
                name:               statefulset.Name(),
                namespaceParameter: statefulset.Namespace(),
                cancellationToken:  cancellationToken);

            await NeonHelper.WaitForAsync(
                async () =>
                {
                    try
                    {
                        var newDeployment = await k8s.AppsV1.ReadNamespacedStatefulSetAsync(
                            name:               statefulset.Name(),
                            namespaceParameter: statefulset.Namespace(),
                            cancellationToken:  cancellationToken);

                        return newDeployment.Status.ObservedGeneration > generation;
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout:           TimeSpan.FromSeconds(300),
                pollInterval:      TimeSpan.FromMilliseconds(500),
                cancellationToken: cancellationToken);

            await NeonHelper.WaitForAsync(
                async () =>
                {
                    try
                    {
                        statefulset = await k8s.AppsV1.ReadNamespacedStatefulSetAsync(
                            name:               statefulset.Name(),
                            namespaceParameter: statefulset.Namespace(),
                            cancellationToken:  cancellationToken);

                        return (statefulset.Status.Replicas == statefulset.Status.ReadyReplicas) && statefulset.Status.UpdatedReplicas == null;
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout:           TimeSpan.FromSeconds(300),
                pollInterval:      TimeSpan.FromMilliseconds(500),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Restarts a <see cref="V1DaemonSet"/>.
        /// </summary>
        /// <param name="daemonset">The daemonset being restarted.</param>
        /// <param name="k8s">The <see cref="IKubernetes"/> client to be used for the operation.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task RestartAsync(this V1DaemonSet daemonset, IKubernetes k8s, CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(k8s != null, nameof(k8s));

            // $todo(jefflill):
            //
            // Fish out the k8s client from the statefulset so we don't have to pass it in as a parameter.

            var generation = daemonset.Status.ObservedGeneration;

            var patchStr = $@"
{{
    ""spec"": {{
        ""template"": {{
            ""metadata"": {{
                ""annotations"": {{
                    ""kubectl.kubernetes.io/restartedAt"": ""{DateTime.UtcNow.ToString("s")}""
                }}
            }}
        }}
    }}
}}";

            await k8s.AppsV1.PatchNamespacedDaemonSetAsync(
                body:               new V1Patch(patchStr, V1Patch.PatchType.MergePatch),
                name:               daemonset.Name(),
                namespaceParameter: daemonset.Namespace(),
                cancellationToken:  cancellationToken);

            await NeonHelper.WaitForAsync(
                async () =>
                {
                    try
                    {
                        var newDeployment = await k8s.AppsV1.ReadNamespacedDaemonSetAsync(
                            name:               daemonset.Name(),
                            namespaceParameter: daemonset.Namespace(),
                            cancellationToken:  cancellationToken);

                        return newDeployment.Status.ObservedGeneration > generation;
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout:           TimeSpan.FromSeconds(300),
                pollInterval:      TimeSpan.FromMilliseconds(500),
                cancellationToken: cancellationToken);

            await NeonHelper.WaitForAsync(
                async () =>
                {
                    try
                    {
                        daemonset = await k8s.AppsV1.ReadNamespacedDaemonSetAsync(
                            name:               daemonset.Name(),
                            namespaceParameter: daemonset.Namespace(),
                            cancellationToken:  cancellationToken);

                        return (daemonset.Status.CurrentNumberScheduled == daemonset.Status.NumberReady) && daemonset.Status.UpdatedNumberScheduled == null;
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout:           TimeSpan.FromSeconds(300),
                pollInterval:      TimeSpan.FromMilliseconds(500),
                cancellationToken: cancellationToken);
        }

        //---------------------------------------------------------------------
        // IKubernetesObject extensions

        // Used to cache [KubernetesEntityAttribute] values for custom resource types
        // for better performance (avoiding unnecessary reflection).

        private class CustomResourceMetadata
        {
            public CustomResourceMetadata(KubernetesEntityAttribute attr)
            {
                this.Group           = attr.Group;
                this.ApiVersion      = attr.ApiVersion;
                this.Kind            = attr.Kind;
                this.GroupApiVersion = $"{attr.Group}/{attr.ApiVersion}";
            }

            public string Group             { get; private set; }
            public string ApiVersion        { get; private set; }
            public string Kind              { get; private set; }
            public string GroupApiVersion   { get; private set; }
        }

        private static Dictionary<Type, CustomResourceMetadata> typeToKubernetesEntity = new ();

        /// <summary>
        /// Initializes a custom Kubernetes object's metadata <b>Group</b>, <b>ApiVersion</b>, and
        /// <b>Kind</b> properties from the <see cref="KubernetesEntityAttribute"/> attached to the
        /// object's type.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <exception cref="InvalidDataException">Thrown when the object's type does not have a <see cref="KubernetesEntityAttribute"/>.</exception>
        /// <remarks>
        /// <para>
        /// This should be called in all custom object constructors to ensure that the object's
        /// metadata is configured and matches what was specified in the attribute.  Here's
        /// what this will look like:
        /// </para>
        /// <code language="C#">
        /// [KubernetesEntity(Group = "mygroup.io", ApiVersion = "v1", Kind = "my-resource", PluralName = "my-resources")]
        /// [KubernetesEntityShortNames]
        /// [EntityScope(EntityScope.Cluster)]
        /// [Description("My custom resource.")]
        /// public class V1MyCustomResource : CustomKubernetesEntity&lt;V1ContainerRegistry.V1ContainerRegistryEntitySpec&gt;
        /// {
        ///     public V1ContainerRegistry()
        ///     {
        ///         ((IKubernetesObject)this).InitializeMetadata();
        ///     }
        ///
        ///     ...
        /// </code>
        /// </remarks>
        public static void SetMetadata(this IKubernetesObject obj)
        {
            var objType = obj.GetType();

            CustomResourceMetadata customMetadata;

            lock (typeToKubernetesEntity)
            {
                if (!typeToKubernetesEntity.TryGetValue(objType, out customMetadata))
                {
                    var entityAttr = objType.GetCustomAttribute<KubernetesEntityAttribute>();

                    if (entityAttr == null)
                    {
                        throw new InvalidDataException($"Custom Kubernetes resource type [{objType.FullName}] does not have a [{nameof(KubernetesEntityAttribute)}].");
                    }

                    customMetadata = new CustomResourceMetadata(entityAttr);

                    typeToKubernetesEntity.Add(objType, customMetadata);
                }
            }

            obj.ApiVersion = customMetadata.GroupApiVersion;
            obj.Kind       = customMetadata.Kind;
        }

        //---------------------------------------------------------------------
        // IKubernetes client extensions.

        // $note(jefflill):
        //
        // These methods are not currently added automatically to the generated [KubernetesWithRetry]
        // class.  We need to add these manually in the [KubernetesWithRetry.manual.cs] file.

        /// <summary>
        /// Adds a new Kubernetes secret or updates an existing secret.
        /// </summary>
        /// <param name="k8s">The <see cref="Kubernetes"/> client.</param>
        /// <param name="secret">The secret.</param>
        /// <param name="namespaceParameter">Specifies the namespace.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The updated secret.</returns>
        public static async Task<V1Secret> UpsertNamspacedSecretAsync(
            this ICoreV1Operations  k8s, 
            V1Secret                secret, 
            string                  namespaceParameter,
            CancellationToken       cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(secret != null, nameof(secret));

            if ((await k8s.ListNamespacedSecretAsync(namespaceParameter, cancellationToken: cancellationToken)).Items.Any(s => s.Metadata.Name == secret.Name()))
            {
                return await k8s.ReplaceNamespacedSecretAsync(
                    body:               secret,
                    name:               secret.Name(),
                    namespaceParameter: namespaceParameter,
                    cancellationToken:  cancellationToken);
            }
            else
            {
                return await k8s.CreateNamespacedSecretAsync(
                    body:               secret,
                    namespaceParameter: namespaceParameter,
                    cancellationToken:  cancellationToken);
            }
        }

        /// <summary>
        /// Waits for a service deployment to start successfully.
        /// </summary>
        /// <param name="k8sAppsV1">The <see cref="Kubernetes"/> client's <see cref="IAppsV1Operations"/>.</param>
        /// <param name="namespaceParameter">The namespace.</param>
        /// <param name="name">Optionally specifies the deployment name.</param>
        /// <param name="labelSelector">Optionally specifies a label selector.</param>
        /// <param name="fieldSelector">Optionally specifies a field selector.</param>
        /// <param name="pollInterval">Optionally specifies the polling interval.  This defaults to 1 second.</param>
        /// <param name="timeout">Optopnally specifies the operation timeout.  This defaults to 30 seconds.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>x
        /// <remarks>
        /// One of <paramref name="name"/>, <paramref name="labelSelector"/>, or <paramref name="fieldSelector"/>
        /// must be specified.
        /// </remarks>
        public static async Task WaitForDeploymentAsync(
            this IAppsV1Operations  k8sAppsV1, 
            string                  namespaceParameter, 
            string                  name              = null, 
            string                  labelSelector     = null,
            string                  fieldSelector     = null,
            TimeSpan                pollInterval      = default,
            TimeSpan                timeout           = default,
            CancellationToken       cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentException>(!string.IsNullOrEmpty(name) || labelSelector != null || fieldSelector != null, "One of [name], [labelSelector] or [fieldSelector] must be specified.");
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(namespaceParameter), nameof(namespaceParameter));

            if (pollInterval <= TimeSpan.Zero)
            {
                pollInterval = TimeSpan.FromSeconds(1);
            }

            if (timeout <= TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(30);
            }

            if (!string.IsNullOrEmpty(name))
            {
                if (!string.IsNullOrEmpty(fieldSelector))
                {
                    fieldSelector += $",metadata.name={name}";
                }
                else
                {
                    fieldSelector = $"metadata.name={name}";
                }
            }

            await NeonHelper.WaitForAsync(
                async () =>
                {
                    try
                    {
                        var deployments = await k8sAppsV1.ListNamespacedDeploymentAsync(
                            namespaceParameter: namespaceParameter,
                            fieldSelector:      fieldSelector,
                            labelSelector:      labelSelector,
                            cancellationToken:  cancellationToken);

                        if (deployments == null || deployments.Items.Count == 0)
                        {
                            return false;
                        }

                        return deployments.Items.All(deployment => deployment.Status.AvailableReplicas == deployment.Spec.Replicas
                                                            && deployment.Status.ReadyReplicas == deployment.Status.Replicas);
                    }
                    catch
                    {
                        return false;
                    }
                            
                },
                timeout:           timeout,
                pollInterval:      pollInterval,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Waits for a stateful set to start successfully.
        /// </summary>
        /// <param name="k8sAppsV1">The <see cref="Kubernetes"/> client's <see cref="IAppsV1Operations"/>.</param>
        /// <param name="namespaceParameter">The namespace.</param>
        /// <param name="name">Optionally specifies the stateful set name..</param>
        /// <param name="labelSelector">Optionally specifies a label selector.</param>
        /// <param name="fieldSelector">Optionally specifies a field selector.</param>
        /// <param name="pollInterval">Optionally specifies the polling interval.  This defaults to 1 second.</param>
        /// <param name="timeout">Optopnally specifies the operation timeout.  This defaults to 30 seconds.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        /// <remarks>
        /// One of <paramref name="name"/>, <paramref name="labelSelector"/>, or <paramref name="fieldSelector"/>
        /// must be specified.
        /// </remarks>
        public static async Task WaitForStatefulSetAsync(
            this IAppsV1Operations  k8sAppsV1,
            string                  namespaceParameter,
            string                  name              = null,
            string                  labelSelector     = null,
            string                  fieldSelector     = null,
            TimeSpan                pollInterval      = default,
            TimeSpan                timeout           = default,
            CancellationToken       cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentException>(!string.IsNullOrEmpty(name) || labelSelector != null || fieldSelector != null, "One of [name], [labelSelector] or [fieldSelector] must be passed.");
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(namespaceParameter), nameof(namespaceParameter));

            if (pollInterval <= TimeSpan.Zero)
            {
                pollInterval = TimeSpan.FromSeconds(1);
            }

            if (timeout <= TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(30);
            }

            if (!string.IsNullOrEmpty(name))
            {
                if (!string.IsNullOrEmpty(fieldSelector))
                {
                    fieldSelector += $",metadata.name={name}";
                }
                else
                {
                    fieldSelector = $"metadata.name={name}";
                }
            }

            await NeonHelper.WaitForAsync(
                async () =>
                {
                    try
                    {
                        var statefulsets = await k8sAppsV1.ListNamespacedStatefulSetAsync(
                            namespaceParameter: namespaceParameter,
                            fieldSelector:      fieldSelector,
                            labelSelector:      labelSelector,
                            cancellationToken:  cancellationToken);

                        if (statefulsets == null || statefulsets.Items.Count == 0)
                        {
                            return false;
                        }

                        return statefulsets.Items.All(@set => @set.Status.AvailableReplicas == @set.Spec.Replicas
                                                            && @set.Status.ReadyReplicas == @set.Status.Replicas);
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout:           timeout,
                pollInterval:      pollInterval,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Waits for a daemon set to start successfully.
        /// </summary>
        /// <param name="k8sAppsV1">The <see cref="Kubernetes"/> client's <see cref="IAppsV1Operations"/>.</param>
        /// <param name="namespaceParameter">The namespace.</param>
        /// <param name="name">Optionally specifies the daemonset name.</param>
        /// <param name="labelSelector">Optionally specifies a label selector.</param>
        /// <param name="fieldSelector">Optionally specifies a field selector.</param>
        /// <param name="pollInterval">Optionally specifies the polling interval.  This defaults to 1 second.</param>
        /// <param name="timeout">Optopnally specifies the operation timeout.  This defaults to 30 seconds.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        /// <remarks>
        /// One of <paramref name="name"/>, <paramref name="labelSelector"/>, or <paramref name="fieldSelector"/>
        /// must be specified.
        /// </remarks>
        public static async Task WaitForDaemonsetAsync(

            this IAppsV1Operations  k8sAppsV1,
            string                  namespaceParameter,
            string                  name              = null,
            string                  labelSelector     = null,
            string                  fieldSelector     = null,
            TimeSpan                pollInterval      = default,
            TimeSpan                timeout           = default,
            CancellationToken       cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentException>(!string.IsNullOrEmpty(name) || labelSelector != null || fieldSelector != null, "One of [name], [labelSelector] or [fieldSelector] must be passed.");
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(namespaceParameter), nameof(namespaceParameter));

            if (pollInterval <= TimeSpan.Zero)
            {
                pollInterval = TimeSpan.FromSeconds(1);
            }

            if (timeout <= TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(30);
            }

            if (!string.IsNullOrEmpty(name))
            {
                if (!string.IsNullOrEmpty(fieldSelector))
                {
                    fieldSelector += $",metadata.name={name}";
                }
                else
                {
                    fieldSelector = $"metadata.name={name}";
                }
            }
            await NeonHelper.WaitForAsync(
                async () =>
                {
                    try
                    {
                        var daemonsets = await k8sAppsV1.ListNamespacedDaemonSetAsync(
                            namespaceParameter: namespaceParameter,
                            fieldSelector:      fieldSelector,
                            labelSelector:      labelSelector,
                            cancellationToken:  cancellationToken);

                        if (daemonsets == null || daemonsets.Items.Count == 0)
                        {
                            return false;
                        }

                        return daemonsets.Items.All(@set => @set.Status.NumberAvailable == @set.Status.DesiredNumberScheduled
                                                            && @set.Status.NumberReady == @set.Status.DesiredNumberScheduled);
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout:           timeout,
                pollInterval:      pollInterval,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Waits for a pod to start successfully.
        /// </summary>
        /// <param name="k8sCoreV1">The <see cref="Kubernetes"/> client's <see cref="ICoreV1Operations"/>.</param>
        /// <param name="name">The pod name.</param>
        /// <param name="namespaceParameter">The namespace.</param>
        /// <param name="pollInterval">Optionally specifies the polling interval.  This defaults to 1 second.</param>
        /// <param name="timeout">Optopnally specifies the operation timeout.  This defaults to 30 seconds.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>x
        public static async Task WaitForPodAsync(
            this ICoreV1Operations  k8sCoreV1, 
            string                  name, 
            string                  namespaceParameter, 
            TimeSpan                pollInterval      = default,
            TimeSpan                timeout           = default,
            CancellationToken       cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentException>(!string.IsNullOrEmpty(name), nameof(name));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(namespaceParameter), nameof(namespaceParameter));

            if (pollInterval <= TimeSpan.Zero)
            {
                pollInterval = TimeSpan.FromSeconds(1);
            }

            if (timeout <= TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(30);
            }

            await NeonHelper.WaitForAsync(
                async () =>
                {
                    try
                    {
                        var pod = await k8sCoreV1.ReadNamespacedPodAsync(
                            name:               name,
                            namespaceParameter: namespaceParameter,
                            cancellationToken:  cancellationToken);

                        return pod.Status.Phase == "Running" &&
                                pod.Status.Conditions.Any(c => c.Type == "Ready" && c.Status == "True");
                    }
                    catch
                    {
                        return false;
                    }
                            
                },
                timeout:           timeout,
                pollInterval:      pollInterval,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Waits for a custom resource definition to be created in the API server.
        /// </summary>
        /// <typeparam name="TEntity">Specifies the custom resource type.</typeparam>
        /// <param name="k8sApiextensionsV1">The <see cref="Kubernetes"/> client's <see cref="IApiextensionsV1Operations"/>.</param>
        /// <param name="pollInterval">Optionally specifies the polling interval.  This defaults to 5 seconds.</param>
        /// <param name="timeout">Optionally specifies the maximum time to wait.  This defaults to 90 seconds.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task WaitForCustomResourceDefinitionAsync<TEntity>(
            this IApiextensionsV1Operations k8sApiextensionsV1,
            TimeSpan                        pollInterval      = default,
            TimeSpan                        timeout           = default,
            CancellationToken               cancellationToken = default)

            where TEntity : IKubernetesObject<V1ObjectMeta>
        {
            await SyncContext.Clear;

            if (pollInterval <= TimeSpan.Zero)
            {
                pollInterval = TimeSpan.FromSeconds(5);
            }

            if (timeout <= TimeSpan.Zero)
            {
                timeout = TimeSpan.FromSeconds(90);
            }

            await NeonHelper.WaitForAsync(
                async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var typeMetadata    = typeof(TEntity).GetKubernetesTypeMetadata();
                        var pluralNameGroup = string.IsNullOrEmpty(typeMetadata.Group) ? typeMetadata.PluralName : $"{typeMetadata.PluralName}.{typeMetadata.Group}";
                        var existingList    = await k8sApiextensionsV1.ListCustomResourceDefinitionAsync(
                            fieldSelector:     $"metadata.name={pluralNameGroup}",
                            cancellationToken: cancellationToken
                            );
                        var existingCustomResourceDefinition = existingList?.Items?.SingleOrDefault();

                        if (existingCustomResourceDefinition != null)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch
                    {
                        return false;
                    }
                },
                timeout:           timeout,
                pollInterval:      pollInterval,
                timeoutMessage:    $"Timeout waiting for CRD: {typeof(TEntity).FullName}",
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Returns a running pod within the specified namespace that matches a label selector. 
        /// </summary>
        /// <param name="k8sCoreV1">The <see cref="Kubernetes"/> client's <see cref="ICoreV1Operations"/>.</param>
        /// <param name="namespaceParameter">Specifies the namespace hosting the pod.</param>
        /// <param name="labelSelector">
        /// Specifies the label selector to constrain the set of pods to be targeted.
        /// This is required.
        /// </param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The <see cref="V1Pod"/>.</returns>
        /// <exception cref="KubernetesException">Thrown when no healthy pods exist.</exception>
        public static async Task<V1Pod> GetNamespacedRunningPodAsync(
            this ICoreV1Operations  k8sCoreV1,
            string                  namespaceParameter,
            string                  labelSelector,
            CancellationToken       cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(namespaceParameter), nameof(namespaceParameter));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(labelSelector), nameof(labelSelector));

            var pods = (await k8sCoreV1.ListNamespacedPodAsync(
                namespaceParameter: namespaceParameter,
                labelSelector:      labelSelector,
                cancellationToken:  cancellationToken)).Items;
            var pod  =  pods.FirstOrDefault(pod => pod.Status.Phase == "Running");

            if (pod == null)
            {
                throw new KubernetesException(pods.Count > 0 ? $"[0 of {pods.Count}] pods are running." : "No deployed pods.");
            }

            return pod;
        }

        /// <summary>
        /// Executes a command within a pod container.
        /// </summary>
        /// <param name="k8s">The <see cref="Kubernetes"/> client.</param>
        /// <param name="name">Specifies the target pod name.</param>
        /// <param name="namespaceParameter">Specifies the namespace hosting the pod.</param>
        /// <param name="container">Identifies the target container within the pod.</param>
        /// <param name="command">Specifies the program and arguments to be executed.</param>
        /// <param name="noSuccessCheck">Optionally disables the <see cref="ExecuteResponse.EnsureSuccess"/> check.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>An <see cref="ExecuteResponse"/> with the command exit code and output and error text.</returns>
        /// <exception cref="ExecuteException">Thrown if the exit code isn't zero and <paramref name="noSuccessCheck"/><c>=false</c>.</exception>
        public static async Task<ExecuteResponse> NamespacedPodExecAsync(
            this IKubernetes        k8s,
            string                  name,
            string                  namespaceParameter,
            string                  container,
            string[]                command,
            bool                    noSuccessCheck    = false,
            CancellationToken       cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(name != null, nameof(name));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(namespaceParameter), nameof(namespaceParameter));
            Covenant.Requires<ArgumentNullException>(command != null, nameof(command));
            Covenant.Requires<ArgumentException>(command.Length > 0, nameof(command));
            Covenant.Requires<ArgumentException>(!string.IsNullOrEmpty(command[0]), nameof(command));
            Covenant.Requires<ArgumentNullException>(container != null, nameof(container));

            var stdOut = "";
            var stdErr = "";

            var handler = new ExecAsyncCallback(async (_stdIn, _stdOut, _stdError) =>
            {
                stdOut = Encoding.UTF8.GetString(await _stdOut.ReadToEndAsync());
                stdErr = Encoding.UTF8.GetString(await _stdError.ReadToEndAsync());
            });

            var exitCode = await k8s.NamespacedPodExecAsync(
                name:              name,
                @namespace:        namespaceParameter,
                container:         container,
                command:           command,
                tty:               false,
                action:            handler,
                cancellationToken: cancellationToken);

            var response = new ExecuteResponse(exitCode, stdOut, stdErr);

            if (!noSuccessCheck)
            {
                response.EnsureSuccess();
            }

            return response;
        }

        /// <summary>
        /// Executes a command within a pod container with a <see cref="IRetryPolicy"/>
        /// </summary>
        /// <param name="k8s">The <see cref="Kubernetes"/> client.</param>
        /// <param name="retryPolicy">The <see cref="IRetryPolicy"/>.</param>
        /// <param name="name">Specifies the target pod name.</param>
        /// <param name="namespaceParameter">Specifies the namespace hosting the pod.</param>
        /// <param name="container">Identifies the target container within the pod.</param>
        /// <param name="command">Specifies the program and arguments to be executed.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>An <see cref="ExecuteResponse"/> with the command exit code and output and error text.</returns>
        /// <exception cref="ExecuteException">Thrown if the exit code isn't zero.</exception>
        public static async Task<ExecuteResponse> NamespacedPodExecWithRetryAsync(
            this IKubernetes    k8s,
            IRetryPolicy        retryPolicy,
            string              name,
            string              namespaceParameter,
            string              container,
            string[]            command,
            CancellationToken   cancellationToken = default)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(retryPolicy != null, nameof(retryPolicy));

            return await retryPolicy.InvokeAsync(
                async () =>
                {
                    return await k8s.NamespacedPodExecAsync(
                        name:               name,
                        namespaceParameter: namespaceParameter,
                        container:          container,
                        command:            command,
                        cancellationToken:  cancellationToken,
                        noSuccessCheck:     true);
                },
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Watches a Kubernetes resource with a callback.
        /// </summary>
        /// <typeparam name="T">The type parameter.</typeparam>
        /// <param name="k8s">The <see cref="IKubernetes"/> instance.</param>
        /// <param name="actionAsync">The async action called as watch events are received.</param>
        /// <param name="namespaceParameter">Optionally specifies a Kubernetes namespace.</param>
        /// <param name="fieldSelector">Optionally specifies a field selector</param>
        /// <param name="labelSelector">Optionally specifies a label selector</param>
        /// <param name="resourceVersion">Optionally specifies a resource version.</param>
        /// <param name="resourceVersionMatch">Optionally specifies a <b>resourceVersionMatch</b> setting.</param>
        /// <param name="timeoutSeconds">Optionally specifies a timeout override.</param>
        /// <param name="retryDelay">Optionally specifies a delay period to wait between watch errors.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <param name="logger">Optionally specifies a <see cref="ILogger"/>.</param>
        /// <returns>The tracking <see cref="Task"/>.</returns>
        public static async Task WatchAsync<T>(
            this IKubernetes           k8s,
            Func<WatchEvent<T> , Task> actionAsync,
            string                     namespaceParameter   = null,
            string                     fieldSelector        = null,
            string                     labelSelector        = null,
            string                     resourceVersion      = null,
            string                     resourceVersionMatch = null,
            int?                       timeoutSeconds       = null,
            TimeSpan?                  retryDelay           = null,
            CancellationToken          cancellationToken    = default,
            ILogger                    logger               = null)

            where T : IKubernetesObject<V1ObjectMeta>, new()
        {
            using (var watcher = new Watcher<T>(k8s, logger))
            {
                await watcher.WatchAsync(actionAsync,
                    namespaceParameter,
                    fieldSelector:        fieldSelector,
                    labelSelector:        labelSelector,
                    resourceVersion:      resourceVersion,
                    resourceVersionMatch: resourceVersionMatch,
                    timeoutSeconds:       timeoutSeconds,
                    retryDelay:           retryDelay,
                    cancellationToken:    cancellationToken);
            }
        }

        /// <summary>
        /// This is a convenience method that creates a new <see cref="Watcher{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type parameter.</typeparam>
        /// <param name="k8s">The <see cref="IKubernetes"/> instance.</param>
        /// <param name="logger">Optionally specifies a <see cref="ILogger"/>.</param>
        /// <returns></returns>
        public static Watcher<T> CreateWatcher<T>(
            this IKubernetes k8s,
            ILogger          logger = null) 
            
            where T : IKubernetesObject<V1ObjectMeta>, new()
        {
            return new Watcher<T>(k8s, logger);
        }

        /// <summary>
        /// Lists pods from all cluster namespaces.
        /// </summary>
        /// <param name="k8sCoreV1">The <see cref="IKubernetes"/> instance.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The <see cref="V1PodList"/>.</returns>
        public static async Task<V1PodList> ListAllPodsAsync(this ICoreV1Operations k8sCoreV1, CancellationToken cancellationToken = default)
        {
            await SyncContext.Clear;

            // Clusters may have hundreds or even thousands of namespaces, so we don't
            // want to query for pods one namespace at a time because that may take too
            // long.  But we also don't want to slam the API server with potentially
            // thousands of pod queries all at once.
            //
            // We're going to query for all of the namespaces and then perform pod
            // queries in parallel, but limiting that concurrency to something reasonable.

            const int podListConcurency = 100;

            var namespaces = (await k8sCoreV1.ListNamespaceAsync(cancellationToken: cancellationToken)).Items;
            var pods       = new V1PodList() { Items = new List<V1Pod>() };

            await Parallel.ForEachAsync(namespaces, new ParallelOptions() { MaxDegreeOfParallelism = podListConcurency },
                async (@namespace, cancellationToken) =>
                {
                    var namespacedPods = await k8sCoreV1.ListNamespacedPodAsync(@namespace.Name(), cancellationToken: cancellationToken);

                    lock (pods)
                    {
                        foreach (var pod in namespacedPods.Items)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            pods.Items.Add(pod);
                        }
                    }
                });

            return pods;
        }

        //---------------------------------------------------------------------
        // Namespaced typed configmap extensions.

        /// <summary>
        /// Creates a namespace scoped typed configmap.
        /// </summary>
        /// <typeparam name="TConfigMapData">Specifies the configmap data type.</typeparam>
        /// <param name="k8sCoreV1">The <see cref="Kubernetes"/> client's <see cref="ICoreV1Operations"/>.</param>
        /// <param name="typedConfigMap">Specifies the typed configmap.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The new <see cref="TypedConfigMap{TConfigMap}"/>.</returns>
        /// <remarks>
        /// Typed configmaps are <see cref="V1ConfigMap"/> objects that wrap a strongly typed
        /// object formatted using the <see cref="TypedConfigMap{TConfigMap}"/> class.  This
        /// makes it easy to persist and retrieve typed data to a Kubernetes cluster.
        /// </remarks>
        public static async Task<TypedConfigMap<TConfigMapData>> CreateNamespacedTypedConfigMapAsync<TConfigMapData>(
            this ICoreV1Operations          k8sCoreV1,
            TypedConfigMap<TConfigMapData>  typedConfigMap,
            CancellationToken               cancellationToken = default)

            where TConfigMapData: class, new()
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(typedConfigMap != null, nameof(typedConfigMap));

            return TypedConfigMap<TConfigMapData>.From(
                await k8sCoreV1.CreateNamespacedConfigMapAsync(
                    body:               typedConfigMap.UntypedConfigMap, 
                    namespaceParameter: typedConfigMap.UntypedConfigMap.Namespace(), 
                    cancellationToken:  cancellationToken));
        }

        /// <summary>
        /// Retrieves a namespace scoped typed configmap.
        /// </summary>
        /// <typeparam name="TConfigMapData">Specifies the configmap data type.</typeparam>
        /// <param name="k8sCoreV1">The <see cref="Kubernetes"/> client's <see cref="ICoreV1Operations"/>.</param>
        /// <param name="name">Specifies the object name.</param>
        /// <param name="namespaceParameter">The target Kubernetes namespace.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The retrieved <see cref="TypedConfigMap{TConfigMap}"/>.</returns>
        /// <remarks>
        /// Typed configmaps are <see cref="V1ConfigMap"/> objects that wrap a strongly typed
        /// object formatted using the <see cref="TypedConfigMap{TConfigMap}"/> class.  This
        /// makes it easy to persist and retrieve typed data to a Kubernetes cluster.
        /// </remarks>
        public static async Task<TypedConfigMap<TConfigMapData>> ReadNamespacedTypedConfigMapAsync<TConfigMapData>(
            this ICoreV1Operations      k8sCoreV1,
            string                      name,
            string                      namespaceParameter,
            CancellationToken           cancellationToken = default)

            where TConfigMapData : class, new()
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(namespaceParameter), nameof(namespaceParameter));

            return TypedConfigMap<TConfigMapData>.From(await k8sCoreV1.ReadNamespacedConfigMapAsync(name, namespaceParameter, pretty: false, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Replaces an existing typed configmap.
        /// </summary>
        /// <typeparam name="TConfigMapData">Specifies the configmap data type.</typeparam>
        /// <param name="k8sCoreV1">The <see cref="Kubernetes"/> client's <see cref="ICoreV1Operations"/>.</param>
        /// <param name="configmap">Specifies the replacement configmap data.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The updated <see cref="TypedConfigMap{TConfigMap}"/>.</returns>
        /// <remarks>
        /// <note>
        /// This method calls <see cref="TypedConfigMap{TConfigMapData}.Update()"/> to ensure that
        /// the untyped configmap data is up-to-date before persisting the changes.
        /// </note>
        /// <para>
        /// Typed configmaps are <see cref="V1ConfigMap"/> objects that wrap a strongly typed
        /// object formatted using the <see cref="TypedConfigMap{TConfigMap}"/> class.  This
        /// makes it easy to persist and retrieve typed data to a Kubernetes cluster.
        /// </para>
        /// </remarks>
        public static async Task<TypedConfigMap<TConfigMapData>> ReplaceNamespacedTypedConfigMapAsync<TConfigMapData>(
            this ICoreV1Operations          k8sCoreV1,
            TypedConfigMap<TConfigMapData>  configmap,
            CancellationToken               cancellationToken = default)

            where TConfigMapData : class, new()
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(configmap != null, nameof(configmap));

            configmap.Update();

            return TypedConfigMap<TConfigMapData>.From(await k8sCoreV1.ReplaceNamespacedConfigMapAsync(
                body:               configmap.UntypedConfigMap, 
                name:               configmap.UntypedConfigMap.Name(), 
                namespaceParameter: configmap.UntypedConfigMap.Namespace(), 
                cancellationToken:  cancellationToken));
        }

        /// <summary>
        /// Replaces an existing typed configmap with new data.
        /// </summary>
        /// <typeparam name="TConfigMapData">Specifies the configmap data type.</typeparam>
        /// <param name="k8sCoreV1">The <see cref="Kubernetes"/> client's <see cref="ICoreV1Operations"/>.</param>
        /// <param name="data">Specifies the replacement configmap data.</param>
        /// <param name="name">Specifies the object name.</param>
        /// <param name="namespaceParameter">The target Kubernetes namespace.</param>
        /// <param name="cancellationToken">Optionally specifies a cancellation token.</param>
        /// <returns>The updated <see cref="TypedConfigMap{TConfigMap}"/>.</returns>
        /// <remarks>
        /// Typed configmaps are <see cref="V1ConfigMap"/> objects that wrap a strongly typed
        /// object formatted using the <see cref="TypedConfigMap{TConfigMap}"/> class.  This
        /// makes it easy to persist and retrieve typed data to a Kubernetes cluster.
        /// </remarks>
        public static async Task<TypedConfigMap<TConfigMapData>> ReplaceNamespacedTypedConfigMapAsync<TConfigMapData>(
            this ICoreV1Operations      k8sCoreV1,
            TConfigMapData              data,
            string                      name,
            string                      namespaceParameter,
            CancellationToken           cancellationToken = default)

            where TConfigMapData : class, new()
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(data != null, nameof(data));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(namespaceParameter), nameof(namespaceParameter));

            var configmap = new TypedConfigMap<TConfigMapData>(name, namespaceParameter, data);

            return TypedConfigMap<TConfigMapData>.From(await k8sCoreV1.ReplaceNamespacedConfigMapAsync(configmap.UntypedConfigMap, name, namespaceParameter, cancellationToken: cancellationToken));
        }
    }
}
