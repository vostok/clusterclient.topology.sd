using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Logging.Console;

namespace Vostok.Clusterclient.Topology.SD.Tests
{
    [TestFixture]
    internal class DynamicEnvironmentClusterClient_Tests
    {
        private static readonly Request Request = Request.Get($"/{nameof(DynamicEnvironmentClusterClient_Tests)}/");

        [Test]
        public async Task DynamicEnvironmentClusterClient_should_create_new_cluster_client_for_each_environment()
        {
            var actualCallCount = 0;
            var expectedCallCount = 5;
            var receivedEnvironments = new List<string>();

            var client = new DynamicEnvironmentClusterClient(
                new SynchronousConsoleLog(),
                () => actualCallCount++.ToString(),
                GetClusterClientSetupProvider(env => receivedEnvironments.Add(env)));

            for (var i = 0; i < expectedCallCount; i++)
                await client.SendAsync(Request);

            actualCallCount.Should().Be(expectedCallCount);
            receivedEnvironments.Should().BeEquivalentTo(Enumerable.Range(0, expectedCallCount).Select(i => i.ToString()));
        }

        [Test]
        public async Task DynamicEnvironmentClusterClient_should_reuse_existing_cluster_client_for_each_environment()
        {
            var environmentStorage = new EnvironmentStorage("e1", "e2");
            var receivedEnvironments = new List<string>();

            var client = new DynamicEnvironmentClusterClient(
                new SynchronousConsoleLog(),
                () => environmentStorage.Next(),
                GetClusterClientSetupProvider(env => receivedEnvironments.Add(env)));

            for (var i = 0; i < 5; i++)
                await client.SendAsync(Request);

            receivedEnvironments.Should().BeEquivalentTo(environmentStorage.Environments);
        }

        private Func<string, ClusterClientSetup> GetClusterClientSetupProvider(Action<string> callback)
        {
            var transport = Substitute.For<ITransport>();
            transport.SendAsync(default, default, default, default).ReturnsForAnyArgs(new Response(ResponseCode.Ok));

            return environment =>
            {
                callback(environment);
                return configuration =>
                {
                    configuration.Transport = transport;
                    configuration.ClusterProvider = new FixedClusterProvider("http://test/");
                };
            };
        }

        private class EnvironmentStorage
        {
            public string[] Environments { get; }
            private int index;

            public EnvironmentStorage(params string[] environments)
            {
                Environments = environments;
            }

            public string Next()
            {
                index++;
                index %= Environments.Length;
                return Environments[index];
            }
        }
    }
}