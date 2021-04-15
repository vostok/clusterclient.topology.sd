using System;
using JetBrains.Annotations;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.ReplicasParsers
{
    public interface IReplicasParser
    {
        [CanBeNull]
        Uri[] ParseReplicas([CanBeNull] IServiceTopology topology);
    }
}