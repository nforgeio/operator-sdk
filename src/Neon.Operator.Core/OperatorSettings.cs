//-----------------------------------------------------------------------------
// FILE:	    OperatorSettings.cs
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
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;

using Neon.Operator.Attributes;
using Neon.Operator.ResourceManager;

using Prometheus;

namespace Neon.Operator
{
    /// <summary>
    /// Specifies global options for an operator.
    /// </summary>
    public class OperatorSettings
    {
        internal bool certManagerEnabled              = false;
        internal bool leaderElectionEnabled           = false;
        internal bool manageCustomResourceDefinitions = false;
        internal bool hasMutatingWebhooks             = false;
        internal bool hasValidatingWebhooks           = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public OperatorSettings()
        {
            if (string.IsNullOrEmpty(Name))
            {
                var nameAttribute = Assembly.GetEntryAssembly().GetCustomAttribute<NameAttribute>();
                if (nameAttribute != null)
                {
                    name = nameAttribute.Name;
                }
                else
                {
                    name = Assembly.GetEntryAssembly().GetName().Name;
                }
            }
        }

        /// <summary>
        /// The Operator name. This defaults to a Kubernetes safe version of the entry assembly name.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = Regex.Replace(value, @"([a-z])([A-Z])", "$1-$2").ToLower();
            }
        }

        private string name;

        /// <summary>
        /// Specifies the namespace to be watched or <c>null</c> for cluster scoped operators.
        /// </summary>
        public string WatchNamespace { get; set; } = null;

        /// <summary>
        /// The namespace where the operator is deployed.
        /// </summary>
        public string PodNamespace { get; set; } = "";

        /// <summary>
        /// The IP address to listen on.
        /// </summary>
        public IPAddress ListenAddress { get; set; } = IPAddress.Any;

        /// <summary>
        /// The port to listen on.
        /// </summary>
        public int Port { get; set; } = 443;

        /// <summary>
        /// <para>
        /// Specifies whether assembly scanning should be enabled. If enabled, Controllers, Finalizers and Webhooks will
        /// be scanned and added automatically. Defaults to true.
        /// </para>
        /// </summary>
        public bool AssemblyScanningEnabled { get; set; } = false;

        /// <summary>
        /// The size of the pool to use for async locks.
        /// </summary>
        public int LockPoolSize { get; set; } = 20;

        /// <summary>
        /// The number of items to fill the lock pool with during initialization.
        /// </summary>
        public int LockPoolInitialFill { get; set; } = 1;

        /// <summary>
        /// Default resource manager options that will be applied to controllers unless overridden.
        /// </summary>
        public ResourceManagerOptions ResourceManagerOptions { get; set; } = new ResourceManagerOptions();

        /// <summary>
        /// The endpoint where the metrics will be exposed.
        /// </summary>
        public string MetricsEndpoint { get; set; } = "/metrics";

        /// <summary>
        /// The endpoint where the startup check will be exposed.
        /// </summary>
        public string StartupEndpooint { get; set; } = "/healthz";

        /// <summary>
        /// The endpoint where the Liveness check will be exposed.
        /// </summary>
        public string LivenessEndpooint { get; set; } = "/healthz";

        /// <summary>
        /// The endpoint where the Readiness check will be exposed.
        /// </summary>
        public string ReadinessEndpooint { get; set; } = "/ready";

        /// <summary>
        /// The amount of time to wait between watches when an error occurs.
        /// </summary>
        public TimeSpan WatchRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Whether certs are provided by CertManager.
        /// </summary>
        public bool CertManagerEnabled 
        { 
            get
            {
                return certManagerEnabled;
            } 
        }

        /// <summary>
        /// Whether to add the "Impersonate-User" header to requests to the Kubernetes API.
        /// </summary>
        public bool? UserImpersonationEnabled { get; set; }

        /// <summary>
        /// Specifies the default <see cref="Prometheus.ExemplarBehavior"/>.
        /// </summary>
        public ExemplarBehavior ExemplarBehavior { get; set; } = new ExemplarBehavior()
        {
            DefaultExemplarProvider = (_, _) => Exemplar.FromTraceContext(),
            NewExemplarMinInterval  = TimeSpan.FromMinutes(5)
        };

        /// <summary>
        /// Validates the option properties.
        /// </summary>
        /// <exception cref="ValidationException">Thrown when any of the properties are invalid.</exception>
        public void Validate()
        {
            if (LockPoolInitialFill > LockPoolSize)
            {
                throw new ValidationException($"{nameof(LockPoolInitialFill)} can't be larger than {nameof(LockPoolSize)}");
            }
        }
    }
}
