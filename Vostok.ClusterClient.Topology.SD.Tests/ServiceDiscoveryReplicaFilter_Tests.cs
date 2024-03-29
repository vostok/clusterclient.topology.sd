using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Modules;
using Vostok.Clusterclient.Core.Ordering;
using Vostok.Clusterclient.Core.Topology;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Clusterclient.Topology.SD.Helpers;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Abstractions.Models;
using Vostok.ServiceDiscovery.Extensions;
using Vostok.ServiceDiscovery.Extensions.TagFilters;

namespace Vostok.Clusterclient.Topology.SD.Tests
{
    [TestFixture]
    internal class ServiceDiscoveryReplicaFilter_Tests
    {
        private readonly ILog log = new SynchronousConsoleLog();

        private readonly Uri replica1 = new Uri("http://replica1:123/v1/");
        private readonly Uri replica2 = new Uri("http://replica2:456/v2/");
        private readonly Uri replica3 = new Uri("http://replica3:789/v3/");
        private List<Uri> replicas;
        private IServiceLocator serviceLocator;
        private ServiceDiscoveryReplicasFilter filter;
        private string environment;
        private string application;
        private IServiceTopology topology;

        private IRequestContext context;

        [SetUp]
        public void SetUp()
        {
            environment = "environment";
            application = "application";
            replicas = new List<Uri>{replica1, replica2};
            topology = ServiceTopology.Build(replicas, null);

            serviceLocator = Substitute.For<IServiceLocator>();
            serviceLocator.Locate(environment, application).Returns(_ => topology);

            filter = new ServiceDiscoveryReplicasFilter(serviceLocator, environment, application, log);
            
            context = new FakeContext(new RequestParameters().SetTagsFilter(collection => collection.ContainsKey("tag1")));
        }

        [Test]
        public void Should_return_given_replicas_for_empty_filter()
        {
            context.Parameters = context.Parameters.WithProperty(RequestParametersExtensions.RequestParametersTagsFilterKey, null);
            filter.Filter(replicas, context).Should().BeEquivalentTo(replicas);
        }

        [Test]
        public void Should_return_given_replicas_for_null_topology()
        {
            topology = null;
            filter.Filter(replicas, context).Should().BeEquivalentTo(replicas);
        }

        [Test]
        public void Should_return_empty_list_for_empty_given_replicas()
        {
            filter.Filter(new List<Uri>(), context).Should().BeEmpty();
        }

        [Test]
        public void Should_return_empty_replicas_by_key_exists_condition_when_no_tags()
        {
            filter.Filter(replicas, context).Should().BeEmpty();
        }

        [Test]
        public void Should_return_replicas_by_key_not_exists_condition_when_no_tags()
        {
            context.Parameters = context.Parameters.SetTagsFilter(collection => !collection.ContainsKey("tag1"));
            filter.Filter(replicas, context).Should().BeEquivalentTo(replicas);
        }

        [Test]
        public void Should_filter_some_replicas_when_they_has_no_tags()
        {
            var applicationInfo = new ApplicationInfo(environment, application, null);
            topology = ServiceTopology.Build(replicas, applicationInfo.Properties.SetPersistentReplicaTags(replica1.ToString(), new TagCollection{"tag1"}));
            filter.Filter(replicas, context).Should().BeEquivalentTo(replica1);
        }

        [Test]
        public void Should_filter_some_replicas_when_they_has_no_tags_and_filter_was_set_in_constructor()
        {
            var applicationInfo = new ApplicationInfo(environment, application, null);
            topology = ServiceTopology.Build(replicas, applicationInfo.Properties.SetPersistentReplicaTags(replica1.ToString(), new TagCollection{"tag1"}));
            var filter1 = new ServiceDiscoveryReplicasFilter(serviceLocator, environment, application, log, collection => collection.ContainsKey("tag1"));
            var newContext = new FakeContext(new RequestParameters());
            
            filter1.Filter(replicas, newContext).Should().BeEquivalentTo(replica1);
            
            var filter2 = new ServiceDiscoveryReplicasFilter(serviceLocator, environment, application, log, new ContainsTagFilter("tag1"));
            filter2.Filter(replicas, newContext).Should().BeEquivalentTo(replica1);
            
            var filter3 = new ServiceDiscoveryReplicasFilter(serviceLocator, environment, application, log, TagFilterExpressionHelpers.Parse("tag1"));
            filter3.Filter(replicas, newContext).Should().BeEquivalentTo(replica1);
        }

        [Test]
        public void Should_override_replicaMatchesFunc_from_constructor_when_defined_in_request()
        {
            var applicationInfo = new ApplicationInfo(environment, application, null);
            topology = ServiceTopology.Build(replicas, applicationInfo.Properties.SetPersistentReplicaTags(replica1.ToString(), new TagCollection{"tag1"}));
            var filter1 = new ServiceDiscoveryReplicasFilter(serviceLocator, environment, application, log, collection => collection.ContainsKey("tag1"));
            var context1 = new FakeContext(new RequestParameters());
            
            filter1.Filter(replicas, context1).Should().BeEquivalentTo(replica1);
            
            var context2 = new FakeContext(new RequestParameters().SetTagsFilter(collection => collection.ContainsKey("tag2")));
            filter1.Filter(replicas, context2).Should().BeEmpty();
        }

