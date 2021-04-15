using JetBrains.Annotations;
using Vostok.Clusterclient.Topology.SD.ReplicasParsers;

namespace Vostok.Clusterclient.Topology.SD
{
    public class ServiceDiscoveryClusterProviderSettings
    {
        [NotNull]
        public IReplicasParser ReplicasParser = new DirectParser();
    }
}