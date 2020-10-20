using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Context;
using Vostok.Logging.Abstractions;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD
{
    /// <summary>
    /// A ClusterClient-wrapper for <see cref="DynamicEnvironmentClusterClient"/>.
    /// Target environment is taken from 'forced.sd.environment' distributed property and <see cref="ServiceDiscoveryClusterProvider"/> is used.
    /// <seealso cref="FlowingContext.Properties"/>
    /// </summary>
    [PublicAPI]
    public class ForcedSdEnvironmentClusterClient : IClusterClient
    {
        private readonly string defaultEnvironment;
        private const string ForcedEnvironmentProperty = ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment;
        private readonly IClusterClient inner;

        static ForcedSdEnvironmentClusterClient()
        {
            FlowingContext.Configuration.RegisterDistributedProperty(ForcedEnvironmentProperty, ContextSerializers.String);
        }

        public ForcedSdEnvironmentClusterClient(
            string application,
            IServiceLocator serviceLocator,
            ILog log,
            string defaultEnvironment = "default",
            [CanBeNull] ClusterClientSetup additionalSetup = null)
        {
            this.defaultEnvironment = defaultEnvironment;
            inner = new DynamicEnvironmentClusterClient(
                log,
                GetEnvironment,
                EnvironmentConfiguration(application, serviceLocator, additionalSetup));
        }

        public async Task<ClusterResult> SendAsync(
            Request request,
            RequestParameters parameters = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            return await inner.SendAsync(request, parameters, timeout, cancellationToken).ConfigureAwait(false);
        }

        private string GetEnvironment()
        {
            var props = FlowingContext.Properties.Current;
            return props.TryGetValue(ForcedEnvironmentProperty, out var environment)
                ? (string)environment
                : defaultEnvironment;
        }

        private Func<string, ClusterClientSetup> EnvironmentConfiguration(
            string application,
            IServiceLocator serviceLocator,
            [CanBeNull] ClusterClientSetup additionalSetup)
        {
            return environment => configuration =>
            {
                configuration.SetupServiceDiscoveryTopology(serviceLocator, environment, application);
                additionalSetup?.Invoke(configuration);
            };
        }
    }
}