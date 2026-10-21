namespace Vion.Contracts.Events.MeshToCloud
{
    /// <summary>
    ///     Asks for the network state of this gateway's peers; answered with a
    ///     <see cref="Vion.Contracts.Events.CloudToMesh.PeersNetworkStatePayload" />.
    /// </summary>
    [Schema("GetPeersNetworkStatePayload")]
    public record GetPeersNetworkStatePayload : IMessage;
}
