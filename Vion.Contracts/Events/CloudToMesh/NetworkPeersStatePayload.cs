using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     This gateway's peers and where each one can be reached. Which gateways are peers is decided by the cloud,
    ///     not by this contract. The reply to <see cref="Vion.Contracts.Events.MeshToCloud.GetNetworkPeersPayload" />
    ///     and every later change carry the whole list.
    /// </summary>
    [Schema("NetworkPeersStatePayload")]
    public record NetworkPeersStatePayload(List<NetworkPeer> Peers) : IMessage;

    /// <summary>
    ///     A peer gateway and where its broker can be reached.
    /// </summary>
    /// <param name="GatewayId">The peer gateway's id.</param>
    /// <param name="InstallationTopic">The peer gateway's installation topic.</param>
    /// <param name="Addresses">
    ///     The addresses the peer last reported in its
    ///     <see cref="Vion.Contracts.Events.MeshToCloud.NetworkStatePayload" />, in the same order; empty when there
    ///     are none.
    /// </param>
    /// <param name="Port">
    ///     The port the peer last reported in its
    ///     <see cref="Vion.Contracts.Events.MeshToCloud.NetworkStatePayload" />.
    /// </param>
    public readonly record struct NetworkPeer(Guid GatewayId, string InstallationTopic, List<string> Addresses, int Port);
}
