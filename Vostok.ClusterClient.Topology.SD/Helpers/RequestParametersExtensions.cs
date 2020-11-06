using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.ServiceDiscovery.Abstractions.Models;

namespace Vostok.Clusterclient.Topology.SD.Helpers
{
    [PublicAPI]
    public static class RequestParametersExtensions
    {
        internal const string RequestParametersTagsFilterKey = "ReplicaTagsFilter";

        /// <summary>
        /// Sets given <paramref name="filterFunc"/> replicas filtering function based on replica ServiceDiscovery <see cref="TagCollection"/> to <paramref name="requestParameters"/>.
        /// </summary>
        public static RequestParameters SetTagsFilter(this RequestParameters requestParameters, Func<TagCollection, bool> filterFunc)
            => requestParameters.WithProperty(RequestParametersTagsFilterKey, filterFunc);

        internal static Func<TagCollection, bool> GetTagsFilter(this RequestParameters requestParameters) =>
            requestParameters.Properties.TryGetValue(RequestParametersTagsFilterKey, out var filterObject)
                ? filterObject as Func<TagCollection, bool>
                : null;
    }
}