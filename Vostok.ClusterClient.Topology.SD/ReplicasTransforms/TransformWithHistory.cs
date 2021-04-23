using System;
using System.Collections.Generic;
using System.Linq;
using Vostok.Clusterclient.Topology.SD.DataStructures.TimedCircularBuffer;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.ReplicasTransforms
{
    public class TransformWithHistory : IServiceTopologyTransform
    {
        private readonly double historyLengthMultiplier;
        private readonly double okayishReplicasAliveRate;
        private readonly TimedCircularBuffer<int> maxObserverReplicasCount;
        private ReplicasHistoryState state;

        public TransformWithHistory(double historyLengthMultiplier, double okayishReplicasAliveRate, TransformWithHistorySettings settings)
        {
            this.historyLengthMultiplier = historyLengthMultiplier;
            this.okayishReplicasAliveRate = okayishReplicasAliveRate;
            maxObserverReplicasCount = new TimedCircularBuffer<int>(settings.HistoryBucketCount, settings.HistoryBucketSizeInTicks, settings.HistoricalAliveReplicasCountAggregator, settings.TimedCircularBufferSettings);
        }

        public IEnumerable<Uri> Transform(IServiceTopology topology)
        {
            var replicas = topology.Replicas.ToArray();

            state = ShiftHistory(replicas, state);

            var okayishReplicasCount = (int)(state.MaxObservedAliveReplicas * okayishReplicasAliveRate);

            if (state.aliveCount >= okayishReplicasCount)
                return state.aliveReplicas.ToArray();

            return state.AliveAndStaleReplicas.Count <= okayishReplicasCount
                ? state.AliveAndStaleReplicas.ToArray()
                : state.AliveAndStaleReplicas.Take(okayishReplicasCount).ToArray();
        }

        private ReplicasHistoryState ShiftHistory(Uri[] newAliveReplicas, ReplicasHistoryState state)
        {
            var updateTime = DateTime.UtcNow.Ticks;
            if (!maxObserverReplicasCount.TryAdd(newAliveReplicas.Length, updateTime)
                || !maxObserverReplicasCount.TryGetAggregate(updateTime, out var maxObservedAliveReplicas))
                return state;

            var historyTailLength = (int)(maxObservedAliveReplicas * historyLengthMultiplier);
            var newBufferLength = historyTailLength + maxObservedAliveReplicas;
            var newBuffer = new Uri[newBufferLength];
            Array.Copy(newAliveReplicas, newBuffer, newAliveReplicas.Length);
            var alive = new HashSet<Uri>(newAliveReplicas);
            var index = newAliveReplicas.Length;
            foreach (var staleReplica in state.AliveAndStaleReplicas)
            {
                if (index >= newBuffer.Length)
                    break;

                if (staleReplica == null)
                    break;

                if (alive.Contains(staleReplica))
                    continue;

                newBuffer[index] = staleReplica;
                index++;
            }

            return new ReplicasHistoryState(maxObservedAliveReplicas, newBuffer, newAliveReplicas.Length, index - newAliveReplicas.Length);
        }
    }
}