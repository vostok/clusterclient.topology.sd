using JetBrains.Annotations;
using Vostok.Clusterclient.Topology.SD.ReplicasTransforms;

namespace Vostok.Clusterclient.Topology.SD
{
    public class ServiceDiscoveryClusterProviderSettings
    {
        [NotNull]
        public IServiceTopologyTransform ServiceTopologyTransform { get; set; } = new DirectTransform();
    }
}