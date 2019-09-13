using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
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
    }
}