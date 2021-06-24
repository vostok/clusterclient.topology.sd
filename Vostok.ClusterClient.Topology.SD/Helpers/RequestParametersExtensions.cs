using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.ServiceDiscovery.Abstractions.Models;
using Vostok.ServiceDiscovery.Extensions.TagFilters;

namespace Vostok.Clusterclient.Topology.SD.Helpers
{
    [PublicAPI]
    public static class RequestParametersExtensions
    {
        internal const string RequestParametersTagsFilterKey = "ReplicasTagsFilter";

        /// <summary>
        /// <para> Sets given <paramref name="replicaMatchesFunc" /> replicas filtering function based on replica ServiceDiscovery <see cref="TagCollection" /> to <paramref name="requestParameters" />. </para> 
        /// <para> If an expression derived from a <paramref name="replicaMatchesFunc" /> returns false then replica will be filtered.</para>
        /// </summary>
        public static RequestParameters SetTagsFilter(this RequestParameters requestParameters, Func<TagCollection, bool> replicaMatchesFunc)
            => requestParameters.WithProperty(RequestParametersTagsFilterKey, replicaMatchesFunc);

        /// <summary>
        /// Sets given <see cref="ITagFilter.Matches" /> realization to <paramref name="requestParameters" />.
        /// </summary>
        public static RequestParameters SetTagsFilter(this RequestParameters requestParameters, ITagFilter tagFilter)
            => requestParameters.SetTagsFilter(tagFilter.Matches);

        internal static Func<TagCollection, bool> GetTagsFilter(this RequestParameters requestParameters) =>
            requestParameters.Properties.TryGetValue(RequestParametersTagsFilterKey, out var filterObject)
                ? filterObject as Func<TagCollection, bool>
                : null;
    }
}