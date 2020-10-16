using System;
using System.Collections.Concurrent;
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
    [PublicAPI]
    public class ForcedSdEnvironmentClusterClient : IClusterClient
    {
        static ForcedSdEnvironmentClusterClient()
        {
            FlowingContext.Configuration.RegisterDistributedProperty(
                ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment,
                ContextSerializers.String
            );
        }

        private readonly ForcedSdEnvironmentClusterClientSettings settings;
        private readonly IServiceLocator serviceLocator;
        private readonly ILog log;
        private ConcurrentDictionary<string, IClusterClient> cache = new ConcurrentDictionary<string, IClusterClient>();

        public ForcedSdEnvironmentClusterClient(
            ForcedSdEnvironmentClusterClientSettings settings,
            IServiceLocator serviceLocator,
            ILog log)
        {
            this.settings = settings;
            this.serviceLocator = serviceLocator;
            this.log = log.ForContext(nameof(ForcedSdEnvironmentClusterClient));
        }

        public Task<ClusterResult> SendAsync(
            Request request,
            RequestParameters parameters = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var client = cache.GetOrAdd(settings.EnvironmentProvider(), CreateClusterClient);
            return client.SendAsync(request, parameters, timeout, cancellationToken);
        }

        private IClusterClient CreateClusterClient(string environment)
        {
            log.Debug("Creating ClusterClient for environment=[{Environment}] and application=[{Application}]...", environment, settings.Application);
            return new ClusterClient(
                log,
                configuration =>
                {
                    settings.ClusterClientSetup(configuration);
                    configuration.SetupServiceDiscoveryTopology(serviceLocator, environment, settings.Application);
                });
        }
    }
}