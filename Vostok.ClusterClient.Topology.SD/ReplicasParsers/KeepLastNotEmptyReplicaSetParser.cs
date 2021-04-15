using System;
using System.Linq;
using Vostok.Commons.Helpers.Topology;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD.ReplicasParsers
{
    public class KeepLastNotEmptyReplicaSetParser : IReplicasParser
    {
        private Uri[] lastSeenBlacklist = new Uri[0];
        private Uri[] lastSeenReplicas = null;

        public Uri[] ParseReplicas(IServiceTopology topology)
        {
            if (topology == null)
            {
                return null;
            }

            var blacklist = topology.Properties.GetBlacklist();
            var replicas = topology.Replicas.ToArray();
            if (replicas.Length == 0 && lastSeenReplicas?.Length > 0)
            {
                replicas = lastSeenReplicas;
            }

            lastSeenReplicas = replicas;
            lastSeenBlacklist = blacklist;

            return replicas
                .Except(lastSeenBlacklist, ReplicaComparer.Instance)
                .ToArray();
        }
    }
}