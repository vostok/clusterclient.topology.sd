namespace Vostok.Clusterclient.Topology.SD.DataStructures.TimedCircularBuffer
{
    public class TimedCircularBufferSettings
    {
        public EmptyBucketHandlingStrategy EmptyBucketHandlingStrategy { get; set; } = EmptyBucketHandlingStrategy.FillValueAsDefault;
    }
}