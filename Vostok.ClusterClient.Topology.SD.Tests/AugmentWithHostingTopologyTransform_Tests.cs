using System;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Topology.SD.Transforms;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Abstractions.Models;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD.Tests;

[TestFixture]
internal class AugmentWithHostingTopologyTransform_Tests
{
    private readonly ILog log = new SynchronousConsoleLog();

    private readonly Uri replica1 = new Uri("http://replica1:123/v1/");
    private readonly Uri replica2 = new Uri("http://replica2:456/v1/");
    private readonly Uri replica3 = new Uri("http://replica3:789/");
    private readonly Uri replica4 = new Uri("http://replica4.dev.kontur.ru:111/");
    private readonly Uri replica5 = new Uri("http://replica5.dev.kontur.ru:222/");
    private readonly Uri replica6 = new Uri("http://replica6.dev.kontur.ru:333/");

    private IServiceLocator serviceLocator;
    private ServiceDiscoveryClusterProvider provider;
    private string environment;
    private string application;

    private Uri[] beacons;
    private Uri[] hosting;

    [SetUp]
    public void SetUp()
    {
        environment = "environment";
        application = "application";

        beacons = null;
        hosting = null;

        serviceLocator = Substitute.For<IServiceLocator>();
        serviceLocator.Locate(environment, application).Returns(_ => BuildTopology());

        var settings = new ServiceDiscoveryClusterProviderSettings
        {
            ServiceTopologyTransform = new AugmentWithHostingTopologyTransform()
        };

        provider = new ServiceDiscoveryClusterProvider(serviceLocator, environment, application, settings, log);
    }

    [Test]
    public void Should_return_beacons_when_there_is_no_hosting_topology()
    {
        SetupBeacons(replica1, replica2);

        provider.GetCluster().Should().Equal(replica1, replica2);
    }

    [Test]
    public void Should_return_empty_when_there_is_no_hosting_topology()
    {
        SetupBeacons();

        provider.GetCluster().Should().BeEmpty();
    }

    [Test]
    public void Should_return_beacons_when_topologies_match()
    {
        SetupBeacons(replica1, replica2, replica3);

        SetupHosting(replica3, replica2, replica1);

        provider.GetCluster().Should().Equal(replica1, replica2, replica3);
    }
    
    [Test]
    public void Should_return_beacons_when_there_is_still_enough_of_them()
    {
        SetupBeacons(replica1, replica2);

        SetupHosting(replica3, replica2, replica1);

        provider.GetCluster().Should().Equal(replica1, replica2);
    }

    [Test]
    public void Should_return_beacons_when_there_are_not_enough_of_them_but_topologies_diverge()
    {
        SetupBeacons(replica1);

        SetupHosting(replica5, replica6, replica4);

        provider.GetCluster().Should().Equal(replica1);
    }

    [Test]
    public void Should_merge_when_there_are_no_beacons()
    {
        SetupBeacons();

        SetupHosting(replica1, replica2, replica3);

        provider.GetCluster().Should().BeEquivalentTo(replica1, replica2, replica3);
    }

    [Test]
    public void Should_merge_when_there_are_not_enough_beacons()
    {
        SetupBeacons(replica2);

        SetupHosting(replica3, replica2, replica1);

        provider.GetCluster().Should().BeEquivalentTo(replica1, replica2, replica3);
    }

    [Test]
    public void Should_prefer_beacon_url_when_merging_common_replicas()
    {
        SetupBeacons(replica2);

        SetupHosting(replica3, ScrambleUri(replica2), replica1);

        provider.GetCluster().Should().BeEquivalentTo(replica1, replica2, replica3);
    }

    private void SetupBeacons(params Uri[] replicas)
        => beacons = replicas;

    private void SetupHosting(params Uri[] replicas)
        => hosting = replicas;

    private IServiceTopology BuildTopology()
    {
        if (hosting == null)
            return ServiceTopology.Build(beacons, null);

        var properties = new ApplicationInfo(environment, application, null).Properties.SetHostingTopology(hosting);

        return ServiceTopology.Build(beacons, properties);
    }

    private Uri ScrambleUri(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Path = string.Empty
        };

        builder.Host += ".domain";

        return builder.Uri;
    }
}