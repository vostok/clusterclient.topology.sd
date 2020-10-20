using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Topology.SD.Helpers
{
    internal class DynamicEnvironmentClusterClient : IClusterClient
    {
        private readonly ILog log;
        private readonly Func<string> environmentProvider;
        private readonly Func<string, ClusterClientSetup> environmentConfiguration;
        private readonly ConcurrentDictionary<string, IClusterClient> cache = new ConcurrentDictionary<string, IClusterClient>();

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

        private IClusterClient CreateClusterClient(string environment) =>
            new ClusterClient(
                log,
                configuration => { environmentConfiguration(environment)(configuration); });
    }
}