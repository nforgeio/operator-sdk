// -----------------------------------------------------------------------------
// FILE:	    UnitTest1.cs
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

using FluentAssertions;

using k8s;
using k8s.Models;

using Neon.Kubernetes.Resources.OperatorLifecycleManager;

namespace TestOperatorLifecycleManager
{
    public class Test_Serialize
    {
        [Fact]
        public void TestSerializeV1ClusterServiceVersionEqualsExample()
        {
            var csv = new V1ClusterServiceVersion().Initialize();
            csv.EnsureMetadata().EnsureAnnotations().Add("operatorframework.io/version", "0.10.0");
            csv.Metadata.Name = "memcached-operator.v0.10.0";
            csv.Spec = new V1ClusterServiceVersionSpec()
            {
                Description = "This is an operator for memcached.",
                DisplayName = "Memcached Operator",
                Keywords = ["memcached", "app"],
                Maintainers =
                [
                    new Maintainer()
                    {
                        Email = "corp@example.com",
                        Name = "Some Corp"
                    }
                ],
                Maturity = "alpha",                                     
                Provider = new Provider()
                {
                    Name = "Example",
                    Url = "www.example.com"
                },
                Version = "0.10.0",
                MinKubeVersion = "1.16.0",
                InstallModes =
                [
                    new InstallMode()
                    {
                        Supported = true,
                        Type = InstallModeType.OwnNamespace                         
                    },
                    new InstallMode()
                    {
                        Supported = true,
                        Type = InstallModeType.SingleNamespace                      
                    },
                    new InstallMode()
                    {
                        Supported = false,
                        Type = InstallModeType.MultiNamespace                      
                    },
                    new InstallMode()
                    {
                        Supported = true,
                        Type = InstallModeType.AllNamespaces                    
                    },
                ],
                Install = new NamedInstallStrategy()
                {
                    Strategy = "deployment",                              
                    Spec = new StrategyDetailsDeployment()
                    {
                        Permissions =
                        [
                            new StrategyDeploymentPermission()
                            {
                                ServiceAccountName = "memcached-operator",
                                Rules =
                                [
                                    new V1PolicyRule()
                                    {
                                        ApiGroups = [""],
                                        Resources = ["pods"],
                                        Verbs = ["*"]
                                    }
                                ]
                            }
                        ],
                        ClusterPermissions =
                        [
                            new StrategyDeploymentPermission()
                            {
                                ServiceAccountName = "memcached-operator",
                                Rules =
                                [
                                    new V1PolicyRule()
                                    {
                                        ApiGroups = [""],
                                        Resources = ["serviceaccounts"],
                                        Verbs = ["*"]
                                    }
                                ]
                            }
                        ],
                        Deployments =
                        [
                            new StrategyDeploymentSpec()
                            {
                                Name = "memcached-operator",
                                Spec = new V1DeploymentSpec()
                                {
                                    Replicas = 1
                                }
                            }
                        ],
                    }
                },
                CustomResourceDefinitions = new CustomResourceDefinitions()
                {
                    Owned =
                    [
                        new CrdDescription()
                        {
                            Name = "memcacheds.cache.example.com",
                            Version = "v1alpha1",
                            Kind = "Memcached",
                        }
                    ],
                    Required =
                    [
                        new CrdDescription()
                        {
                            Name = "others.example.com",
                            Version = "v1alpha1",
                            Kind = "Other",
                        }
                    ]
                }
            };

            var json = KubernetesJson.Serialize(csv);
            var yaml = KubernetesYaml.Serialize(csv);

            var expectedString = $@"apiVersion: operators.coreos.com/v1alpha1
kind: ClusterServiceVersion
metadata:
  annotations:
    operatorframework.io/version: 0.10.0
  name: memcached-operator.v0.10.0
spec:
  description: This is an operator for memcached.
  displayName: Memcached Operator
  keywords:
  - memcached
  - app
  maintainers:
  - email: corp@example.com
    name: Some Corp
  maturity: alpha
  provider:
    name: Example
    url: www.example.com
  version: 0.10.0
  minKubeVersion: 1.16.0
  installModes:
  - supported: true
    type: OwnNamespace
  - supported: true
    type: SingleNamespace
  - supported: false
    type: MultiNamespace
  - supported: true
    type: AllNamespaces
  install:
    strategy: deployment
    spec:
      permissions:
      - serviceAccountName: memcached-operator
        rules:
        - apiGroups:
          - ''
          resources:
          - pods
          verbs:
          - '*'
      clusterPermissions:
      - serviceAccountName: memcached-operator
        rules:
        - apiGroups:
          - ''
          resources:
          - serviceaccounts
          verbs:
          - '*'
      deployments:
      - name: memcached-operator
        spec:
          replicas: 1
  customresourcedefinitions:
    owned:
    - name: memcacheds.cache.example.com
      version: v1alpha1
      kind: Memcached
    required:
    - name: others.example.com
      version: v1alpha1
      kind: Other";
            var expectedCsv = KubernetesYaml.Deserialize<V1ClusterServiceVersion>(expectedString);
            var expectedJson = KubernetesJson.Serialize(expectedCsv);
            var expectedYaml = KubernetesYaml.Serialize(expectedCsv);

            yaml.Should().BeEquivalentTo(expectedYaml);
            json.Should().BeEquivalentTo(expectedJson);
            csv.Should().BeEquivalentTo(expectedCsv);
        }
    }
}