        [Test]
        public void Should_filter_new_replicas_from_new_topology()
        {
            var applicationInfo = new ApplicationInfo(environment, application, null);
            topology = ServiceTopology.Build(replicas, applicationInfo.Properties.SetPersistentReplicaTags(replica1.ToString(), new TagCollection{"tag1"}));
            filter.Filter(replicas, context).Should().BeEquivalentTo(replica1);
            
            topology = ServiceTopology.Build(
                replicas, 
                applicationInfo.Properties
                    .SetPersistentReplicaTags(replica1.ToString(), new TagCollection{"tag2"})
                    .SetPersistentReplicaTags(replica2.ToString(), new TagCollection{"tag1"}));
            filter.Filter(replicas, context).Should().BeEquivalentTo(replica2);
        }

        [Test]
        public void Should_filter_replicas_with_FQDN()
        {
            var replica1Fqdn = new Uri("http://replica1.domain.my:123/v1/");
            var replica2Fqdn = new Uri("http://replica2.domain.my:456/v2/");
            
            var applicationInfo = new ApplicationInfo(environment, application, null);
            topology = ServiceTopology.Build(
                new []{replica1, replica2Fqdn, replica3}, 
                applicationInfo.Properties
                    .SetPersistentReplicaTags(replica1.ToString(), new TagCollection{"tag1"})
                    .SetPersistentReplicaTags(replica2Fqdn.ToString(), new TagCollection{"tag1"}));
            filter.Filter(new List<Uri>{replica1Fqdn, replica2, replica3}, context).Should().BeEquivalentTo(replica1Fqdn, replica2);
        }

        [Test]
        public void Should_ignore_duplicate_replica_name_in_service_tags()
        {
            var replicaNoFqdn = new Uri("http://replica1:123/v1/");
            var replicaFqdn = new Uri("http://replica1.domain.my:123/v1/");
            
            var applicationInfo = new ApplicationInfo(environment, application, null);
            topology = ServiceTopology.Build(
                new []{replicaFqdn},
                applicationInfo.Properties
                    .SetPersistentReplicaTags(replicaNoFqdn.ToString(), new TagCollection{"tag2"})
                    .SetPersistentReplicaTags(replicaFqdn.ToString(), new TagCollection{"tag1"}));
            
            filter.Filter(new List<Uri>{replicaFqdn}, context).Should().BeEquivalentTo(replicaFqdn);
            filter.Filter(new List<Uri>{replicaNoFqdn}, context).Should().BeEquivalentTo(replicaNoFqdn);
        }

        [Test]
        public void Should_filter_replicas_that_does_not_exists_in_service_locator_replicas()
        {
            var applicationInfo = new ApplicationInfo(environment, application, null);
            topology = ServiceTopology.Build(
                new []{replica1}, 
                applicationInfo.Properties
                    .SetPersistentReplicaTags(replica1.ToString(), new TagCollection{"tag1"})
                    .SetPersistentReplicaTags(replica2.ToString(), new TagCollection{"tag1"})
                    .SetPersistentReplicaTags(replica3.ToString(), new TagCollection{"tag1"}));
            
            filter.Filter(new List<Uri>{replica1, replica2, replica3}, context).Should().BeEquivalentTo(replica1);
        }

        [Test]
        public void Should_filter_replicas_that_does_not_exists_in_given_replicas()
        {
            var applicationInfo = new ApplicationInfo(environment, application, null);
            topology = ServiceTopology.Build(
                new []{replica1, replica2, replica3}, 
                applicationInfo.Properties
                    .SetPersistentReplicaTags(replica1.ToString(), new TagCollection{"tag1"})
                    .SetPersistentReplicaTags(replica2.ToString(), new TagCollection{"tag1"})
                    .SetPersistentReplicaTags(replica3.ToString(), new TagCollection{"tag1"}));
            
            filter.Filter(new List<Uri>{replica1, replica2}, context).Should().BeEquivalentTo(replica1, replica2);
        }

        [Test]
        public void Should_throw_exception_then_filter_function_throws_exception()
        {
            context.Parameters = context.Parameters.SetTagsFilter(collection => collection["tag"] == "smth");
            filter
                .Invoking(c => c.Filter(replicas, context).ToArray())
                .Should()
                .Throw<KeyNotFoundException>();
        }
        
        private class FakeContext : IRequestContext
        {
            // ReSharper disable once NotNullMemberIsNotInitialized
            public FakeContext(RequestParameters requestParameters)
            {
                Parameters = requestParameters;
            }

            public void ResetReplicaResults()
            {
                throw new NotImplementedException();
            }

            public Request Request { get; set; }
            public RequestParameters Parameters { get; set; }
            public IRequestTimeBudget Budget { get; }
            public ILog Log { get; }
            public IClusterProvider ClusterProvider { get; set; }
            public IAsyncClusterProvider AsyncClusterProvider { get; set; }
            public IReplicaOrdering ReplicaOrdering { get; set; }
            public ITransport Transport { get; set; }
            public CancellationToken CancellationToken { get; }
            public int MaximumReplicasToUse { get; set; }
            public int ConnectionAttempts { get; set; }
            public string ClientApplicationName { get; }
        }
    }
}