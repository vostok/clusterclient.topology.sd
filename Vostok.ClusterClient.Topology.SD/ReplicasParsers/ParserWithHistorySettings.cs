using System;
using Vostok.Clusterclient.Topology.SD.DataStructures.TimedCircularBuffer;

namespace Vostok.Clusterclient.Topology.SD.ReplicasParsers
{
    public class ParserWithHistorySettings
    {
        public int HistoryBucketCount { get; set; } = 12;
        public long HistoryBucketSizeInTicks { get; set; } = TimeSpan.FromHours(1).Ticks;
        public IValueAggregator<int> HistoricalAliveReplicasCountAggregator { get; set; } = new AverageOfTheHighsValueAggregator();
        public TimedCircularBufferSettings TimedCircularBufferSettings { get; set; } = new TimedCircularBufferSettings {EmptyBucketHandlingStrategy = EmptyBucketHandlingStrategy.KeepLastValue};
    }
}