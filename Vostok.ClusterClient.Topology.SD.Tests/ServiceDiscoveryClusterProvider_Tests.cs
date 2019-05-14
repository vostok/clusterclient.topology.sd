using System;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD.Tests
{
    [TestFixture]
    internal class ServiceDiscoveryClusterProvider_Tests
    {
        private IServiceLocator serviceLocator;
        private ServiceDiscoveryClusterProvider provider;
        private readonly ILog log = new SynchronousConsoleLog();
        private string environment;
        private string application;
        private IServiceTopology topology;
        private IServiceTopologyProperties properties;
        private Uri[] blacklist;

        private readonly Uri replica1 = new Uri("http://replica1:123/v1/");
        private readonly Uri replica2 = new Uri("http://replica2:456/v1/");

        [SetUp]
        public void SetUp()
        {
            environment = "environment";
            application = "application";
            
            serviceLocator = Substitute.For<IServiceLocator>();
            serviceLocator.Locate(environment, application).Returns(_ => topology);

            provider = new ServiceDiscoveryClusterProvider(serviceLocator, environment, application, log);

            blacklist = null;

            properties = Substitute.For<IServiceTopologyProperties>();
            properties.TryGetValue(IServiceTopologyPropertiesExtensions.BlacklistProperty, out var value)
                .Returns(
                    x =>
                    {
                        if (blacklist == null)
                            return false;
                        x[1] = string.Join(IServiceTopologyPropertiesExtensions.BlacklistItemSeparator, blacklist.Select(xx => xx.ToString()));
                        return true;
                    });
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
            topology = Substitute.For<IServiceTopology>();
            topology.Replicas.Returns(new Uri[0]);
            topology.Properties.Returns(properties);

            provider.GetCluster().Should().BeEmpty();
        }

        [Test]
        public void Should_return_replicas()
        {
            topology = Substitute.For<IServiceTopology>();
            topology.Replicas.Returns(new[] {replica1, replica2});
            topology.Properties.Returns(properties);

            provider.GetCluster().Should().BeEquivalentTo(new[] { replica1, replica2 }.Cast<object>());
        }

        [Test]
        public void Should_return_new_replicas_from_new_topology()
        {
            topology = Substitute.For<IServiceTopology>();
            topology.Properties.Returns(properties);
            topology.Replicas.Returns(new[] { replica1, replica2 });
            provider.GetCluster().Should().BeEquivalentTo(new[] { replica1, replica2 }.Cast<object>());

            topology = Substitute.For<IServiceTopology>();
            topology.Properties.Returns(properties);
            topology.Replicas.Returns(new[] { replica2 });
            provider.GetCluster().Should().BeEquivalentTo(new[] { replica2 }.Cast<object>());
        }

        [Test]
        public void Should_filter_blacklisted_replicas()
        {
            topology = Substitute.For<IServiceTopology>();
            topology.Properties.Returns(properties);
            topology.Replicas.Returns(new[] { replica1, replica2 });

            blacklist = new[] { replica2 };

            provider.GetCluster().Should().BeEquivalentTo(new[] { replica1 }.Cast<object>());
        }

        [Test]
        public void Should_filter_all_blacklisted_replicas()
        {
            topology = Substitute.For<IServiceTopology>();
            topology.Properties.Returns(properties);
            topology.Replicas.Returns(new[] { replica1, replica2 });

            blacklist = new[] { replica2, replica1 };

            provider.GetCluster().Should().BeEmpty();
        }
    }
}