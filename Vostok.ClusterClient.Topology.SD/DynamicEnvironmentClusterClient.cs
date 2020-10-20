using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Topology.SD
{
    /// <summary>
    /// A wrapper for a set of ClusterClients. Each ClusterClient is created for its designated environment.
    /// The environment and <see cref="ClusterClientSetup"/> are provided at runtime.
    /// </summary>
    [PublicAPI]
    public class DynamicEnvironmentClusterClient : IClusterClient
    {
        private readonly ILog log;
        private readonly Func<string> environmentProvider;
        private readonly Func<string, ClusterClientSetup> environmentConfiguration;
        private ConcurrentDictionary<string, IClusterClient> cache = new ConcurrentDictionary<string, IClusterClient>();

        public DynamicEnvironmentClusterClient(
            ILog log,
            Func<string> environmentProvider,
            Func<string, ClusterClientSetup> environmentConfiguration)
        {
            this.log = log;
            this.environmentProvider = environmentProvider;
            this.environmentConfiguration = environmentConfiguration;
        }

        public async Task<ClusterResult> SendAsync(
            Request request,
            RequestParameters parameters = null,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var client = cache.GetOrAdd(environmentProvider(), CreateClusterClient);
            return await client.SendAsync(request, parameters, timeout, cancellationToken).ConfigureAwait(false);
        }

        private IClusterClient CreateClusterClient(string environment)
        {
            log.Debug("Creating ClusterClient for environment={Environment}.", environment);
            return new ClusterClient(
                log,
                configuration =>
                {
                    environmentConfiguration(environment)(configuration);
                });
        }
    }
}