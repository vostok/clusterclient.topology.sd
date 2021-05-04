using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Logging.Abstractions;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.Transforms
{
    [PublicAPI]
    public class KeepLastNonEmptyTopologyTransform : IServiceTopologyTransform
    {
        private readonly ILog log;
        private volatile IReadOnlyList<Uri> lastSeenReplicas;

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