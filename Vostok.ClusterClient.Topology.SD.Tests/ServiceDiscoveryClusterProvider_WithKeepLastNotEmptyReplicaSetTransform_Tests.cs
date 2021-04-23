using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Topology.SD.ServiceTopologyTransforms;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Abstractions.Models;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD.Tests
{
    [TestFixture]
    internal class ServiceDiscoveryClusterProvider_WithKeepLastNotEmptyReplicaSetTransform_Tests
    {
        private readonly ILog log = new SynchronousConsoleLog();

        private readonly Uri replica1 = new Uri("http://replica1:123/v1/");
        private readonly Uri replica2 = new Uri("http://replica2:456/v1/");
        private IServiceLocator serviceLocator;
        private ServiceDiscoveryClusterProvider provider;
        private string environment;
        private string application;
        private IServiceTopology topology;

        [SetUp]
        public void SetUp()
        {
            environment = "environment";
            application = "application";

            serviceLocator = Substitute.For<IServiceLocator>();
            serviceLocator.Locate(environment, application).Returns(_ => topology);

            var settings = new ServiceDiscoveryClusterProviderSettings {ServiceTopologyTransform = new KeepLastNonEmptyTopologyTransform(log)};
            provider = new ServiceDiscoveryClusterProvider(serviceLocator, environment, application, settings, log);
        }

        [Test]
        public void Should_return_null_for_null_topology()
        {
            topology = null;
            provider.GetCluster().Should().BeNull();
        }

        [Test]
        public void Should_return_empty_list_for_empty_replicas()
        {
            topology = ServiceTopology.Build(new List<Uri>(), null);

            provider.GetCluster().Should().BeEmpty();
        }

        [Test]
        public void Should_return_last_seen_non_empty_replicas_set()
        {
            var aliveReplicas = new[] {replica1, replica2};
            topology = ServiceTopology.Build(aliveReplicas, null);
            provider.GetCluster().Should().BeEquivalentTo(aliveReplicas.Cast<object>());


            topology = ServiceTopology.Build(new List<Uri>(), null);
            provider.GetCluster().Should().BeEquivalentTo(aliveReplicas.Cast<object>());
        }

        [Test]
        public void Should_return_last_seen_non_empty_replicas_set_and_ignore_blacklist_that_was_at_the_moment_of_caching()
        {
            var aliveReplicas = new[] { replica1, replica2 };
            topology = ServiceTopology.Build(aliveReplicas, CreateApplicationInfoPropertiesWithBlackList(aliveReplicas));
            provider.GetCluster().Should().BeEquivalentTo(Array.Empty<Uri>().Cast<object>());


            topology = ServiceTopology.Build(new List<Uri>(), null);
            provider.GetCluster().Should().BeEquivalentTo(aliveReplicas.Cast<object>());
        }

        [Test]
        public void Should_return_last_seen_non_empty_replicas_set_and_aply_new_blacklist_that_was_added_after_moment_of_caching()
        {
            var aliveReplicas = new[] { replica1, replica2 };
            topology = ServiceTopology.Build(aliveReplicas, null);
            provider.GetCluster().Should().BeEquivalentTo(aliveReplicas.Cast<object>());


            topology = ServiceTopology.Build(new List<Uri>(), CreateApplicationInfoPropertiesWithBlackList(new[] {replica1}));
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica2}.Cast<object>());
        }

        private IApplicationInfoProperties CreateApplicationInfoPropertiesWithBlackList(Uri[] blacklist)
        {
            var applicationInfo = new ApplicationInfo(environment, application, null);
            return applicationInfo.Properties.SetBlacklist(blacklist);
        }
    }
}