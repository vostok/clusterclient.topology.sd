using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Context;

namespace Vostok.Clusterclient.Topology.SD
{
    [PublicAPI]
    public class ForcedSdEnvironmentClusterClientSettings
    {
        [NotNull]
        public Func<string> EnvironmentProvider { get; set; } = () => (string)FlowingContext.Properties.Current[ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment];

        [NotNull]
        public string Application { get; set; }

        [NotNull]
        public ClusterClientSetup ClusterClientSetup { get; set; }
    }
}