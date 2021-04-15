using System;

namespace Vostok.Clusterclient.Topology.SD.ReplicasParsers
{
    public class ReplicasHistoryState
    {
        public ReplicasHistoryState(int maxObservedAliveReplicas, Uri[] buffer, int aliveCount, int staleCount)
        {
            MaxObservedAliveReplicas = maxObservedAliveReplicas;
            this.aliveCount = aliveCount;
            
            AliveAndStaleReplicas = new ArraySegment<Uri>(buffer, 0, aliveCount + staleCount);
            aliveReplicas = new ArraySegment<Uri>(buffer, 0, aliveCount);
        }

        public readonly int MaxObservedAliveReplicas;
        public ArraySegment<Uri> AliveAndStaleReplicas;
        public ArraySegment<Uri> aliveReplicas;
        public readonly int aliveCount;
    }
}