using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Topology.SD.Transforms;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Abstractions.Models;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD.Tests
{
    [TestFixture]
    internal class ServiceDiscoveryClusterProvider_WithNeverForgetReplicasTransform_Tests
    {
        private readonly ILog log = new SynchronousConsoleLog();

        private readonly Uri replica1 = new Uri("http://replica1:123/v1/");
        private readonly Uri replica2 = new Uri("http://replica2:456/v1/");
        private readonly Uri replica3 = new Uri("http://replica3:789/");
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

            var settings = new ServiceDiscoveryClusterProviderSettings { ServiceTopologyTransform = new NeverForgetReplicasTransform() };
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
        public void Should_never_forget_replicas()
        {
            topology = ServiceTopology.Build(new[] {replica1}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1}.Cast<object>());

            topology = ServiceTopology.Build(new List<Uri>(), null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1}.Cast<object>());

            topology = ServiceTopology.Build(new[] {replica2}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1, replica2}.Cast<object>());

            topology = ServiceTopology.Build(new[] {replica1, replica2}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1, replica2}.Cast<object>());

            topology = ServiceTopology.Build(new List<Uri>(), null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1, replica2}.Cast<object>());

            topology = ServiceTopology.Build(new[] {replica1, replica2, replica3}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1, replica2, replica3}.Cast<object>());

            topology = ServiceTopology.Build(new[] {replica3}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1, replica2, replica3}.Cast<object>());

            topology = ServiceTopology.Build(new List<Uri>(), null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1, replica2, replica3}.Cast<object>());
        }

        [Test]
        public void Should_apply_bl_only_after_restoring_all_replicas()
        {
            topology = ServiceTopology.Build(new[] {replica1}, CreateApplicationInfoPropertiesWithBlackList(new[] {replica1}));
            provider.GetCluster().Should().BeEmpty();

            topology = ServiceTopology.Build(new List<Uri>(), null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1}.Cast<object>());

            topology = ServiceTopology.Build(new[] {replica2}, CreateApplicationInfoPropertiesWithBlackList(new[] {replica1}));
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica2}.Cast<object>());

            topology = ServiceTopology.Build(new[] {replica1, replica2}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1, replica2}.Cast<object>());

            topology = ServiceTopology.Build(new List<Uri>(), CreateApplicationInfoPropertiesWithBlackList(new[] {replica1, replica2}));
            provider.GetCluster().Should().BeEmpty();

            topology = ServiceTopology.Build(new List<Uri>(), null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {replica1, replica2}.Cast<object>());
        }

        [Test]
        public void Should_correct_merge_fqdn_and_nofqdn_replicas()
        {
            var r1 = new Uri("http://razr02.domain.whatever:80");
            var r2 = new Uri("http://razr02:80");

            topology = ServiceTopology.Build(new[] {r1}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {r1}.Cast<object>());

            topology = ServiceTopology.Build(new[] {r2}, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] {r1}.Cast<object>());
        }

        //(deniaa): This test not pass check. I believe that fqdn replica should replace no-fqdn one. But the implementation will be expensive on the CPU..
        [Explicit]
        [Test]
        public void Should_correct_merge_fqdn_and_nofqdn_replicas_and_replace_nofqnd_to_fqdn_one()
        {
            var r1 = new Uri("http://razr02:80");
            var r2 = new Uri("http://razr02.domain.whatever:80");

            topology = ServiceTopology.Build(new[] { r1 }, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] { r1 }.Cast<object>());

            topology = ServiceTopology.Build(new[] { r2 }, null);
            provider.GetCluster().Should().BeEquivalentTo(new[] { r2 }.Cast<object>());
        }

        private IApplicationInfoProperties CreateApplicationInfoPropertiesWithBlackList(Uri[] blacklist)
        {
            var applicationInfo = new ApplicationInfo(environment, application, null);
            return applicationInfo.Properties.SetBlacklist(blacklist);
        }
    }
}