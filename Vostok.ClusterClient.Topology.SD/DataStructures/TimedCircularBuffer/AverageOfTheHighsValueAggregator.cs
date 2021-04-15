using System;

namespace Vostok.Clusterclient.Topology.SD.DataStructures.TimedCircularBuffer
{
    public class AverageOfTheHighsValueAggregator : IValueAggregator<int>
    {
        public void AggregateWithBucket(ref TimedCircularBuffer<int>.Bucket bucket, ref TimedCircularBuffer<int>.Bucket aggregate, int observationsCount, int value)
        {
            var oldValue = bucket.Value;
            var newValue = Math.Max(oldValue, value);
            if (newValue == oldValue)
                return;

            var average = aggregate.Value;
            var averageWithoutOldValue = SubtractFromAverage(observationsCount, average, oldValue);
            var averageWithNewValue = AddToTheAverage(observationsCount, averageWithoutOldValue, newValue);

            bucket.Value = newValue;
            aggregate.Value = averageWithNewValue;
        }

        public void HandleBucketRemove(ref TimedCircularBuffer<int>.Bucket bucketToRemove, ref TimedCircularBuffer<int>.Bucket aggregate, int observationsCount)
        {
            aggregate.Value = SubtractFromAverage(observationsCount, aggregate.Value, bucketToRemove.Value);
        }

        public void HandleNewBucket(ref TimedCircularBuffer<int>.Bucket bucketToAdd, ref TimedCircularBuffer<int>.Bucket aggregate, int observationsCount)
        {
            aggregate.Value = AddToTheAverage(observationsCount, aggregate.Value, bucketToAdd.Value);
        }

        public int GetDefaultValue() => 0;

        private static int AddToTheAverage(int observationsCount, int average, int newValue) =>
            average + ((newValue - average) / observationsCount);

        private static int SubtractFromAverage(int observationsCount, int average, int oldValue) =>
            ((average * observationsCount) - oldValue) / (observationsCount - 1);
    }
}