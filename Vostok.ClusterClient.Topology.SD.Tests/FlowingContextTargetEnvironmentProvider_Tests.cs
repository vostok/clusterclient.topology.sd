using FluentAssertions;
using NUnit.Framework;
using Vostok.Context;

namespace Vostok.Clusterclient.Topology.SD.Tests
{
    [TestFixture]
    internal class FlowingContextTargetEnvironmentProvider_Tests
    {
        [Test]
        public void Should_retrieve_environment_value_from_flowing_context()
        {
            var provider = new FlowingContextTargetEnvironmentProvider();
            FlowingContext.Properties.Clear();

            FlowingContext.Properties.Set(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment, "environment1");
            var actual1 = provider.Find();
            FlowingContext.Properties.Set(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment, "environment2");
            var actual2 = provider.Find();

            actual1.Should().Be("environment1");
            actual2.Should().Be("environment2");
        }

        [Test]
        public void Should_return_null_if_flowing_context_has_no_forced_environment_property()
        {
            var provider = new FlowingContextTargetEnvironmentProvider();
            FlowingContext.Properties.Clear();

            var actual = provider.Find();
            actual.Should().BeNull();
        }

    }
}