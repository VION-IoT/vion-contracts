namespace Vion.Contracts.Events.CloudToMesh
{
    /// <summary>
    ///     Stops a named base-image system service on the gateway (system-control family). The service is the FINAL
    ///     segment of the topic — compose as <see cref="Vion.Contracts.Mqtt.Topics.SystemServiceStop" /> + "/" +
    ///     &lt;serviceName&gt; (e.g. <c>.../service/stop/remote-console</c>). <see cref="InstanceId" /> is the required
    ///     top-level field naming the per-service unit instance to stop (for remote access, the RemoteAccessSession
    ///     id); stop needs no further arguments. See the architecture spec
    ///     <c>2026-06-30-on-demand-remote-gateway-access</c>.
    /// </summary>
    [Schema("StopSystemServicePayload")]
    public record StopSystemServicePayload(string InstanceId) : IMessage;
}
