using System;
using System.Diagnostics.Contracts;
using System.Security.Cryptography.X509Certificates;

using k8s;
using k8s.Models;

using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;

namespace Neon.K8s
{
    /// <summary>
    /// Helper methods.
    /// </summary>
    public static class KubeHelper
    {

        /// <summary>
        /// Constructs an <b>initialized</b> Kubernetes object of a specific type.
        /// </summary>
        /// <typeparam name="T">The Kubernetes object type.</typeparam>
        /// <param name="name">Specifies the object name.</param>
        /// <returns>The new <typeparamref name="T"/>.</returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when <typeparamref name="T"/> does not define define string <b>KubeGroup</b>, 
        /// <b>KubeApiVersion</b> and <b>KubeKind</b> constants.
        /// </exception>
        /// <remarks>
        /// <para>
        /// Unfortunately, the default constructors for objects like <see cref="V1ConfigMap"/> do not
        /// initialize the <see cref="IKubernetesObject.ApiVersion"/> and <see cref="IKubernetesObject.Kind"/>
        /// and properties even though these values will be the same for all instances of each object type.
        /// (I assume that Microsoft doesn't do this as an optimization that avoids initializing these
        /// properties and then doing that again when deserializing responses from the API server.
        /// </para>
        /// <para>
        /// This method constructs the request object and then configures its <see cref="IKubernetesObject.ApiVersion"/>
        /// and <see cref="IKubernetesObject.Kind"/> properties by reflecting <typeparamref name="T"/> and using
        /// the constant <b>KubeGroup</b>, <b>KubeApiVersion</b> and <b>KubeKind</b> values.  This is very convenient 
        /// but will be somwehat slower than setting these values explicitly but is probably worth the cost in most
        /// situations because Kubernetes objects are typically read much more often than created.
        /// </para>
        /// <note>
        /// This method requires that <typeparamref name="T"/> define string <b>KubeGroup</b> <b>KubeApiVersion</b> 
        /// and <b>KubeKind</b> constants that return the correct values for the type.
        /// </note>
        /// </remarks>
        public static T CreateKubeObject<T>(string name)
            where T : IKubernetesObject<V1ObjectMeta>, new()
        {
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(name), nameof(name));

            var metadata            = typeof(T).GetKubernetesTypeMetadata();

            var obj = new T();

            obj.ApiVersion = String.IsNullOrEmpty(metadata.Group) ? metadata.ApiVersion : $"{metadata.Group}/{metadata.ApiVersion}";
            obj.Kind       = metadata.Kind;
            obj.Metadata   = new V1ObjectMeta() { Name = name };

            return obj;
        }
        /// <summary>
        /// Helper to get a Kubernetes client.
        /// </summary>
        /// <param name="kubeConfigPath"></param>
        /// <param name="currentContext"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public static IKubernetes GetKubernetesClient(
            string         kubeConfigPath = null,
            string         currentContext = null,
            ILoggerFactory loggerFactory  = null)
        {
            KubernetesClientConfiguration config = null;

            if (kubeConfigPath != null)
            {
                config = KubernetesClientConfiguration.BuildConfigFromConfigFile(kubeconfigPath: kubeConfigPath, currentContext: currentContext);
            }
            else
            {
                config = KubernetesClientConfiguration.BuildDefaultConfig();
            }

            if (config.SslCaCerts == null)
            {
                var store = new X509Store(
                            StoreName.CertificateAuthority,
                            StoreLocation.CurrentUser);

                config.SslCaCerts = store.Certificates;
            }

            KubernetesRetryHandler retryHandler = null;
            if (loggerFactory == null)
            {
                retryHandler = new KubernetesRetryHandler();
            }
            else
            {
                retryHandler = new KubernetesRetryHandler(new LoggingHttpMessageHandler(loggerFactory.CreateLogger<IKubernetes>()));
            }

            var k8s = new k8s.Kubernetes(config, retryHandler);
            return k8s;
        }
    }
}
