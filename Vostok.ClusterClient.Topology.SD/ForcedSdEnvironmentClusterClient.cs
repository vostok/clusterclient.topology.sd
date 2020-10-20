using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Topology.SD.Helpers;
using Vostok.Context;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Topology.SD
{
    /// <summary>
    /// <para>A ClusterClient-wrapper with dynamic target environment.</para>
    /// <para>Target environment will be taken from 'forced.sd.environment' distributed property or <see cref="ForcedSdEnvironmentClusterClientSettings.DefaultEnvironment"/> will be used.</para>
    /// </summary>
    [PublicAPI]
    public class ForcedSdEnvironmentClusterClient : IClusterClient
    {
        private readonly IClusterClient client;
        private readonly ForcedSdEnvironmentClusterClientSettings settings;

        static ForcedSdEnvironmentClusterClient() =>
            FlowingContext.Configuration.RegisterDistributedProperty(ServiceDiscoveryConstants.ForcedEnvironmentProperty, ContextSerializers.String);

        public ForcedSdEnvironmentClusterClient([NotNull] ForcedSdEnvironmentClusterClientSettings settings, [CanBeNull] ILog log)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            client = new DynamicEnvironmentClusterClient(
                log ?? LogProvider.Get(),
                GetEnvironment,
                SetupClient);
        }

        public Task<ClusterResult> SendAsync(
            Request request,
            RequestParameters parameters = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = new CancellationToken()) =>
            client.SendAsync(request, parameters, timeout, cancellationToken);

        private string GetEnvironment() =>
            FlowingContext.Properties.Get(ServiceDiscoveryConstants.ForcedEnvironmentProperty, settings.DefaultEnvironment);

        private ClusterClientSetup SetupClient(string environment) =>
            configuration =>
            {
                configuration.SetupServiceDiscoveryTopology(settings.ServiceLocator, environment, settings.Application);
                settings.AdditionalSetup?.Invoke(configuration);
            };
    }
}