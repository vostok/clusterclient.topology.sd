using JetBrains.Annotations;
using Vostok.Clusterclient.Topology.SD.Transforms;

namespace Vostok.Clusterclient.Topology.SD
{
    [PublicAPI]
    public class ServiceDiscoveryClusterProviderSettings
    {
        /// <summary>
        /// If not set, replicas will be extracted from the IServiceTopology as is.
        /// Blacklist will be applied in <see cref="ServiceDiscoveryClusterProvider" /> after calling this Transform.
        /// </summary>
        [CanBeNull]
        public IServiceTopologyTransform ServiceTopologyTransform { get; set; }
    }
}