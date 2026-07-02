using System;

namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     Starts or stops a remote-access browser-console session on the gateway (the
    ///     <c>vion-remote-console@{SessionId}</c> unit, which also activates host sshd bound to the tunnel interface).
    ///     Emitted Cloud -&gt; Mesh on <see cref="Vion.Contracts.Mqtt.Topics.RemoteConsoleCommand" />; its sole
    ///     authorization gate is the tenant <c>RemoteConsoleAccess</c> permission at the emitting controller. A
    ///     dedicated system-control command, not a service-provider property. See the architecture spec
    ///     <c>2026-06-30-on-demand-remote-gateway-access</c>.
    /// </summary>
    [Schema("RemoteConsoleCommandPayload")]
    public record RemoteConsoleCommandPayload(Guid SessionId, RemoteAccessAction Action, RemoteAccessSessionParameters? Parameters) : IMessage;
}
