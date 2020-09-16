using Vostok.Clusterclient.Core.Topology.TargetEnvironment;
using Vostok.Context;

namespace Vostok.Clusterclient.Topology.SD
{
    public class FlowingContextTargetEnvironmentProvider : ITargetEnvironmentProvider
    {
        static FlowingContextTargetEnvironmentProvider()
        {
            FlowingContext.Configuration.RegisterDistributedProperty(
                ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment,
                ContextSerializers.String
            );
        }

        public string Find()
        {
            return FlowingContext.Properties.Get<string>(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment);
        }
    }
}