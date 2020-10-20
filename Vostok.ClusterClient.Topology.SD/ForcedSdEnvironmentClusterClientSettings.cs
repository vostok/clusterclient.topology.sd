using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.ServiceDiscovery.Abstractions;

namespace Vostok.Clusterclient.Topology.SD
{
    [PublicAPI]
    public class ForcedSdEnvironmentClusterClientSettings
    {
        public ForcedSdEnvironmentClusterClientSettings([NotNull] IServiceLocator serviceLocator, [NotNull] string application, [NotNull] string defaultEnvironment)
        {
            ServiceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            Application = application ?? throw new ArgumentNullException(nameof(application));
            DefaultEnvironment = defaultEnvironment ?? throw new ArgumentNullException(nameof(defaultEnvironment));
        }

        [NotNull]
        public IServiceLocator ServiceLocator { get; }

        [NotNull]
        public string Application { get; }

        [NotNull]
        public string DefaultEnvironment { get; }

        [CanBeNull]
        public ClusterClientSetup AdditionalSetup { get; set; }
    }
}