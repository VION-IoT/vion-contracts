namespace Vion.Contracts.Events.MeshToCloud
{
    /// <summary>
    ///     Asks for the network state of this gateway's peers.
    /// </summary>
    [Schema("GetPeersNetworkStatePayload")]
    public record GetPeersNetworkStatePayload : IMessage;
}
