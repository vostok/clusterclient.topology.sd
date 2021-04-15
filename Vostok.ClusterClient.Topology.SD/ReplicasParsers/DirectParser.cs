using System;
using System.Linq;
using Vostok.Commons.Helpers.Topology;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD.ReplicasParsers
{
    public class DirectParser : IReplicasParser
    {
        public Uri[] ParseReplicas(IServiceTopology topology)
        {
            if (topology == null)
            {
                return null;
            }

            var blacklist = topology.Properties.GetBlacklist();
            var replicas = topology.Replicas
                .Except(blacklist, ReplicaComparer.Instance)
                .ToArray();
            
            return replicas;
        }
    }
}