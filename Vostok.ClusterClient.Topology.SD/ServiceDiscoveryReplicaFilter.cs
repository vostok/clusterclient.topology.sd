using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Clusterclient.Core.ReplicaFilter;
using Vostok.Clusterclient.Topology.SD.Helpers;
using Vostok.Commons.Collections;
using Vostok.Commons.Helpers.Topology;
using Vostok.Logging.Abstractions;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Abstractions.Models;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD
{
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

            var tags = serviceTopology.Properties.GetTags();
            var dict = new Dictionary<Uri, TagCollection>(tags.Count, ReplicaComparer.Instance);
            foreach (var tag in tags)
                dict.Add(tag.Key, tag.Value);
            return dict;
        }

        #region Logging
        
        private void LogTopologyNotFound()
        {
            log.Warn("Topology of '{Application}' application in '{Environment}' environment was not found in ServiceDiscovery.", application, environment);
        }

        #endregion
    }
}