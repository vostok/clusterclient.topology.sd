using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.Transforms
{
    /// <summary>
    /// By implementing this interface, you can transform replica set from SD.
    /// For example, this is a good place to implement caching.
    /// </summary>
    [PublicAPI]
    public interface IServiceTopologyTransform
    {
        [NotNull]
        IEnumerable<Uri> Transform([NotNull] IServiceTopology topology);
    }
}