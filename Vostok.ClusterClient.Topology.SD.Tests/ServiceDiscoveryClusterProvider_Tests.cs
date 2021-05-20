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
    internal class ServiceDiscoveryClusterProvider_DesiredTopology_Tests
    {
        private readonly ILog log = new SynchronousConsoleLog();

        private readonly Uri replica1 = new Uri("http://replica1:123/v1/");
        private readonly Uri replica2 = new Uri("http://replica2:456/v1/");
        private IServiceLocator serviceLocator;
        private ServiceDiscoveryClusterProvider provider;
        private string environment;
        private string application;
        private IServiceTopology topology;
        private Uri[] desiredTopology;
        private DesiredTopologySettings desiredTopologySettings;

        [SetUp]
        public void SetUp()
        {
            environment = "environment";
            application = "application";

            serviceLocator = Substitute.For<IServiceLocator>();
            serviceLocator.Locate(environment, application).Returns(_ => topology);
            desiredTopologySettings = new DesiredTopologySettings();

            var settings = new ServiceDiscoveryClusterProviderSettings { DesiredTopologySettings = desiredTopologySettings};
            provider = new ServiceDiscoveryClusterProvider(serviceLocator, environment, application, settings, log);

            desiredTopology = null;
        }

        [Test]
        public void Should_do_nothing_if_desired_topology_equals_actual()
        {
            desiredTopology = new[] { replica1, replica2 };

            var applicationInfo = new ApplicationInfo(environment, application, null);

            topology = ServiceTopology.Build(
                new[] { replica1, replica2 },
                applicationInfo.Properties.SetDesiredTopology(desiredTopology));

            provider.GetCluster().Should().BeEquivalentTo(replica1, replica2);
        }

        [Test]
        public void Should_advance_empty_topology_by_desired_replicas()
        {
            desiredTopology = new[] { replica1, replica2 };

            var applicationInfo = new ApplicationInfo(environment, application, null);

            topology = ServiceTopology.Build(
                new Uri[0],
                applicationInfo.Properties.SetDesiredTopology(desiredTopology));

            provider.GetCluster().Should().BeEquivalentTo(replica1, replica2);
        }

        [Test]
        public void Should_advance_partial_topology_by_desired_replicas()
        {
            var replicas = new Uri[100];
            for (var i = 0; i < replicas.Length; i++)
                replicas[i] = new Uri($"http://replica{i}:123/");

            desiredTopology = replicas;
            desiredTopologySettings.MinDesiredTopologyPresenceAmongAliveToAdvance = 1d / 2;

            var applicationInfo = new ApplicationInfo(environment, application, null);

            topology = ServiceTopology.Build(
                //every third is less than the MinDesiredTopologyPresenceAmongAlive = 1d / 2
                replicas.Where((replica, index) => index % 3 == 0).ToArray(),
                applicationInfo.Properties.SetDesiredTopology(desiredTopology));

            provider.GetCluster().Should().BeEquivalentTo(replicas.Cast<object>());
        }

        [Test]
        public void Should_NOT_advance_partial_topology_by_desired_replicas_if_there_are_enough()
        {
            var replicas = new Uri[100];
            for (var i = 0; i < replicas.Length; i++)
                replicas[i] = new Uri($"http://replica{i}:123/");

            desiredTopology = replicas;
            desiredTopologySettings.MinDesiredTopologyPresenceAmongAliveToAdvance = 1d / 2;

            var applicationInfo = new ApplicationInfo(environment, application, null);

            var aliveReplicas = replicas.Where((replica, index) => index % 3 != 0).ToArray();
            topology = ServiceTopology.Build(
                //every NOT third is greater then the MinDesiredTopologyPresenceAmongAlive = 1d / 2
                aliveReplicas,
                applicationInfo.Properties.SetDesiredTopology(desiredTopology));

            provider.GetCluster().Should().BeEquivalentTo(aliveReplicas.Cast<object>());
        }

        [Test]
        public void Should_advance_partial_topology_by_desired_replicas_if_MaxReplicasCountToAlwaysAdvanceReplicasByDesiredTopology_satisfy()
        {
            var replicas = new Uri[100];
            for (var i = 0; i < replicas.Length; i++)
                replicas[i] = new Uri($"http://replica{i}:123/");

            desiredTopology = replicas;
            desiredTopologySettings.MinDesiredTopologyPresenceAmongAliveToAdvance = 1;
            desiredTopologySettings.MaxReplicasCountToAlwaysAdvanceReplicasByDesiredTopology = 100;

            var applicationInfo = new ApplicationInfo(environment, application, null);

            var aliveReplicas = replicas.Where((replica, index) => index % 3 == 0).ToArray();
            topology = ServiceTopology.Build(
                //every third is less than the MinDesiredTopologyPresenceAmongAlive = 1, but it is also less than MaxReplicasCountToAlwaysAdvanceReplicasByDesiredTopology = 100;
                aliveReplicas,
                applicationInfo.Properties.SetDesiredTopology(desiredTopology));

            provider.GetCluster().Should().BeEquivalentTo(replicas.Cast<object>());
        }
    }

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
    }
}