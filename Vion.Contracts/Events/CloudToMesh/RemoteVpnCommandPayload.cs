using System;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     Starts or stops a remote-access VPN session on the gateway (the <c>vion-remote-vpn@{SessionId}</c> unit).
    ///     Emitted Cloud -&gt; Mesh on <see cref="Vion.Contracts.Mqtt.Topics.RemoteVpnCommand" />; its sole
    ///     authorization gate is the tenant <c>RemoteVpnAccess</c> permission at the emitting controller. A dedicated
    ///     system-control command (beside restart / logLevel), not a service-provider property — so it is invisible on
    ///     the service surface and ungated by the broad property-set permission. See the architecture spec
    ///     <c>2026-06-30-on-demand-remote-gateway-access</c>.
    /// </summary>
    [Schema("RemoteVpnCommandPayload")]
    public record RemoteVpnCommandPayload(Guid SessionId, RemoteAccessAction Action, RemoteAccessSessionParameters? Parameters) : IMessage;
}
