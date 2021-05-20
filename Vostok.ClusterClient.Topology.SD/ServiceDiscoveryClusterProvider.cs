using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Commons.Collections;
using Vostok.Commons.Helpers.Comparers;
using Vostok.Commons.Helpers.Topology;
using Vostok.Logging.Abstractions;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD
{
    /// <summary>
    /// An implementation of <see cref="IClusterProvider" /> that fetches topology from ServiceDiscovery.
    /// </summary>
    [PublicAPI]
    public class ServiceDiscoveryClusterProvider : IClusterProvider
    {
        private static readonly IEqualityComparer<IReadOnlyList<Uri>> ReplicaListComparer = new ListComparer<Uri>(ReplicaComparer.Instance);

        private readonly ServiceDiscoveryClusterProviderSettings settings;
        private readonly IServiceLocator serviceLocator;
        private readonly string environment;
        private readonly string application;
        private readonly ILog log;

        private readonly CachingTransform<IServiceTopology, Uri[]> transform;
        private volatile Uri[] resolvedReplicas;

        public ServiceDiscoveryClusterProvider([NotNull] IServiceLocator serviceLocator, [NotNull] string environment, [NotNull] string application, [CanBeNull] ILog log)
            : this(serviceLocator, environment, application, new ServiceDiscoveryClusterProviderSettings(), log)
        {
        }

        public ServiceDiscoveryClusterProvider(
            [NotNull] IServiceLocator serviceLocator,
            [NotNull] string environment,
            [NotNull] string application,
            [NotNull] ServiceDiscoveryClusterProviderSettings settings,
            [CanBeNull] ILog log)
        {
            this.serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.settings = settings;
            this.log = log ?? LogProvider.Get();

            transform = new CachingTransform<IServiceTopology, Uri[]>(ParseReplicas, preventParallelProcessing: true);
        }

        public IList<Uri> GetCluster()
            => transform.Get(serviceLocator.Locate(environment, application));

        [CanBeNull]
        private Uri[] ParseReplicas([CanBeNull] IServiceTopology topology)
        {
            if (topology == null)
            {
                LogTopologyNotFound();
                return null;
            }

            var aliveReplicas = settings.ServiceTopologyTransform?.Transform(topology) ?? topology.Replicas;
            aliveReplicas = AdvanceReplicasByDesiredTopology(topology, aliveReplicas);

            var replicas = aliveReplicas
                .Except(topology.Properties.GetBlacklist(), ReplicaComparer.Instance)
                .ToArray();

            if (!ReplicaListComparer.Equals(resolvedReplicas, replicas))
            {
                LogResolvedReplicas(replicas);
                resolvedReplicas = replicas;
            }

            return replicas;
        }

        private IEnumerable<Uri> AdvanceReplicasByDesiredTopology(IServiceTopology topology, IEnumerable<Uri> aliveReplicas)
        {
            var desiredTopologySettings = settings.DesiredTopologySettings;
            if (desiredTopologySettings != null && desiredTopologySettings.Enabled)
                aliveReplicas = MergeWithDesiredTopology(aliveReplicas, new HashSet<Uri>(topology.Properties.GetDesiredTopology(), ReplicaComparer.Instance), desiredTopologySettings);
            return aliveReplicas;
        }

        private IEnumerable<Uri> MergeWithDesiredTopology(IEnumerable<Uri> aliveReplicas, HashSet<Uri> desiredTopology, DesiredTopologySettings desiredTopologySettings)
        {
            if (desiredTopology == null || desiredTopology.Count == 0)
                return aliveReplicas;

            if (!(aliveReplicas is IReadOnlyCollection<Uri> aliveReplicasCollection))
                aliveReplicasCollection = aliveReplicas.ToList();

            var aliveInDesired = aliveReplicasCollection.Count(desiredTopology.Contains);
            if (aliveReplicasCollection.Count <= desiredTopologySettings.MaxReplicasCountToAlwaysAdvanceReplicasByDesiredTopology
                || aliveInDesired < desiredTopology.Count * desiredTopologySettings.MinDesiredTopologyPresenceAmongAliveToAdvance)
            {
                //TODO у нас так-то задан порядок. Можно мержить эффективнее, через расширение ReplicaListComparer
                return UnionTopologies(desiredTopology, aliveReplicasCollection);
            }

            return aliveReplicasCollection;
        }

        private static IEnumerable<Uri> UnionTopologies(HashSet<Uri> desiredTopology, IReadOnlyCollection<Uri> aliveReplicasCollection)
        {
            foreach (var uri in desiredTopology)
                yield return uri;

            foreach (var uri in aliveReplicasCollection)
                if (!desiredTopology.Contains(uri))
                    yield return uri;
        }

        #region Logging

        private void LogTopologyNotFound()
        {
            log.Warn("Topology of '{Application}' application in '{Environment}' environment was not found in ServiceDiscovery.", application, environment);
        }

        private void LogResolvedReplicas(Uri[] replicas)
        {
            if (replicas.Length == 0)
            {
                log.Info("Resolved ServiceDiscovery topology of '{Application}' application in '{Environment}' to an empty set of replicas.", application, environment);
            }
            else
            {
                log.Info(
                    "Resolved ServiceDiscovery topology of '{Application}' application in '{Environment}' to following replicas: \n\t{Replicas}",
                    application,
                    environment,
                    string.Join("\n\t", replicas as IEnumerable<Uri>));
            }
        }

        #endregion
    }
}