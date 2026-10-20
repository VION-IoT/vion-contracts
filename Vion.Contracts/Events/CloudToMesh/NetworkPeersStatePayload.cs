using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     The gateways this gateway may bridge with. The reply to
    ///     <see cref="Vion.Contracts.Events.MeshToCloud.GetNetworkPeersPayload" /> and every later change carry the
    ///     whole list.
    /// </summary>
    [Schema("NetworkPeersStatePayload")]
    public record NetworkPeersStatePayload(List<NetworkPeer> Peers) : IMessage;

    /// <summary>
    ///     A gateway this gateway may bridge with, and where its broker can be reached.
    /// </summary>
    /// <param name="GatewayId">The peer gateway's id.</param>
    /// <param name="InstallationTopic">The peer gateway's installation topic.</param>
    /// <param name="Addresses">
    ///     Host names or IP literals the peer's broker can be dialled on from the local network, in order of
    ///     preference, any address family; empty when there are none.
    /// </param>
    /// <param name="Port">The port of the peer broker's mTLS listener.</param>
    public readonly record struct NetworkPeer(Guid GatewayId, string InstallationTopic, List<string> Addresses, int Port);
}
