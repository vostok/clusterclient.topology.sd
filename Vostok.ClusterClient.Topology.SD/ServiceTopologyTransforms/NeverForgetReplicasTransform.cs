using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Topology;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.ServiceTopologyTransforms
{
    [PublicAPI]
    public class NeverForgetReplicasTransform : IServiceTopologyTransform
    {
        //If replicas are "the same" but one is with fqdn and another is not, we prefer to remember the last one we saw.
        //It also make sense if the virtual path changes from http://my-vm:80/ to http://my-vm:80/foo. We prefer to remember the last one we saw too.
        private readonly Dictionary<Uri, Uri> detectedReplicas = new Dictionary<Uri, Uri>(ReplicaComparer.Instance);

        public IEnumerable<Uri> Transform(IServiceTopology topology)
        {
            foreach (var topologyReplica in topology.Replicas)
            {
                detectedReplicas[topologyReplica] = topologyReplica;
            }

            return detectedReplicas.Values;
        }
    }
}