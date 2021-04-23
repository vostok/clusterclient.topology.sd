using System;
using System.Collections.Generic;
using Vostok.Logging.Abstractions;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.ReplicasTransforms
{
    public class KeepLastNonEmptyTopologyTransform : IServiceTopologyTransform
    {
        private readonly ILog log;
        private IReadOnlyList<Uri> lastSeenReplicas;

        public KeepLastNonEmptyTopologyTransform(ILog log)
        {
            this.log = log ?? LogProvider.Get();
        }

        public IEnumerable<Uri> Transform(IServiceTopology topology)
        {
            var replicas = topology.Replicas;
            if (replicas.Count == 0 && lastSeenReplicas?.Count > 0)
            {
                log.Warn("New observed topology is empty, but we have a cached one. Last seen cached topology will be used.");
                return lastSeenReplicas;
            }

            return lastSeenReplicas = replicas;
        }
    }
}