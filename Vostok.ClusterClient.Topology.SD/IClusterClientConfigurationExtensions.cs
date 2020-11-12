using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Topology;
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
            self.AddReplicasFilter(new ServiceDiscoveryReplicasFilter(serviceLocator, environment, application, self.Log));
            self.TargetEnvironment = environment;
            self.TargetServiceName = application;
        }

        /// <summary>
        /// Sets up an <see cref="IClusterProvider"/> that will fetch replicas of <paramref name="application"/> in <paramref name="environment"/> from ServiceDiscovery with given <paramref name="serviceLocator"/> and <paramref name="settings"/>.
        /// </summary>
        public static void SetupServiceDiscoveryTopology(
            [NotNull] this IClusterClientConfiguration self,
            [NotNull] IServiceLocator serviceLocator,
            [NotNull] string environment,
            [NotNull] string application,
            [NotNull] ServiceDiscoveryClusterProviderSettings settings)
        {
            self.ClusterProvider = new ServiceDiscoveryClusterProvider(serviceLocator, environment, application, settings, self.Log);
            self.TargetEnvironment = environment;
            self.TargetServiceName = application;
        }
    }
}