using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.ReplicasTransforms
{
    public interface IServiceTopologyTransform
    {
        [NotNull]
        IEnumerable<Uri> Transform([NotNull] IServiceTopology topology);
    }
}