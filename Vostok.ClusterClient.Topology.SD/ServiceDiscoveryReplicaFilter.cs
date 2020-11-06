using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Clusterclient.Topology.SD.Helpers;
using Vostok.Commons.Collections;
using Vostok.Commons.Helpers.Topology;
using Vostok.Logging.Abstractions;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Abstractions.Models;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD
{
    /// <summary>
    /// An implementation of <see cref="IReplicaFilter"/> based on ServiceDiscovery replica tags.
    /// </summary>
    [PublicAPI]
    public class ServiceDiscoveryReplicaFilter : IReplicaFilter
    {
        private readonly IServiceLocator serviceLocator;
        private readonly string environment;
        private readonly string application;
        private readonly ILog log;

        private readonly CachingTransform<IServiceTopology, IReadOnlyDictionary<Uri, TagCollection>> transform;

        public ServiceDiscoveryReplicaFilter([NotNull] IServiceLocator serviceLocator, [NotNull] string environment, [NotNull] string application, [CanBeNull] ILog log)
        {
            this.serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.log = log ?? LogProvider.Get();

            transform = new CachingTransform<IServiceTopology, IReadOnlyDictionary<Uri, TagCollection>>(ParseTags);
        }

        /// <summary>
        /// <para>Returns filtered given <paramref name="replicas"/> based on filter from <paramref name="requestContext"/> <see cref="IRequestContext.Parameters"/>.</para>
        /// <para>Use <see cref="RequestParametersExtensions.SetTagsFilter(RequestParameters, Func{TagCollection, bool})"/> to add filter rules for request.</para>
        /// <para>If filter is null method returns initial replicas.</para>
        /// </summary>
        public IEnumerable<Uri> Filter(IEnumerable<Uri> replicas, IRequestContext requestContext)
        {
            var filter = requestContext.Parameters.GetTagsFilter();
            if (filter == null)
                return replicas;

            var tags = transform.Get(serviceLocator.Locate(environment, application));
            if (tags == null)
                return replicas;

            var list = new List<Uri>();
            foreach (var replica in replicas)
            {
                var tagCollection = tags.TryGetValue(replica, out var collection) ? collection : new TagCollection();
                if (filter(tagCollection)) // todo exception
                    list.Add(replica);
            }

            return list;
        }

        private IReadOnlyDictionary<Uri, TagCollection> ParseTags(IServiceTopology serviceTopology)
        {
            if (serviceTopology == null)
            {
                LogTopologyNotFound();
                return null;
            }

            var serviceTags = serviceTopology.Properties.GetTags();
            var replicaTagsDictionary = new Dictionary<Uri, TagCollection>(serviceTags.Count, ReplicaComparer.Instance);
            foreach (var replicaTags in serviceTags)
                replicaTagsDictionary.Add(replicaTags.Key, replicaTags.Value);
            return replicaTagsDictionary;
        }

        #region Logging

        private void LogTopologyNotFound()
        {
            log.Warn("Topology of '{Application}' application in '{Environment}' environment was not found in ServiceDiscovery.", application, environment);
        }

        #endregion
    }
}