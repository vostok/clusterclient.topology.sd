using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Vostok.Commons.Helpers.Topology;
using Vostok.ServiceDiscovery.Abstractions;
using Vostok.ServiceDiscovery.Extensions;

namespace Vostok.Clusterclient.Topology.SD.Transforms;

/// <summary>
/// A transform that returns the union of resolved replicas and replicas specified by the app's hosting system in a dedicated property
/// when it detects that resolved topology is considerably smaller than it should be (see <see cref="AcceptableAliveBeaconsRatio"/>).
/// </summary>
[PublicAPI]
public class AugmentWithHostingTopologyTransform : IServiceTopologyTransform
{
    /// <summary>
    /// Resolved topology that is at least as large as this fraction of the corresponding hosting topology won't be transformed.
    /// </summary>
    public double AcceptableAliveBeaconsRatio { get; set; } = 2 / 3d;

    /// <summary>
    /// Resolved topology's fraction of replicas that are also in hosting topology must be no less than this value in order for transformation to occur.
    /// </summary>
    public double MinCommonReplicasRatio { get; set; } = 2 / 3d;

    public IEnumerable<Uri> Transform(IServiceTopology topology)
    {
        var hostingTopology = GetHostingTopology(topology);

        if (!ShouldMerge(topology.Replicas, hostingTopology))
            return topology.Replicas;

        // (iloktionov, 16.05.2022): Preserve exact addresses from beacons when there are differences (such as URL path or domain name) ignored by ReplicaComparer:
        foreach (var replica in topology.Replicas)
            hostingTopology[replica] = replica;

        return hostingTopology.Values;
    }

    private bool ShouldMerge(IReadOnlyList<Uri> beacons, Dictionary<Uri, Uri> hostingTopology)
    {
        if (hostingTopology.Count == 0)
            return false;

        if (beacons.Count == 0)
            return true;

        if (beacons.Count >= hostingTopology.Count * AcceptableAliveBeaconsRatio)
            return false;

        var commonReplicasCount = beacons.Count(hostingTopology.ContainsKey);
        var commonReplicasRatio = 1d * commonReplicasCount / beacons.Count;
        if (commonReplicasRatio < MinCommonReplicasRatio)
            return false;

        return true;
    }

    private static Dictionary<Uri, Uri> GetHostingTopology(IServiceTopology topology)
    {
        var replicas = topology.Properties.GetHostingTopology();

        var result = new Dictionary<Uri, Uri>(replicas.Length, ReplicaComparer.Instance);

        foreach (var replica in replicas)
            result[replica] = replica;

        return result;
    }
}