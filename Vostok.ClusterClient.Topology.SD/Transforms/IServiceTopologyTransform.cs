using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.Transforms
{
    [PublicAPI]
    public interface IServiceTopologyTransform
    {
        [NotNull]
        IEnumerable<Uri> Transform([NotNull] IServiceTopology topology);
    }
}