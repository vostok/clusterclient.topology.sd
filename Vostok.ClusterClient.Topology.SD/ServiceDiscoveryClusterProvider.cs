﻿using System;
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

            var replicas = (settings.ServiceTopologyTransform?.Transform(topology) ?? topology.Replicas)
                .Except(topology.Properties.GetBlacklist(), ReplicaComparer.Instance)
                .ToArray();

            if (!ReplicaListComparer.Equals(resolvedReplicas, replicas))
            {
                LogResolvedReplicas(replicas);
                resolvedReplicas = replicas;
            }

            return replicas;
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