using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Topology;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.Transforms
{
    [PublicAPI]
    public class NeverForgetReplicasTransform : IServiceTopologyTransform
    {
        private readonly HashSet<Uri> detectedReplicas = new HashSet<Uri>(ReplicaComparer.Instance);

        public IEnumerable<Uri> Transform(IServiceTopology topology)
        {
            foreach (var topologyReplica in topology.Replicas)
            {
                detectedReplicas.Add(topologyReplica);
            }

            return detectedReplicas;
        }
    }
}