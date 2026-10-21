using System.Collections.Generic;

namespace Vion.Contracts.Events.MeshToCloud
{
    /// <summary>
    ///     Where a peer on the local network can reach this gateway's broker.
    /// </summary>
    /// <param name="Addresses">
    ///     Host names or IP literals a peer on the local network can dial this gateway's broker on, in order of
    ///     preference, any address family; empty when there are none.
    /// </param>
    /// <param name="Port">The port of the broker's mTLS listener.</param>
    [Schema("NetworkStatePayload")]
    public record NetworkStatePayload(List<string> Addresses, int Port) : IMessage;
}
