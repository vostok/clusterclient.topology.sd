using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Context;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD
{
    [PublicAPI]
    public static class IClusterClientConfigurationExtensions
    {
        /// <summary>
        /// Sets up an <see cref="IClusterProvider"/> that will fetch replicas of <paramref name="application"/> in <paramref name="environment"/> from ServiceDiscovery with given <paramref name="serviceLocator"/>
        /// </summary>
        public static void SetupServiceDiscoveryTopology(
            [NotNull] this IClusterClientConfiguration self,
            [NotNull] IServiceLocator serviceLocator,
            [NotNull] string environment,
            [NotNull] string application)
        {
            self.ClusterProvider = new ServiceDiscoveryClusterProvider(serviceLocator, environment, application, self.Log);
            self.TargetEnvironment = environment;
            self.TargetServiceName = application;
        }

        /// <summary>
        /// Sets up an <see cref="IClusterProvider"/> that will fetch replicas of <paramref name="application"/>from ServiceDiscovery with given <paramref name="serviceLocator"/>.
        /// The target environment will be taken from `forced.sd.environment` distributed property (<see cref="FlowingContext.Properties"/>) or will be considered 'default'.
        /// </summary>
        public static void SetupServiceDiscoveryTopology(
            [NotNull] this IClusterClientConfiguration self,
            [NotNull] IServiceLocator serviceLocator,
            [NotNull] string application)
        {
            var environment = FlowingContext.Properties.Get<string>(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment)
                              ?? ServiceDiscoveryConstants.DefaultEnvironment;
            self.SetupServiceDiscoveryTopology(serviceLocator, environment, application);
        }
    }
}