using System;
using System.Collections.Generic;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.ReplicasTransforms
{
    public class DirectTransform : IServiceTopologyTransform
    {
        public IEnumerable<Uri> Transform(IServiceTopology topology)
        {
            return topology.Replicas;
        }
    }
}