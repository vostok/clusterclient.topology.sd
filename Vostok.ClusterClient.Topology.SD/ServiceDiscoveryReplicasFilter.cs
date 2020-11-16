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
    /// An implementation of <see cref="IReplicasFilter" /> based on ServiceDiscovery replica tags.
    /// </summary>
    [PublicAPI]
    public class ServiceDiscoveryReplicasFilter : IReplicasFilter
    {
        private readonly TagCollection emptyCollection = new TagCollection();
        private readonly IServiceLocator serviceLocator;
        private readonly string environment;
        private readonly string application;
        private readonly ILog log;

        private readonly CachingTransform<IServiceTopology, IReadOnlyDictionary<Uri, TagCollection>> transform;

        public ServiceDiscoveryReplicasFilter([NotNull] IServiceLocator serviceLocator, [NotNull] string environment, [NotNull] string application, [CanBeNull] ILog log)
        {
            this.serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.application = application ?? throw new ArgumentNullException(nameof(application));
            this.log = log ?? LogProvider.Get();

            transform = new CachingTransform<IServiceTopology, IReadOnlyDictionary<Uri, TagCollection>>(ParseTags);
        }

        /// <summary>
        /// <para> Returns filtered given <paramref name="replicas" /> based on filter from <paramref name="requestContext" /> <see cref="IRequestContext.Parameters" />. </para>
        /// <para> Use <see cref="RequestParametersExtensions.SetTagsFilter(RequestParameters, Func{TagCollection, bool})" /> to add filter rules for request. </para>
        /// <para> If filter returns false then replica will be filtered. </para>
        /// <para> If filter is null then method returns initial replicas. </para>
        /// </summary>
        public IEnumerable<Uri> Filter(IEnumerable<Uri> replicas, IRequestContext requestContext)
        {
            var replicaMatchesFunc = requestContext.Parameters.GetTagsFilter();
            if (replicaMatchesFunc == null)
                return replicas;

            //CR: (deniaa) Была реплика r1. Мы захватили её в кластерпровайдере, они приехала сюда.
            //CR: (deniaa) На реплике был таг "v1".
            //CR: (deniaa) У нас условие "!v1". 
            //CR: (deniaa) Реплика r1 исчезла, вместе со своими тегами. Мы захватили это знание её в serviceLocator.Locate.
            //CR: (deniaa) Никаких тегов у реплики мы не найдем, условие "!v1" скажет, что реплика подходит.
            //CR: (deniaa) И, допустим, реплика на самом деле все ещё жива (просто из SD выпала).
            //CR: (deniaa) Мы отправим на неё запрос, что будет ошибкой.
            //CR: (deniaa) Проблем "в обратную сторону" с появлением новых реплик или тегов, или удалением тегов с ранее захваченных реплик (без удаления реплик) я не придумал.

            //CR: (deniaa) Чтобы поправить это "правильно", нужно пофиксить аналогичную проблему в ServiceLocator'е и сломать тонну интерфейсов и моделей:
            //CR: (deniaa) сделать все атомарным и единожды зачитаннынм из serviceLocator'а, зафиксированным на весь запрос, через все модули.
            //CR: (deniaa) Можно закостылить это здесь и сейчас - отфильтровать все те реплики, что отсутствуют в ответе от новозапрошенного Locate. "Освежить" состав реплик.

            var tags = transform.Get(serviceLocator.Locate(environment, application));
            if (tags == null)
                return replicas;

            return FilterReplicas(replicas, replicaMatchesFunc, tags);
        }

        private IEnumerable<Uri> FilterReplicas(IEnumerable<Uri> replicas, Func<TagCollection, bool> replicaMatchesFunc, IReadOnlyDictionary<Uri, TagCollection> tags)
        {
            foreach (var replica in replicas)
            {
                var tagCollection = tags.TryGetValue(replica, out var collection) ? collection : emptyCollection;
                if (replicaMatchesFunc(tagCollection))
                    yield return replica;
            }
        }

        private IReadOnlyDictionary<Uri, TagCollection> ParseTags(IServiceTopology serviceTopology)
        {
            if (serviceTopology == null)
            {
                LogTopologyNotFound();
                return null;
            }

            var serviceTags = serviceTopology.Properties.GetTags();
            //CR: (deniaa) А зачем мы делаем здесь копию? Это наш фильтр. Мы сами не портим этот словарь и не отдаем его никуда наружу.
            //CR: (deniaa) Наружу отдаются только TagCollections, но именно их копии мы здесь не делаем все равно.
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