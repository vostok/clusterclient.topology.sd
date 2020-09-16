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
            string EnvironmentProvider() =>  environment;
            self.ClusterProvider = new ServiceDiscoveryClusterProvider(serviceLocator, EnvironmentProvider, application, self.Log);
            self.TargetEnvironmentProvider = EnvironmentProvider;
            self.TargetServiceName = application;
        }

        /// <summary>
        /// Sets up an <see cref="IClusterProvider"/> that will fetch replicas of <paramref name="application"/>from ServiceDiscovery with given <paramref name="serviceLocator"/>.
        /// The target environment will be taken from FlowingContext `forced.sd.environment`, with fallback value specified in <paramref name="defaultEnvironment"/>.
        /// </summary>
        public static void SetupServiceDiscoveryTopologyWithContextForcing(
            [NotNull] this IClusterClientConfiguration self,
            [NotNull] IServiceLocator serviceLocator,
            [NotNull] string defaultEnvironment,
            [NotNull] string application)
        {
            string EnvironmentProvider() => FlowingContext.Properties.Get<string>(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment) ?? defaultEnvironment;

            self.ClusterProvider = new ServiceDiscoveryClusterProvider(serviceLocator, EnvironmentProvider, application, self.Log);
            self.TargetEnvironmentProvider = EnvironmentProvider;
            self.TargetServiceName = application;
        }
    }
}