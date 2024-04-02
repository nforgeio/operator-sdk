//-----------------------------------------------------------------------------
// FILE:	    TestDatabaseController.cs
// CONTRIBUTOR: Marcus Bowyer
// COPYRIGHT:   Copyright © 2005-2024 by NEONFORGE LLC.  All rights reserved.
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
using System.Threading.Tasks;

using k8s;
using k8s.Models;

using Neon.Operator;
using Neon.Operator.Controllers;
using Neon.Operator.Util;

using Neon.K8s;
using System.Threading;

namespace Test.Neon.Operator
{
    public class TestDatabaseController : ResourceControllerBase<V1TestDatabase>
    {
        //---------------------------------------------------------------------
        // Instance members

        private readonly IKubernetes k8s;

        /// <summary>
        /// Constructor.
        /// </summary>
        public TestDatabaseController(IKubernetes k8s)
        {
            Covenant.Requires<ArgumentNullException>(k8s != null, nameof(k8s));
            this.k8s = k8s;
        }

        /// <inheritdoc/>
        public override async Task<ResourceControllerResult> ReconcileAsync(V1TestDatabase resource, CancellationToken cancellationToken = default)
        {
            var patch = OperatorHelper.CreatePatch<V1TestDatabase>();

            patch.Replace(path => path.Status, new TestDatabaseStatus());
            patch.Replace(path => path.Status.Status, "reconciling");

            await k8s.CustomObjects.PatchNamespacedCustomObjectStatusAsync<V1TestDatabase>(
                patch:              OperatorHelper.ToV1Patch<V1TestDatabase>(patch),
                name:               resource.Name(),
                namespaceParameter: resource.Namespace());

            var statefuSetList = await k8s.AppsV1.ListNamespacedStatefulSetAsync(resource.Metadata.Namespace(),
                labelSelector: $"app.kubernetes.io/name={resource.Name()}");

            V1StatefulSet statefulSet;

            if (statefuSetList.Items.Count > 0)
            {
                statefulSet = statefuSetList.Items[0];
            }
            else
            {
                statefulSet = new V1StatefulSet().Initialize();
                statefulSet.Metadata.Name = resource.Name();
                statefulSet.Metadata.SetNamespace(resource.Namespace());
                statefulSet.AddOwnerReference(resource.MakeOwnerReference());
            }

            statefulSet.Spec = new V1StatefulSetSpec()
            {
                Replicas = resource.Spec.Servers,
                Template = new V1PodTemplateSpec()
                {
                    Spec = new V1PodSpec()
                    {
                        Containers = new List<V1Container>()
                        {
                            new V1Container()
                            {
                                Image = resource.Spec.Image,
                            }
                        }
                    }
                },
                VolumeClaimTemplates = new List<V1PersistentVolumeClaim>()
                {
                    new V1PersistentVolumeClaim()
                    {
                        Spec = new V1PersistentVolumeClaimSpec()
                        {
                            Resources = new V1ResourceRequirements()
                            {
                                Requests = new Dictionary<string, ResourceQuantity>()
                                {
                                    { "storage", new ResourceQuantity(resource.Spec.VolumeSize)}
                                }
                            }
                        }
                    }
                }
            };

            await k8s.AppsV1.CreateNamespacedStatefulSetAsync(statefulSet, statefulSet.Namespace());

            var service = new V1Service().Initialize();
            service.Metadata.Name = resource.Name();
            service.Metadata.SetNamespace(resource.Namespace());
            service.AddOwnerReference(resource.MakeOwnerReference());

            service.Spec = new V1ServiceSpec()
            {
                Selector = new Dictionary<string, string>()
                {
                    {"app.kubernetes.io/name", resource.Name() }
                },
                Ports = new List<V1ServicePort>()
                {
                    new V1ServicePort(80)
                }
            };

            await k8s.CoreV1.CreateNamespacedServiceAsync(service, service.Namespace());

            patch = OperatorHelper.CreatePatch<V1TestDatabase>();

            patch.Replace(path => path.Status, new TestDatabaseStatus());
            patch.Replace(path => path.Status.Status, "reconciled");

            await k8s.CustomObjects.PatchNamespacedCustomObjectStatusAsync<V1TestDatabase>(
                patch:              OperatorHelper.ToV1Patch<V1TestDatabase>(patch),
                name:               resource.Name(),
                namespaceParameter: resource.Namespace());

            return Ok();
        }
    }
}