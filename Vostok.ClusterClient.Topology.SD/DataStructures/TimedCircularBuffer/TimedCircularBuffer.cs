using System;

namespace Vostok.Clusterclient.Topology.SD.DataStructures.TimedCircularBuffer
{
    public class TimedCircularBuffer<T> 
    {
        private readonly long bucketSizeInTicks;
        private readonly IValueAggregator<T> valueAggregator;
        private readonly TimedCircularBufferSettings settings;
        private readonly Bucket[] buckets;

        private long head;
        private Bucket aggregate;

        public TimedCircularBuffer(int bucketCount, long bucketSizeInTicks, IValueAggregator<T> valueAggregator, TimedCircularBufferSettings settings)
        {
            this.bucketSizeInTicks = bucketSizeInTicks;
            this.valueAggregator = valueAggregator;
            this.settings = settings;
            buckets = new Bucket[bucketCount];
        }

        public bool TryAdd(T value, long timestamp)
        {
            var index = timestamp / bucketSizeInTicks;
            if (!TryAdjustHead(index))
                return false;
            ref var bucket = ref GetBucket(index);
            valueAggregator.AggregateWithBucket(ref bucket, ref aggregate, (int)Math.Max(head, buckets.Length), value);
            return true;
        }

        /// <summary>
        /// Returns false only if <paramref name="timestamp"/> is earlier than the stored history.
        /// </summary>
        public bool TryGetAggregate(long timestamp, out T aggregatedValue)
        {
            var index = timestamp / bucketSizeInTicks;
            if (!TryAdjustHead(index))
            {
                aggregatedValue = valueAggregator.GetDefaultValue();
                return false;
            }

            aggregatedValue = aggregate.Value;
            return true;
        }

        private bool TryAdjustHead(long index)
        {
            if (index < head && head - index >= buckets.Length)
                return false;

            var lastValue = buckets[head].Value;
            var keepLastValue = settings.EmptyBucketHandlingStrategy == EmptyBucketHandlingStrategy.KeepLastValue;

            if (index - head >= buckets.Length)
            {
                //(deniaa): Too far behind. Need to remake all buckets.
                if (keepLastValue)
                {
                    for (var i = 0; i < buckets.Length; i++)
                    {
                        ref var oldBucket = ref buckets[i];
                        valueAggregator.HandleBucketRemove(ref oldBucket, ref aggregate, buckets.Length);
                        oldBucket.Value = lastValue;
                        valueAggregator.HandleNewBucket(ref oldBucket, ref aggregate, buckets.Length);
                    }

                    head = index;
                }
                else
                {
                    for (var i = 0; i < buckets.Length; i++)
                        buckets[i].Value = valueAggregator.GetDefaultValue();
                    aggregate.Value = valueAggregator.GetDefaultValue();
                    head = index;
                }
            }

            //(deniaa): Remaking stragglers buckets.
            while (head < index)
            {
                ref var oldBucket = ref GetBucket(++head);
                valueAggregator.HandleBucketRemove(ref oldBucket, ref aggregate, buckets.Length);
                oldBucket.Value = keepLastValue ? lastValue : valueAggregator.GetDefaultValue();
                valueAggregator.HandleNewBucket(ref oldBucket, ref aggregate, buckets.Length);
            }

            return true;
        }

        private ref Bucket GetBucket(long index)
        {
            return ref buckets[index % buckets.Length];
        }

        public struct Bucket
        {
            public T Value;
        }
    }
}