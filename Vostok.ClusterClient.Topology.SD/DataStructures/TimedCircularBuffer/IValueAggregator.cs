namespace Vostok.Clusterclient.Topology.SD.DataStructures.TimedCircularBuffer
{
    public interface IValueAggregator<T>
    {
        void AggregateWithBucket(ref TimedCircularBuffer<T>.Bucket bucket, ref TimedCircularBuffer<T>.Bucket aggregate, int observationsCount, T value);
        void HandleBucketRemove(ref TimedCircularBuffer<T>.Bucket bucketToRemove, ref TimedCircularBuffer<T>.Bucket aggregate, int observationsCount);
        void HandleNewBucket(ref TimedCircularBuffer<T>.Bucket bucketToAdd, ref TimedCircularBuffer<T>.Bucket aggregate, int observationsCount);
        T GetDefaultValue();
    }
}