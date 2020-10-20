using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Context;
using Vostok.Logging.Console;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Abstractions.Models;

namespace Vostok.Clusterclient.Topology.SD.Tests
{
    [TestFixture]
    internal class ForcedSdEnvironmentClusterClient_Tests
    {
        private const string DefaultEnvironment = "default";
        private static readonly Request Request = Request.Get($"/{nameof(ForcedSdEnvironmentClusterClient_Tests)}/");

        private static readonly string[] Environments = {DefaultEnvironment, "env1", "env2"};

        [TestCaseSource(nameof(Environments))]
        public async Task use_replicas_from_environment_specified_in_forced_sd_environment_distributed_property(string environment)
        {
            FlowingContext.Properties.Set(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment, environment);

            var transport = GetTransport();
            var client = GetForcedSdEnvironmentClusterClient(transport);

            await client.SendAsync(Request);

            EnsureTransportGotCallToEnvironment(transport, environment);
        }

        [Test]
        public async Task fallback_to_default_environment_when_distributed_property_is_not_specified()
        {
            FlowingContext.Properties.Clear();
            FlowingContext.Properties.Current
                .ContainsKey(ServiceDiscoveryConstants.DistributedProperties.ForcedEnvironment)
                .Should()
                .BeFalse("Test initialization went wrong: forced.sd.environment property is still set.");

            var transport = GetTransport();
            var client = GetForcedSdEnvironmentClusterClient(transport);

            await client.SendAsync(Request);

            EnsureTransportGotCallToEnvironment(transport, DefaultEnvironment);
        }

        private static void EnsureTransportGotCallToEnvironment(ITransport transport, string environment)
        {
            var calls = transport.ReceivedCalls().ToArray();
            calls.Should().HaveCount(1);
            calls.First().GetArguments().First().Should().Match<Request>(request => request.Url.Host.StartsWith(environment));
        }

        private static ITransport GetTransport()
        {
            var transport = Substitute.For<ITransport>();
            transport.SendAsync(default, default, default, default).ReturnsForAnyArgs(new Response(ResponseCode.Ok));
            return transport;
        }

        private static IClusterClient GetForcedSdEnvironmentClusterClient(ITransport transport)
        {
            const string application = "app_name";

            var serviceLocator = Substitute.For<IServiceLocator>();
            foreach (var env in Environments)
            {
                serviceLocator
                    .Locate(env, application)
                    .Returns(
                        _ => ServiceTopology.Build(
                            new List<Uri>
                            {
                                new Uri($"http://{env}-replica1:123/v1/"),
                                new Uri($"http://{env}-replica2:123/v1/"),
                            },
                            null));
            }

            return new ForcedSdEnvironmentClusterClient(
                application,
                serviceLocator,
                new SynchronousConsoleLog(),
                DefaultEnvironment,
                configuration =>
                {
                    configuration.Transport = transport;
                }
            );
        }
    }
}