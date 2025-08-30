# Neon.Kubernetes.Resources

[![.NET Test](https://github.com/nforgeio/operator-sdk/actions/workflows/test.yaml/badge.svg?branch=master)](https://github.com/nforgeio/operator-sdk/actions/workflows/test.yaml)
[![NuGet Version](https://img.shields.io/nuget/v/Neon.Kubernetes.Resources?style=flat&logo=nuget&label=NuGet)](https://www.nuget.org/packages/Neon.Kubernetes.Resources)

[![Slack](https://img.shields.io/badge/Slack-4A154B?style=for-the-badge&logo=slack&logoColor=white)](https://communityinviter.com/apps/neonforge/neonforge)

---

Contains some useful custom resource definitions (CRDs) and other resources for working with Kubernetes.

## CRD types

Cert Manager:
- `V1Certificate`
- `V1CertificateRequest`
- `V1ClusterIssuer`

Grafana:
- `V1GrafanaDashboard`
- `V1GrafanaDatasource`

Istio:
- `V1AuthorizationPolicy`
- `V1DestinationRule`
- `V1Gateway`
- `V1ServiceEntry`
- `V1Telemetry`
- `V1VirtualService`

Prometheus:
- `V1ServiceMonitor`

Neon.Kubernetes.Resources is an open source project released under the Apache-2.0 license.
