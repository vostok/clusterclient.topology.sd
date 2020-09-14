using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core;
using Vostok.Context;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Abstractions.Models;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD.Tests
{
    [TestFixture]
    internal class ServiceDiscoveryClusterProvider_Tests
    {
        private readonly ILog log = new SynchronousConsoleLog();

        private readonly Uri replica1 = new Uri("http://replica1:123/v1/");
        private readonly Uri replica2 = new Uri("http://replica2:456/v1/");
        private IServiceLocator serviceLocator;
        private ServiceDiscoveryClusterProvider provider;
        private string environment;
        private string application;
        private IServiceTopology topology;
        private Uri[] blacklist;

        [SetUp]
        public void SetUp()
        {
            environment = "environment";
            application = "application";

            serviceLocator = Substitute.For<IServiceLocator>();
            serviceLocator.Locate(environment, application).Returns(_ => topology);

            provider = new ServiceDiscoveryClusterProvider(serviceLocator, environment, application, log);

            blacklist = null;
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
        public void Should_return_replicas()
        {
            topology = ServiceTopology.Build(new[] {replica1, replica2}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1, replica2}.Cast<object>());
        }

        [Test]
        public void Should_return_new_replicas_from_new_topology()
        {
            topology = ServiceTopology.Build(new[] {replica1, replica2}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1, replica2}.Cast<object>());

            topology = ServiceTopology.Build(new[] {replica2}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica2}.Cast<object>());
        }

        [Test]
        public void Should_filter_blacklisted_replicas()
        {
            blacklist = new[] {replica2};

            var applicationInfo = new ApplicationInfo(environment, application, null);

            topology = ServiceTopology.Build(
                new[] {replica1, replica2},
                applicationInfo.Properties.SetBlacklist(blacklist));

            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1}.Cast<object>());
        }

        [Test]
        public void Should_filter_all_blacklisted_replicas()
        {
            blacklist = new[] {replica2, replica1};

            var applicationInfo = new ApplicationInfo(environment, application, null);

            topology = ServiceTopology.Build(
                new[] {replica1, replica2},
                applicationInfo.Properties.SetBlacklist(blacklist));

            provider.GetCluster().Should().BeEmpty();
        }

        [Test]
        public void Should_filter_blacklisted_replicas_with_FQDN()
        {
            var r1 = new Uri("http://razr01:80");
            var r2 = new Uri("http://razr02:80");
            var r3 = new Uri("http://razr03:80");
            var r4 = new Uri("http://razr02.domain.whatever:80");

            blacklist = new[] {r4};

            var applicationInfo = new ApplicationInfo(environment, application, null);

            topology = ServiceTopology.Build(
                new[] { r1, r2, r3, r4 },
                applicationInfo.Properties.SetBlacklist(blacklist));

            provider.GetCluster().Should().Equal(r1, r3);
        }

        [TestCase(null, "http://default_topology-replica1:80")]
        [TestCase("topology1", "http://topology1-replica1:80")]
        [TestCase("topology2", "http://topology2-replica1:80")]
        public void Should_take_target_environment_from_flowing_context(string forcedEnvironment, string expectedReplica)
        {
            FlowingContext.Properties.Clear();
            FlowingContext.Properties.Set(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment, forcedEnvironment);
            var provider = new ServiceDiscoveryClusterProvider(GetMultipleEnvironmentLocator(), application, log);

            var actual = provider.GetCluster();

            actual.Should().Equal(new Uri(expectedReplica));
        }

        [Test]
        public void Should_take_target_environment_from_flowing_context_on_each_request()
        {
            FlowingContext.Properties.Clear();
            var provider = new ServiceDiscoveryClusterProvider(GetMultipleEnvironmentLocator(), application, log);

            FlowingContext.Properties.Set(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment, "topology1");
            var actualCluster1 = provider.GetCluster();
            FlowingContext.Properties.Set(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment, "topology2");
            var actualCluster2 = provider.GetCluster();

            actualCluster1.Should().Equal(new Uri("http://topology1-replica1:80"));
            actualCluster2.Should().Equal(new Uri("http://topology2-replica1:80"));
        }

        private IServiceLocator GetMultipleEnvironmentLocator()
        {
            var topology1 = ServiceTopology.Build(new[] {new Uri("http://topology1-replica1:80")}, null);
            var topology2 = ServiceTopology.Build(new[] {new Uri("http://topology2-replica1:80")}, null);
            var defaultTopology = ServiceTopology.Build(new[] {new Uri("http://default_topology-replica1:80")}, null);

            var locator = Substitute.For<IServiceLocator>();
            locator.Locate("topology1", application).Returns(_ => topology1);
            locator.Locate("topology2", application).Returns(_ => topology2);
            locator.Locate("default", application).Returns(_ => defaultTopology);
            return locator;
        }
    }
}