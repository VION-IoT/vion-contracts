using System;
using System.Collections.Generic;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     This gateway's peers and where each one can be reached. Every message carries the whole list.
    /// </summary>
    [Schema("PeersNetworkStatePayload")]
    public record PeersNetworkStatePayload(List<PeerNetworkState> Peers) : IMessage;

    /// <summary>
    ///     A peer gateway and where its broker can be reached.
    /// </summary>
    /// <param name="GatewayId">The peer gateway's id.</param>
    /// <param name="InstallationTopic">The peer gateway's installation topic.</param>
    /// <param name="Addresses">
    ///     Host names or IP literals the peer's broker can be dialled on, in order of preference, any address family;
    ///     empty when there are none.
    /// </param>
    /// <param name="Port">The port of the peer broker's mTLS listener.</param>
    public readonly record struct PeerNetworkState(Guid GatewayId, string InstallationTopic, List<string> Addresses, int Port);
}
