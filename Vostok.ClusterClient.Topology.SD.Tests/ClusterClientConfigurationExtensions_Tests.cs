﻿using System;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core;
using Vostok.Context;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Abstractions.Models;

namespace Vostok.Clusterclient.Topology.SD.Tests
{
    [TestFixture]
    internal class ClusterClientConfigurationExtensions_Tests
    {
        private const string Application = "application";

        [TestCase(null, "http://default_topology-replica1:80")]
        [TestCase("topology1", "http://topology1-replica1:80")]
        [TestCase("topology2", "http://topology2-replica1:80")]
        public void Should_take_target_environment_from_flowing_context(string forcedEnvironment, string expectedReplica)
        {
            FlowingContext.Properties.Clear();
            FlowingContext.Properties.Set(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment, forcedEnvironment);

            var clusterClientConfig = Substitute.For<IClusterClientConfiguration>();
            clusterClientConfig.SetupServiceDiscoveryTopology(GetMultipleEnvironmentLocator(), Application);

            clusterClientConfig.TargetServiceName.Should().Be(Application);
            clusterClientConfig.ClusterProvider.GetCluster().Should().Equal(new Uri(expectedReplica));
        }

        [Test]
        public void Should_take_target_environment_from_flowing_context_on_each_request()
        {
            FlowingContext.Properties.Clear();
            var clusterClientConfig = Substitute.For<IClusterClientConfiguration>();
            clusterClientConfig.SetupServiceDiscoveryTopology(GetMultipleEnvironmentLocator(), Application);

            FlowingContext.Properties.Set(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment, "topology1");
            var actualCLuster1 = clusterClientConfig.ClusterProvider.GetCluster();
            FlowingContext.Properties.Set(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment, "topology2");
            var actualCLuster2 = clusterClientConfig.ClusterProvider.GetCluster();

            actualCLuster1.Should().Equal(new Uri("http://topology1-replica1:80"));
            actualCLuster2.Should().Equal(new Uri("http://topology2-replica1:80"));
        }

        private IServiceLocator GetMultipleEnvironmentLocator()
        {
            var topology1 = ServiceTopology.Build(new[] {new Uri("http://topology1-replica1:80")}, null);
            var topology2 = ServiceTopology.Build(new[] {new Uri("http://topology2-replica1:80")}, null);
            var defaultTopology = ServiceTopology.Build(new[] {new Uri("http://default_topology-replica1:80")}, null);

            var serviceLocator = Substitute.For<IServiceLocator>();
            serviceLocator.Locate("topology1", Application).Returns(_ => topology1);
            serviceLocator.Locate("topology2", Application).Returns(_ => topology2);
            serviceLocator.Locate("default", Application).Returns(_ => defaultTopology);
            return serviceLocator;
        }
    }
}