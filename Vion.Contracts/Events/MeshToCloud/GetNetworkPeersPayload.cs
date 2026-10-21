namespace Vion.Contracts.Events.MeshToCloud
{
    /// <summary>
    ///     Asks for this gateway's peers; answered with a
    ///     <see cref="Vion.Contracts.Events.CloudToMesh.NetworkPeersStatePayload" />.
    /// </summary>
    [Schema("GetNetworkPeersPayload")]
    public record GetNetworkPeersPayload : IMessage;
}
